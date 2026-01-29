using System.Security.Cryptography;
using System.Text;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Gumroad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/gumroad")]
public class GumroadController : ControllerBase
{
    private readonly AgenticCommerceDbContext _db;
    private readonly IApiKeyGenerationService _apiKeyService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GumroadController> _logger;

    public GumroadController(
        AgenticCommerceDbContext db,
        IApiKeyGenerationService apiKeyService,
        IConfiguration configuration,
        ILogger<GumroadController> logger)
    {
        _db = db;
        _apiKeyService = apiKeyService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gumroad Ping webhook endpoint - called when a purchase is made
    /// </summary>
    [HttpPost("webhook")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> HandleWebhook([FromForm] GumroadPingPayload payload)
    {
        _logger.LogInformation("Received Gumroad webhook for sale {SaleId}, product {ProductId}",
            payload.SaleId, payload.ProductId);

        // Verify webhook signature if configured
        var webhookSecret = _configuration["Gumroad:WebhookSecret"];
        if (!string.IsNullOrEmpty(webhookSecret))
        {
            if (!VerifySignature(webhookSecret))
            {
                _logger.LogWarning("Invalid Gumroad webhook signature");
                return Unauthorized("Invalid signature");
            }
        }

        // Check if this is a refund notification
        if (payload.Refunded?.ToLower() == "true")
        {
            await HandleRefundAsync(payload.SaleId);
            return Ok(new { status = "refund_processed" });
        }

        // Check for duplicate
        var existingPurchase = await _db.GumroadPurchases
            .FirstOrDefaultAsync(p => p.SaleId == payload.SaleId);

        if (existingPurchase != null)
        {
            _logger.LogInformation("Duplicate webhook for sale {SaleId}, ignoring", payload.SaleId);
            return Ok(new { status = "duplicate", message = "Purchase already processed" });
        }

        try
        {
            // Provision organization and API key
            var (org, apiKey, rawKey) = await _apiKeyService.ProvisionForPurchaseAsync(
                payload.Email,
                payload.ProductName ?? "AgenticCommerce Pro",
                payload.SaleId);

            // Record the purchase
            var purchase = new GumroadPurchase
            {
                SaleId = payload.SaleId,
                ProductId = payload.ProductId,
                ProductName = payload.ProductName ?? "Unknown",
                Email = payload.Email,
                LicenseKey = payload.LicenseKey,
                PriceCents = ParsePrice(payload.Price),
                Currency = payload.Currency ?? "usd",
                OrganizationId = org.Id,
                ApiKeyId = apiKey.Id,
                RawPayload = SerializePayload(payload)
            };

            _db.GumroadPurchases.Add(purchase);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Provisioned API key {KeyPrefix} for {Email} (sale {SaleId})",
                apiKey.KeyPrefix, payload.Email, payload.SaleId);

            // Return the API key - Gumroad can display this in the purchase confirmation
            // IMPORTANT: This is the only time the raw key is returned
            return Ok(new
            {
                status = "success",
                api_key = rawKey,
                key_prefix = apiKey.KeyPrefix,
                message = "Your API key has been generated. Save it securely - it won't be shown again."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision API key for sale {SaleId}", payload.SaleId);
            return StatusCode(500, new { status = "error", message = "Failed to provision API key" });
        }
    }

    /// <summary>
    /// Verify a license key and return the associated API key prefix
    /// (Customer can use this if they lost their API key - they'll need to contact support for a new one)
    /// </summary>
    [HttpGet("verify/{licenseKey}")]
    public async Task<IActionResult> VerifyLicenseKey(string licenseKey)
    {
        var purchase = await _db.GumroadPurchases
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.LicenseKey == licenseKey && !p.Refunded);

        if (purchase == null)
        {
            return NotFound(new { status = "invalid", message = "License key not found or refunded" });
        }

        return Ok(new
        {
            status = "valid",
            product_name = purchase.ProductName,
            email = purchase.Email,
            api_key_prefix = purchase.ApiKey?.KeyPrefix,
            created_at = purchase.CreatedAt,
            message = purchase.ApiKey != null
                ? "License is valid. If you need a new API key, please contact support."
                : "License is valid but no API key was provisioned."
        });
    }

    private bool VerifySignature(string secret)
    {
        // Gumroad signs webhooks with HMAC SHA-256
        if (!Request.Headers.TryGetValue("X-Gumroad-Signature", out var signatureHeader))
        {
            return false;
        }

        // Read the raw body for signature verification
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = reader.ReadToEnd();
        Request.Body.Position = 0;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var expectedSignature = Convert.ToBase64String(hash);

        return signatureHeader.ToString() == expectedSignature;
    }

    private async Task HandleRefundAsync(string saleId)
    {
        var purchase = await _db.GumroadPurchases
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.SaleId == saleId);

        if (purchase == null)
        {
            _logger.LogWarning("Refund for unknown sale {SaleId}", saleId);
            return;
        }

        purchase.Refunded = true;

        // Revoke the API key
        if (purchase.ApiKey != null)
        {
            purchase.ApiKey.RevokedAt = DateTime.UtcNow;
            _logger.LogInformation("Revoked API key {KeyId} due to refund", purchase.ApiKey.Id);
        }

        await _db.SaveChangesAsync();
    }

    private static int ParsePrice(string? price)
    {
        if (string.IsNullOrEmpty(price)) return 0;
        if (int.TryParse(price, out var cents)) return cents;
        return 0;
    }

    private static string SerializePayload(GumroadPingPayload payload)
    {
        return System.Text.Json.JsonSerializer.Serialize(payload);
    }
}

/// <summary>
/// Gumroad Ping webhook payload (form-urlencoded)
/// </summary>
public class GumroadPingPayload
{
    [FromForm(Name = "seller_id")]
    public string SellerId { get; set; } = string.Empty;

    [FromForm(Name = "product_id")]
    public string ProductId { get; set; } = string.Empty;

    [FromForm(Name = "product_name")]
    public string? ProductName { get; set; }

    [FromForm(Name = "permalink")]
    public string? Permalink { get; set; }

    [FromForm(Name = "product_permalink")]
    public string? ProductPermalink { get; set; }

    [FromForm(Name = "email")]
    public string Email { get; set; } = string.Empty;

    [FromForm(Name = "price")]
    public string? Price { get; set; }

    [FromForm(Name = "currency")]
    public string? Currency { get; set; }

    [FromForm(Name = "quantity")]
    public string? Quantity { get; set; }

    [FromForm(Name = "order_number")]
    public string? OrderNumber { get; set; }

    [FromForm(Name = "sale_id")]
    public string SaleId { get; set; } = string.Empty;

    [FromForm(Name = "sale_timestamp")]
    public string? SaleTimestamp { get; set; }

    [FromForm(Name = "license_key")]
    public string? LicenseKey { get; set; }

    [FromForm(Name = "ip_country")]
    public string? IpCountry { get; set; }

    [FromForm(Name = "refunded")]
    public string? Refunded { get; set; }

    [FromForm(Name = "resource_name")]
    public string? ResourceName { get; set; }

    [FromForm(Name = "disputed")]
    public string? Disputed { get; set; }

    [FromForm(Name = "dispute_won")]
    public string? DisputeWon { get; set; }

    [FromForm(Name = "purchaser_id")]
    public string? PurchaserId { get; set; }
}
