using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgenticCommerce.API.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-Key";

    private readonly AgenticCommerceDbContext _db;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AgenticCommerceDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for API key in header
        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        // Hash the provided key
        var keyHash = HashApiKey(apiKey);

        // Look up the API key
        var storedKey = await _db.ApiKeys
            .Include(k => k.Organization)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.RevokedAt == null);

        if (storedKey == null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        // Validate tier vs environment
        var org = storedKey.Organization;
        if (storedKey.IsMainnet && org.Tier == OrganizationTiers.Sandbox)
        {
            return AuthenticateResult.Fail("Sandbox tier cannot use mainnet API keys. Upgrade to pay-as-you-go to access mainnet.");
        }

        // Update last used timestamp
        storedKey.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Create claims with tier and environment info
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, storedKey.Id.ToString()),
            new Claim("organization_id", storedKey.OrganizationId.ToString()),
            new Claim("organization_name", org.Name),
            new Claim("organization_tier", org.Tier),
            new Claim("api_key_id", storedKey.Id.ToString()),
            new Claim("api_key_name", storedKey.Name),
            new Claim("api_key_environment", storedKey.Environment),
            new Claim(ClaimTypes.AuthenticationMethod, "ApiKey")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        Logger.LogInformation("API key authenticated: {KeyName} ({Environment}) for org {OrgId} ({Tier})",
            storedKey.Name, storedKey.Environment, storedKey.OrganizationId, org.Tier);

        return AuthenticateResult.Success(ticket);
    }

    private static string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}

public static class ApiKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder builder)
    {
        return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
            ApiKeyAuthenticationHandler.SchemeName,
            options => { });
    }
}

/// <summary>
/// Extension methods for accessing API key context from HttpContext
/// </summary>
public static class ApiKeyContextExtensions
{
    public static Guid? GetOrganizationId(this HttpContext context)
    {
        var claim = context.User.FindFirst("organization_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    public static string? GetOrganizationTier(this HttpContext context)
    {
        return context.User.FindFirst("organization_tier")?.Value;
    }

    public static Guid? GetApiKeyId(this HttpContext context)
    {
        var claim = context.User.FindFirst("api_key_id");
        return claim != null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    public static string? GetApiKeyEnvironment(this HttpContext context)
    {
        return context.User.FindFirst("api_key_environment")?.Value;
    }

    public static bool IsMainnet(this HttpContext context)
    {
        return context.GetApiKeyEnvironment() == ApiKeyEnvironments.Mainnet;
    }

    public static bool IsPayingTier(this HttpContext context)
    {
        var tier = context.GetOrganizationTier();
        return tier == OrganizationTiers.PayAsYouGo || 
               tier == OrganizationTiers.Pro || 
               tier == OrganizationTiers.Enterprise;
    }
}
