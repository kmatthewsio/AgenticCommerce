using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Email;
using AgenticCommerce.Infrastructure.Gumroad;
using AgenticCommerce.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace AgenticCommerce.API.Controllers;

/// <summary>
/// Stripe payment controller for Enterprise tier purchases.
///
/// Product Tiers:
/// - Standard/Sandbox: Free tier (no Stripe), uses x402 protocol on testnet
/// - Enterprise: $2,500 one-time via Stripe Checkout, full production access
///
/// All payment events are logged to:
/// - Application logs (Serilog - file + console)
/// - Database logs (app_logs table via DbLogger)
/// - Stripe purchases table (stripe_purchases for audit trail)
/// </summary>
[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly AgenticCommerceDbContext _db;
    private readonly IApiKeyGenerationService _apiKeyService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeController> _logger;
    private readonly IDbLogger _dbLogger;

    public StripeController(
        AgenticCommerceDbContext db,
        IApiKeyGenerationService apiKeyService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<StripeController> logger,
        IDbLogger dbLogger)
    {
        _db = db;
        _apiKeyService = apiKeyService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _dbLogger = dbLogger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    /// <summary>
    /// Create a Stripe Checkout Session for the Implementation Kit
    /// </summary>
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest? request)
    {
        var priceId = _configuration["Stripe:ImplementationKitPriceId"];
        if (string.IsNullOrEmpty(priceId))
        {
            _logger.LogError("Stripe:ImplementationKitPriceId not configured");
            return StatusCode(500, new { error = "Product not configured" });
        }

        var domain = _configuration["App:Domain"] ?? "https://agentrails.io";

        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = priceId,
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = $"{domain}/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/#pricing",
            CustomerEmail = request?.Email, // Pre-fill email if provided
            Metadata = new Dictionary<string, string>
            {
                { "product", "implementation-kit" }
            }
        };

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created checkout session {SessionId}", session.Id);

            // Log to database for payment audit trail
            await _dbLogger.LogAsync(
                "Information",
                $"Enterprise checkout session created: {session.Id}",
                source: "StripeController",
                requestPath: HttpContext.Request.Path,
                properties: new Dictionary<string, object>
                {
                    { "sessionId", session.Id },
                    { "email", request?.Email ?? "not provided" },
                    { "product", "AgentRails Implementation Kit" },
                    { "tier", "Enterprise" },
                    { "amount", "$2,500" }
                });

            return Ok(new { url = session.Url, sessionId = session.Id });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create checkout session");

            // Log error to database
            await _dbLogger.LogErrorAsync(
                $"Failed to create Enterprise checkout session: {ex.Message}",
                source: "StripeController",
                exception: ex.ToString());

            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Stripe webhook endpoint - handles checkout.session.completed events
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _configuration["Stripe:WebhookSecret"];

        Event stripeEvent;

        try
        {
            if (string.IsNullOrEmpty(webhookSecret))
            {
                // For testing without webhook signature verification
                _logger.LogWarning("Stripe webhook secret not configured - skipping signature verification");
                stripeEvent = EventUtility.ParseEvent(json);
            }
            else
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
            }
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return BadRequest(new { error = "Invalid signature" });
        }

        _logger.LogInformation("Received Stripe event {EventType} ({EventId})",
            stripeEvent.Type, stripeEvent.Id);

        // Handle the event
        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompleted(stripeEvent, json);
                break;

            case "charge.refunded":
                await HandleChargeRefunded(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        return Ok(new { received = true });
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent, string rawJson)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null)
        {
            _logger.LogWarning("Could not parse checkout session from event");
            return;
        }

        _logger.LogInformation("Processing checkout session {SessionId} for {Email}",
            session.Id, session.CustomerEmail ?? session.CustomerDetails?.Email);

        // Check for duplicate
        var existingPurchase = await _db.StripePurchases
            .FirstOrDefaultAsync(p => p.SessionId == session.Id);

        if (existingPurchase != null)
        {
            _logger.LogInformation("Duplicate webhook for session {SessionId}, ignoring", session.Id);
            return;
        }

        var email = session.CustomerEmail ?? session.CustomerDetails?.Email;
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogError("No email found for checkout session {SessionId}", session.Id);
            return;
        }

        try
        {
            // Provision organization and API key
            var productName = session.Metadata?.GetValueOrDefault("product") == "implementation-kit"
                ? "AgentRails Implementation Kit"
                : "AgentRails";

            var (org, apiKey, rawKey) = await _apiKeyService.ProvisionForStripeAsync(
                email,
                productName,
                session.Id);

            // Record the purchase
            var purchase = new StripePurchase
            {
                SessionId = session.Id,
                PaymentIntentId = session.PaymentIntentId,
                CustomerId = session.CustomerId,
                Email = email,
                ProductName = productName,
                AmountCents = (int)(session.AmountTotal ?? 0),
                Currency = session.Currency ?? "usd",
                OrganizationId = org.Id,
                ApiKeyId = apiKey.Id,
                RawEvent = rawJson
            };

            _db.StripePurchases.Add(purchase);
            await _db.SaveChangesAsync();

            // Send API key via email (non-blocking - don't fail webhook if email fails)
            try
            {
                await _emailService.SendApiKeyEmailAsync(email, rawKey, productName);
            }
            catch (Exception emailEx)
            {
                _logger.LogWarning(emailEx, "Failed to send API key email to {Email}, but purchase was recorded", email);
                await _dbLogger.LogWarningAsync(
                    $"Email delivery failed for {email} - API key was provisioned but email not sent",
                    source: "StripeController.Webhook",
                    exception: emailEx.Message);
            }

            _logger.LogInformation(
                "Provisioned API key {KeyPrefix} for {Email} (Stripe session {SessionId})",
                apiKey.KeyPrefix, email, session.Id);

            // Log successful Enterprise purchase to database
            await _dbLogger.LogAsync(
                "Information",
                $"Enterprise purchase completed: {email} - {productName}",
                source: "StripeController.Webhook",
                properties: new Dictionary<string, object>
                {
                    { "tier", "Enterprise" },
                    { "sessionId", session.Id },
                    { "paymentIntentId", session.PaymentIntentId ?? "" },
                    { "email", email },
                    { "productName", productName },
                    { "amountCents", session.AmountTotal ?? 0 },
                    { "currency", session.Currency ?? "usd" },
                    { "organizationId", org.Id.ToString() },
                    { "apiKeyPrefix", apiKey.KeyPrefix },
                    { "status", "completed" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision API key for Stripe session {SessionId}", session.Id);

            // Log error to database
            await _dbLogger.LogErrorAsync(
                $"Failed to provision Enterprise API key for session {session.Id}: {ex.Message}",
                source: "StripeController.Webhook",
                exception: ex.ToString());

            throw; // Re-throw to signal webhook failure (Stripe will retry)
        }
    }

    private async Task HandleChargeRefunded(Event stripeEvent)
    {
        var charge = stripeEvent.Data.Object as Charge;
        if (charge == null)
        {
            _logger.LogWarning("Could not parse charge from refund event");
            return;
        }

        // Find purchase by payment intent
        var purchase = await _db.StripePurchases
            .Include(p => p.ApiKey)
            .FirstOrDefaultAsync(p => p.PaymentIntentId == charge.PaymentIntentId);

        if (purchase == null)
        {
            _logger.LogWarning("Refund for unknown payment intent {PaymentIntentId}", charge.PaymentIntentId);
            return;
        }

        purchase.Refunded = true;

        // Revoke the API key
        if (purchase.ApiKey != null)
        {
            purchase.ApiKey.RevokedAt = DateTime.UtcNow;
            _logger.LogInformation("Revoked API key {KeyId} due to Stripe refund", purchase.ApiKey.Id);

            // Log refund to database
            await _dbLogger.LogAsync(
                "Warning",
                $"Enterprise purchase refunded - API key revoked: {purchase.Email}",
                source: "StripeController.Webhook",
                properties: new Dictionary<string, object>
                {
                    { "tier", "Enterprise" },
                    { "sessionId", purchase.SessionId },
                    { "paymentIntentId", charge.PaymentIntentId ?? "" },
                    { "email", purchase.Email },
                    { "productName", purchase.ProductName },
                    { "apiKeyId", purchase.ApiKey.Id.ToString() },
                    { "status", "refunded" },
                    { "apiKeyRevoked", true }
                });
        }

        await _db.SaveChangesAsync();
    }
}

public class CreateCheckoutRequest
{
    public string? Email { get; set; }
}
