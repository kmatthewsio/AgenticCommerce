using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Trust;

/// <summary>
/// Implementation of trust checking service - combines service registry
/// with payment history to derive trust scores.
/// </summary>
public class TrustService : ITrustService
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<TrustService> _logger;

    public TrustService(AgenticCommerceDbContext db, ILogger<TrustService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TrustCheckResult> CheckTrustAsync(string serviceUrl)
    {
        _logger.LogInformation("Checking trust for service: {ServiceUrl}", serviceUrl);

        // Get registry entry if exists
        var service = await GetServiceAsync(serviceUrl);

        // Get payment stats from x402 payments
        var stats = await GetPaymentStatsAsync(serviceUrl);

        // Calculate trust score
        var trustScore = CalculateTrustScore(service, stats);

        return new TrustCheckResult
        {
            ServiceUrl = serviceUrl,
            Registered = service != null,
            Verified = service?.Verified ?? false,
            Name = service?.Name,
            Description = service?.Description,
            OwnerWallet = service?.OwnerWallet,
            Stats = stats,
            TrustScore = trustScore
        };
    }

    /// <inheritdoc />
    public async Task<ServiceRegistryEntity?> GetServiceAsync(string serviceUrl)
    {
        return await _db.ServiceRegistry
            .FirstOrDefaultAsync(s => s.ServiceUrl == serviceUrl);
    }

    /// <inheritdoc />
    public async Task<ServiceRegistryEntity> RegisterServiceAsync(RegisterServiceRequest request)
    {
        _logger.LogInformation("Registering service: {ServiceUrl}", request.ServiceUrl);

        // Check if already registered
        var existing = await GetServiceAsync(request.ServiceUrl);
        if (existing != null)
        {
            throw new InvalidOperationException($"Service already registered: {request.ServiceUrl}");
        }

        var entity = new ServiceRegistryEntity
        {
            ServiceUrl = request.ServiceUrl,
            Name = request.Name,
            Description = request.Description,
            OwnerWallet = request.OwnerWallet,
            PriceUsdc = request.PriceUsdc,
            Verified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ServiceRegistry.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Service registered successfully: {ServiceUrl}", request.ServiceUrl);
        return entity;
    }

    /// <inheritdoc />
    public async Task<List<ServiceRegistryEntity>> ListServicesAsync(bool? verifiedOnly = null, int limit = 50)
    {
        var query = _db.ServiceRegistry.AsQueryable();

        if (verifiedOnly.HasValue)
        {
            query = query.Where(s => s.Verified == verifiedOnly.Value);
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ServiceRegistryEntity?> VerifyServiceAsync(string serviceUrl)
    {
        _logger.LogInformation("Verifying service: {ServiceUrl}", serviceUrl);

        var service = await GetServiceAsync(serviceUrl);
        if (service == null)
        {
            return null;
        }

        service.Verified = true;
        service.VerifiedAt = DateTime.UtcNow;
        service.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Service verified: {ServiceUrl}", serviceUrl);
        return service;
    }

    /// <inheritdoc />
    public async Task<ServicePaymentStats> GetPaymentStatsAsync(string serviceUrl)
    {
        // Query x402_payments table for this resource URL
        var payments = await _db.X402Payments
            .Where(p => p.Resource == serviceUrl)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalPayments = g.Count(),
                SettledPayments = g.Count(p => p.Status == X402PaymentStatus.Settled),
                FailedPayments = g.Count(p => p.Status == X402PaymentStatus.Failed),
                TotalVolumeUsdc = g.Sum(p => p.AmountUsdc),
                FirstPayment = g.Min(p => p.CreatedAt),
                LastPayment = g.Max(p => p.CreatedAt)
            })
            .FirstOrDefaultAsync();

        if (payments == null)
        {
            return new ServicePaymentStats();
        }

        var successRate = payments.TotalPayments > 0
            ? Math.Round((decimal)payments.SettledPayments / payments.TotalPayments * 100, 1)
            : 0;

        return new ServicePaymentStats
        {
            TotalPayments = payments.TotalPayments,
            SettledPayments = payments.SettledPayments,
            FailedPayments = payments.FailedPayments,
            SuccessRate = successRate,
            TotalVolumeUsdc = payments.TotalVolumeUsdc,
            FirstPayment = payments.FirstPayment,
            LastPayment = payments.LastPayment
        };
    }

    /// <summary>
    /// Calculate trust score based on registry status and payment history
    /// </summary>
    private string CalculateTrustScore(ServiceRegistryEntity? service, ServicePaymentStats stats)
    {
        // high: registered + verified + 100+ settled payments + 95%+ success rate
        if (service?.Verified == true && stats.SettledPayments >= 100 && stats.SuccessRate >= 95)
        {
            return "high";
        }

        // medium: registered + 50+ settled payments + 90%+ success rate
        if (service != null && stats.SettledPayments >= 50 && stats.SuccessRate >= 90)
        {
            return "medium";
        }

        // low: registered OR 10+ payments (but doesn't meet higher criteria)
        if (service != null || stats.SettledPayments >= 10)
        {
            return "low";
        }

        // unknown: not registered AND <10 payments
        return "unknown";
    }
}
