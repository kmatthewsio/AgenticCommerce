using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Billing;

public class UsageTrackingService : IUsageTrackingService
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<UsageTrackingService> _logger;

    private const decimal FeeRate = 0.005m;

    public UsageTrackingService(
        AgenticCommerceDbContext db,
        ILogger<UsageTrackingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UsageEvent> RecordTransactionAsync(
        Guid organizationId,
        Guid? apiKeyId,
        decimal transactionAmountUsdc,
        string? paymentId = null)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        var feeAmount = org.Tier == OrganizationTiers.Pro 
            ? 0m 
            : transactionAmountUsdc * FeeRate;

        var usageEvent = new UsageEvent
        {
            OrganizationId = organizationId,
            ApiKeyId = apiKeyId,
            PaymentId = paymentId,
            TransactionAmount = transactionAmountUsdc,
            FeeAmount = feeAmount,
            Billed = org.Tier == OrganizationTiers.Pro,
            RecordedAt = DateTime.UtcNow
        };

        _db.UsageEvents.Add(usageEvent);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Recorded usage for org {OrgId}: {Amount} USDC, fee {Fee} USD",
            organizationId, transactionAmountUsdc, feeAmount);

        return usageEvent;
    }

    public async Task<IEnumerable<UsageEvent>> GetUnbilledUsageAsync(Guid organizationId)
    {
        return await _db.UsageEvents
            .Where(u => u.OrganizationId == organizationId && !u.Billed)
            .OrderBy(u => u.RecordedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetUnbilledFeesAsync(Guid organizationId)
    {
        return await _db.UsageEvents
            .Where(u => u.OrganizationId == organizationId && !u.Billed)
            .SumAsync(u => u.FeeAmount);
    }

    public async Task MarkAsBilledAsync(IEnumerable<Guid> usageEventIds)
    {
        var events = await _db.UsageEvents
            .Where(u => usageEventIds.Contains(u.Id))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var evt in events)
        {
            evt.Billed = true;
            evt.BilledAt = now;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Marked {Count} usage events as billed", events.Count);
    }

    public async Task<UsageSummary> GetUsageSummaryAsync(Guid organizationId, DateTime from, DateTime to)
    {
        var events = await _db.UsageEvents
            .Where(u => u.OrganizationId == organizationId && 
                        u.RecordedAt >= from && 
                        u.RecordedAt < to)
            .ToListAsync();

        return new UsageSummary
        {
            OrganizationId = organizationId,
            From = from,
            To = to,
            TransactionCount = events.Count,
            TotalVolume = events.Sum(e => e.TransactionAmount),
            TotalFees = events.Sum(e => e.FeeAmount),
            BilledFees = events.Where(e => e.Billed).Sum(e => e.FeeAmount),
            UnbilledFees = events.Where(e => !e.Billed).Sum(e => e.FeeAmount)
        };
    }
}
