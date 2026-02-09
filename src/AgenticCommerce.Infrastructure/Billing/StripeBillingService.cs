using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace AgenticCommerce.Infrastructure.Billing;

/// <summary>
/// Stripe metered billing service for pay-as-you-go accounts.
/// Reports usage (0.5% fees) to Stripe for monthly invoicing.
/// </summary>
public class StripeBillingService : IStripeBillingService
{
    private readonly AgenticCommerceDbContext _db;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        AgenticCommerceDbContext db,
        IUsageTrackingService usageTrackingService,
        IConfiguration configuration,
        ILogger<StripeBillingService> logger)
    {
        _db = db;
        _usageTrackingService = usageTrackingService;
        _configuration = configuration;
        _logger = logger;

        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<string> CreateMeteredSubscriptionAsync(Guid organizationId)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        var meteredPriceId = _configuration["Stripe:MeteredPriceId"];
        if (string.IsNullOrEmpty(meteredPriceId))
        {
            throw new InvalidOperationException("Stripe:MeteredPriceId not configured");
        }

        // Ensure we have a Stripe customer
        if (string.IsNullOrEmpty(org.StripeCustomerId))
        {
            throw new InvalidOperationException("Organization does not have a Stripe customer ID");
        }

        try
        {
            var subscriptionService = new SubscriptionService();

            // Create subscription with metered billing
            var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
            {
                Customer = org.StripeCustomerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions
                    {
                        Price = meteredPriceId
                    }
                },
                // Don't charge immediately - bill at end of billing period
                PaymentBehavior = "default_incomplete",
                // Collect payment method if needed
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Metadata = new Dictionary<string, string>
                {
                    { "organization_id", org.Id.ToString() },
                    { "tier", OrganizationTiers.PayAsYouGo }
                }
            });

            // Store subscription info
            org.StripeSubscriptionId = subscription.Id;
            org.StripeSubscriptionItemId = subscription.Items.Data.FirstOrDefault()?.Id;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Created metered subscription {SubscriptionId} for org {OrgId}",
                subscription.Id, organizationId);

            return subscription.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe subscription for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<int> ReportUsageAsync(Guid organizationId)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null)
        {
            throw new InvalidOperationException("Organization not found");
        }

        if (string.IsNullOrEmpty(org.StripeCustomerId))
        {
            _logger.LogWarning("Org {OrgId} has no Stripe customer ID, skipping usage report", organizationId);
            return 0;
        }

        // Get the meter event name from configuration
        var meterEventName = _configuration["Stripe:MeterEventName"];
        if (string.IsNullOrEmpty(meterEventName))
        {
            _logger.LogWarning("Stripe:MeterEventName not configured, skipping usage report");
            return 0;
        }

        // Get unbilled usage
        var unbilledUsage = await _usageTrackingService.GetUnbilledUsageAsync(organizationId);
        var usageList = unbilledUsage.ToList();

        if (!usageList.Any())
        {
            _logger.LogDebug("No unbilled usage for org {OrgId}", organizationId);
            return 0;
        }

        try
        {
            var meterEventService = new Stripe.Billing.MeterEventService();
            var reportedCount = 0;

            // Report each usage event to Stripe using Meter Events
            // We report the fee amount in cents (smallest unit)
            foreach (var usage in usageList)
            {
                // Convert fee to cents (USD has 2 decimal places)
                // FeeAmount is already in USD, multiply by 100 for cents
                var feeInCents = (long)Math.Round(usage.FeeAmount * 100);

                if (feeInCents <= 0)
                {
                    // Skip zero-fee events (Pro tier)
                    continue;
                }

                await meterEventService.CreateAsync(new Stripe.Billing.MeterEventCreateOptions
                {
                    EventName = meterEventName,
                    Payload = new Dictionary<string, string>
                    {
                        { "value", feeInCents.ToString() },
                        { "stripe_customer_id", org.StripeCustomerId }
                    },
                    Identifier = usage.Id.ToString(), // Idempotency key
                    Timestamp = usage.RecordedAt
                });

                reportedCount++;
            }

            // Mark all as billed
            await _usageTrackingService.MarkAsBilledAsync(usageList.Select(u => u.Id));

            _logger.LogInformation(
                "Reported {Count} meter events to Stripe for org {OrgId}",
                reportedCount, organizationId);

            return reportedCount;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to report usage to Stripe for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task<int> ReportAllUsageAsync()
    {
        // Get all pay-as-you-go organizations with active subscriptions
        var paygOrgs = await _db.Organizations
            .Where(o => o.Tier == OrganizationTiers.PayAsYouGo &&
                        o.StripeSubscriptionItemId != null)
            .ToListAsync();

        var totalReported = 0;

        foreach (var org in paygOrgs)
        {
            try
            {
                var count = await ReportUsageAsync(org.Id);
                totalReported += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to report usage for org {OrgId}", org.Id);
                // Continue with other organizations
            }
        }

        _logger.LogInformation(
            "Reported {Total} usage records across {OrgCount} organizations",
            totalReported, paygOrgs.Count);

        return totalReported;
    }

    public async Task CancelSubscriptionAsync(Guid organizationId)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null || string.IsNullOrEmpty(org.StripeSubscriptionId))
        {
            return;
        }

        try
        {
            var subscriptionService = new SubscriptionService();
            await subscriptionService.CancelAsync(org.StripeSubscriptionId);

            org.StripeSubscriptionId = null;
            org.StripeSubscriptionItemId = null;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Cancelled Stripe subscription for org {OrgId}",
                organizationId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel Stripe subscription for org {OrgId}", organizationId);
            throw;
        }
    }

    public async Task HandleInvoicePaidAsync(string subscriptionId, string invoiceId)
    {
        var org = await _db.Organizations
            .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscriptionId);

        if (org == null)
        {
            _logger.LogWarning("No organization found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        _logger.LogInformation(
            "Invoice {InvoiceId} paid for org {OrgId} (subscription {SubscriptionId})",
            invoiceId, org.Id, subscriptionId);

        // Usage is already marked as billed when reported, nothing more to do
    }

    public async Task HandleInvoicePaymentFailedAsync(string subscriptionId, string invoiceId)
    {
        var org = await _db.Organizations
            .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscriptionId);

        if (org == null)
        {
            _logger.LogWarning("No organization found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        _logger.LogWarning(
            "Invoice {InvoiceId} payment FAILED for org {OrgId} (subscription {SubscriptionId})",
            invoiceId, org.Id, subscriptionId);

        // TODO: Could send email notification, restrict access, etc.
        // For now, just log - Stripe will retry and eventually cancel
    }

    public async Task HandleSubscriptionDeletedAsync(string subscriptionId)
    {
        var org = await _db.Organizations
            .FirstOrDefaultAsync(o => o.StripeSubscriptionId == subscriptionId);

        if (org == null)
        {
            _logger.LogWarning("No organization found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        // Downgrade to sandbox tier
        org.Tier = OrganizationTiers.Sandbox;
        org.StripeSubscriptionId = null;
        org.StripeSubscriptionItemId = null;
        await _db.SaveChangesAsync();

        _logger.LogWarning(
            "Subscription {SubscriptionId} deleted, downgraded org {OrgId} to sandbox",
            subscriptionId, org.Id);

        // TODO: Revoke mainnet API keys, send notification email
    }
}
