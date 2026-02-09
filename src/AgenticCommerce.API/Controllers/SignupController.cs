using System.Security.Cryptography;
using System.Text;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/signup")]
public class SignupController : ControllerBase
{
    private readonly AgenticCommerceDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SignupController> _logger;

    public SignupController(
        AgenticCommerceDbContext db,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SignupController> logger)
    {
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    [HttpPost]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        var email = request.Email.Trim().ToLowerInvariant();
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
            return Conflict(new { error = "An account already exists for this email" });

        try
        {
            string stripeCustomerId = null;
            try
            {
                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(new CustomerCreateOptions { Email = email });
                stripeCustomerId = customer.Id;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Stripe customer for {Email}", email);
            }

            var orgSlug = GenerateSlug(email);
            var org = new Organization
            {
                Name = email,
                Slug = orgSlug,
                Tier = OrganizationTiers.Sandbox,
                StripeCustomerId = stripeCustomerId
            };
            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();

            var user = new User
            {
                OrganizationId = org.Id,
                Email = email,
                PasswordHash = "",
                Role = UserRoles.Owner
            };
            _db.Users.Add(user);

            var (apiKey, rawKey) = GenerateApiKey(org.Id, "Default API Key", ApiKeyEnvironments.Testnet);
            _db.ApiKeys.Add(apiKey);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created sandbox account for {Email}", email);

            return Ok(new SignupResponse
            {
                Success = true,
                Email = email,
                Tier = org.Tier,
                ApiKey = rawKey,
                Environment = apiKey.Environment,
                Message = "Sandbox account created."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account for {Email}", email);
            return StatusCode(500, new { error = "Failed to create account" });
        }
    }

    [HttpPost("upgrade")]
    public async Task<IActionResult> UpgradeToPayg([FromBody] UpgradeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "Email is required" });

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Organization).FirstOrDefaultAsync(u => u.Email == email);
        if (user?.Organization == null)
            return NotFound(new { error = "Account not found" });

        var org = user.Organization;
        if (org.Tier != OrganizationTiers.Sandbox)
            return BadRequest(new { error = "Account is already upgraded" });

        try
        {
            org.Tier = OrganizationTiers.PayAsYouGo;
            var (apiKey, rawKey) = GenerateApiKey(org.Id, "Production API Key", ApiKeyEnvironments.Mainnet);
            _db.ApiKeys.Add(apiKey);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Upgraded {Email} to pay-as-you-go", email);

            return Ok(new SignupResponse
            {
                Success = true,
                Email = email,
                Tier = org.Tier,
                ApiKey = rawKey,
                Environment = apiKey.Environment,
                Message = "Upgraded to pay-as-you-go."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upgrade {Email}", email);
            return StatusCode(500, new { error = "Failed to upgrade account" });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = "Email is required" });

        var user = await _db.Users
            .Include(u => u.Organization).ThenInclude(o => o.ApiKeys)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());

        if (user?.Organization == null)
            return NotFound(new { error = "Account not found" });

        var org = user.Organization;
        var apiKeys = org.ApiKeys.Where(k => k.RevokedAt == null)
            .Select(k => new { prefix = k.KeyPrefix, environment = k.Environment }).ToList();

        return Ok(new { email = user.Email, tier = org.Tier, apiKeys });
    }

    private (ApiKey apiKey, string rawKey) GenerateApiKey(Guid organizationId, string name, string environment)
    {
        var prefix = environment == ApiKeyEnvironments.Mainnet ? "ar_live" : "ar_test";
        var keyBytes = new byte[32];
        RandomNumberGenerator.Fill(keyBytes);
        var base64 = Convert.ToBase64String(keyBytes).Replace("+", "").Replace("/", "").Replace("=", "");
        var rawKey = prefix + "_" + base64.Substring(0, 32);
        var keyHash = HashKey(rawKey);
        var keyPrefix = rawKey.Substring(0, 12);
        var apiKey = new ApiKey
        {
            OrganizationId = organizationId,
            Name = name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Environment = environment
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
        var username = email.Split('@')[0];
        var slug = username.ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9]", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-").Trim('-');
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        return slug + "-" + suffix;
    }
}

public class SignupRequest { public string Email { get; set; } = ""; }
public class UpgradeRequest { public string Email { get; set; } = ""; }
public class SignupResponse
{
    public bool Success { get; set; }
    public string Email { get; set; } = "";
    public string Tier { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Environment { get; set; } = "";
    public string Message { get; set; } = "";
}
