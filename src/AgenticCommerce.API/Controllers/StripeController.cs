using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Email;
using AgenticCommerce.Infrastructure.Gumroad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace AgenticCommerce.API.Controllers;

/// <summary>
/// Stripe payment controller for paid tier purchases.
///
/// Product Tiers:
/// - Sandbox: Free tier (no Stripe), uses x402 protocol on testnet
/// - Pay-as-you-go: 0.5% per transaction, billed monthly via Stripe metered billing
/// - Pro: $49/month subscription via Stripe, 0% transaction fees
/// - Enterprise: $2,500 one-time via Stripe Checkout, full source + policy engine
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
    private readonly IStripeBillingService _stripeBillingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeController> _logger;

    public StripeController(
        AgenticCommerceDbContext db,
        IApiKeyGenerationService apiKeyService,
        IEmailService emailService,
        IStripeBillingService stripeBillingService,
        IConfiguration configuration,
        ILogger<StripeController> logger)
    {
        _db = db;
        _apiKeyService = apiKeyService;
        _emailService = emailService;
        _stripeBillingService = stripeBillingService;
        _configuration = configuration;
        _logger = logger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    /// <summary>
    /// Create a Stripe Checkout Session for the specified tier
    /// </summary>
    /// <param name="request">Checkout request with optional email and tier (payg/pro/enterprise)</param>
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest? request)
    {
        // Determine tier and price
        var tier = request?.Tier?.ToLowerInvariant() ?? "enterprise";
        string? priceId;
        string productName;
        string amount;
        string mode;

        switch (tier)
        {
            case "payg":
                // Pay-as-you-go: Create a subscription with metered billing (0.5% per tx)
                priceId = _configuration["Stripe:PaygPriceId"];
                productName = "AgentRails Pay-as-you-go";
                amount = "0.5% per transaction";
                mode = "subscription";
                break;
            case "pro":
                // Pro: $49/month subscription
                priceId = _configuration["Stripe:ProPriceId"];
                productName = "AgentRails Pro";
                amount = "$49/month";
                mode = "subscription";
                break;
            case "startup":
                priceId = _configuration["Stripe:StartupPriceId"];
                productName = "AgentRails Startup";
                amount = "$500";
                mode = "payment";
                break;
            case "enterprise":
            default:
                priceId = _configuration["Stripe:EnterprisePriceId"]
                    ?? _configuration["Stripe:ImplementationKitPriceId"]; // fallback for backward compat
                productName = "AgentRails Enterprise";
                amount = "$2,500";
                mode = "payment";
                break;
        }

        if (string.IsNullOrEmpty(priceId))
        {
            _logger.LogError("Stripe price not configured for tier {Tier}", tier);
            return StatusCode(500, new { error = $"Product not configured for tier: {tier}. Please contact sales@agentrails.io" });
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
            Mode = mode,
            SuccessUrl = $"{domain}/success?session_id={{CHECKOUT_SESSION_ID}}&tier={tier}",
            CancelUrl = $"{domain}/#pricing",
            CustomerEmail = request?.Email, // Pre-fill email if provided
            Metadata = new Dictionary<string, string>
            {
                { "product", productName },
                { "tier", tier }
            }
        };

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options);

            _logger.LogInformation("Created {Tier} checkout session {SessionId} ({Amount})", tier, session.Id, amount);

            return Ok(new { url = session.Url, sessionId = session.Id });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create checkout session for tier {Tier}", tier);

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

            // Metered billing events
            case "invoice.paid":
                await HandleInvoicePaid(stripeEvent);
                break;

            case "invoice.payment_failed":
                await HandleInvoicePaymentFailed(stripeEvent);
                break;

            case "customer.subscription.deleted":
                await HandleSubscriptionDeleted(stripeEvent);
                break;

            default:
                _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                break;
        }

        return Ok(new { received = true });
    }

    private async Task HandleInvoicePaid(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            return;
        }

        // Get subscription ID from parent.subscription_details.subscription.id
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogDebug("Invoice {InvoiceId} has no subscription ID", invoice.Id);
            return;
        }

        await _stripeBillingService.HandleInvoicePaidAsync(subscriptionId, invoice.Id);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
        {
            return;
        }

        // Get subscription ID from parent.subscription_details.subscription.id
        var subscriptionId = invoice.Parent?.SubscriptionDetails?.Subscription?.Id;
        if (string.IsNullOrEmpty(subscriptionId))
        {
            _logger.LogDebug("Invoice {InvoiceId} has no subscription ID", invoice.Id);
            return;
        }

        await _stripeBillingService.HandleInvoicePaymentFailedAsync(subscriptionId, invoice.Id);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null)
        {
            return;
        }

        await _stripeBillingService.HandleSubscriptionDeletedAsync(subscription.Id);
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
            }

            _logger.LogInformation(
                "Provisioned API key {KeyPrefix} for {Email} (Stripe session {SessionId})",
                apiKey.KeyPrefix, email, session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision API key for Stripe session {SessionId}", session.Id);
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
        }

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Test endpoint to verify email sending (Development only)
    /// </summary>
    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest request)
    {
        if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return NotFound();
        }

        try
        {
            await _emailService.SendApiKeyEmailAsync(
                request.Email,
                "ac_live_TEST_KEY_FOR_DEMO_ONLY",
                "AgentRails Implementation Kit (Test)");

            _logger.LogInformation("Test email sent to {Email}", request.Email);
            return Ok(new { success = true, message = $"Test email sent to {request.Email}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email to {Email}", request.Email);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}

public class TestEmailRequest
{
    public string Email { get; set; } = "";
}

public class CreateCheckoutRequest
{
    public string? Email { get; set; }
    /// <summary>
    /// Tier options:
    /// - "payg": Pay-as-you-go (0.5% per transaction, metered billing)
    /// - "pro": Pro subscription ($49/month, 0% transaction fees)
    /// - "enterprise": Enterprise license ($2,500 one-time, full source + policy engine)
    /// Defaults to enterprise.
    /// </summary>
    public string? Tier { get; set; }
}
