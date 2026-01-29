using System.Security.Cryptography;
using System.Text;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Gumroad;

public interface IApiKeyGenerationService
{
    Task<(Organization org, ApiKey apiKey, string rawKey)> ProvisionForPurchaseAsync(
        string email,
        string productName,
        string saleId);

    Task<ApiKey?> GetApiKeyBySaleIdAsync(string saleId);
}

public class ApiKeyGenerationService : IApiKeyGenerationService
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<ApiKeyGenerationService> _logger;

    public ApiKeyGenerationService(
        AgenticCommerceDbContext db,
        ILogger<ApiKeyGenerationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(Organization org, ApiKey apiKey, string rawKey)> ProvisionForPurchaseAsync(
        string email,
        string productName,
        string saleId)
    {
        // Check if we already provisioned for this sale
        var existingPurchase = await _db.GumroadPurchases
            .Include(p => p.Organization)
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.SaleId == saleId);

        if (existingPurchase?.Organization != null && existingPurchase.ApiKey != null)
        {
            _logger.LogInformation("Purchase {SaleId} already provisioned", saleId);
            // Return existing but we can't return the raw key (it's hashed)
            // The customer will need to use their original key or request a new one
            throw new InvalidOperationException($"Purchase {saleId} already provisioned. API key was provided at time of purchase.");
        }

        // Create organization based on email
        var orgSlug = GenerateSlug(email);
        var org = new Organization
        {
            Name = $"{productName} - {email}",
            Slug = orgSlug
        };

        _db.Organizations.Add(org);
        await _db.SaveChangesAsync();

        // Generate API key
        var (apiKey, rawKey) = GenerateApiKey(org.Id, $"Gumroad Purchase - {saleId}");
        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Provisioned org {OrgId} and API key {KeyPrefix} for {Email}",
            org.Id, apiKey.KeyPrefix, email);

        return (org, apiKey, rawKey);
    }

    public async Task<ApiKey?> GetApiKeyBySaleIdAsync(string saleId)
    {
        var purchase = await _db.GumroadPurchases
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.SaleId == saleId);

        return purchase?.ApiKey;
    }

    private (ApiKey apiKey, string rawKey) GenerateApiKey(Guid organizationId, string name)
    {
        // Generate a secure random key: ac_live_xxxxxxxxxxxxxxxxxxxxxxxxxxxx
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        var rawKey = $"ac_live_{Convert.ToBase64String(keyBytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32]}";

        var keyHash = HashKey(rawKey);
        var keyPrefix = rawKey[..12]; // ac_live_xxxx

        var apiKey = new ApiKey
        {
            OrganizationId = organizationId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix
        };

        return (apiKey, rawKey);
    }

    private static string HashKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string GenerateSlug(string email)
    {
        // Extract username part of email and make it URL-safe
        var username = email.Split('@')[0];
        var slug = username.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');

        // Add random suffix to ensure uniqueness
        var suffix = Guid.NewGuid().ToString("N")[..6];
        return $"{slug}-{suffix}";
    }
}
