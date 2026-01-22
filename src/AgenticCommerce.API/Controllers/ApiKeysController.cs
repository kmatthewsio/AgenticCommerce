using System.Security.Cryptography;
using AgenticCommerce.API.Middleware;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ApiKeysController : ControllerBase
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<ApiKeysController> _logger;

    public ApiKeysController(AgenticCommerceDbContext db, ILogger<ApiKeysController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListApiKeys()
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var keys = await _db.ApiKeys
            .Where(k => k.OrganizationId == orgId.Value && k.RevokedAt == null)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new
            {
                k.Id,
                k.Name,
                maskedKey = k.MaskedKey,
                k.CreatedAt,
                k.LastUsedAt
            })
            .ToListAsync();

        return Ok(keys);
    }

    [HttpPost]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        // Generate a secure random API key
        var rawKey = GenerateApiKey();
        var keyPrefix = rawKey.Substring(0, 8);
        var keyHash = HashApiKey(rawKey);

        var apiKey = new ApiKey
        {
            OrganizationId = orgId.Value,
            Name = request.Name,
            KeyHash = keyHash,
            KeyPrefix = keyPrefix
        };

        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync();

        _logger.LogInformation("API key created: {Name} for org {OrgId}", request.Name, orgId.Value);

        // Return the raw key only once - it won't be retrievable again
        return Ok(new
        {
            id = apiKey.Id,
            name = apiKey.Name,
            key = rawKey,
            createdAt = apiKey.CreatedAt
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeApiKey(Guid id)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var apiKey = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.OrganizationId == orgId.Value);

        if (apiKey == null)
        {
            return NotFound(new { error = "API key not found" });
        }

        apiKey.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("API key revoked: {Id}", id);

        return Ok(new { message = "API key revoked" });
    }

    private static string GenerateApiKey()
    {
        // Format: ar_<random-base64-string>
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var base64 = Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
        return $"ar_{base64}";
    }

    private static string HashApiKey(string key)
    {
        using var sha256 = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}

public record CreateApiKeyRequest(string Name);
