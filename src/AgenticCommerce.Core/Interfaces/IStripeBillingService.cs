namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Service for Stripe metered billing integration
/// </summary>
public interface IStripeBillingService
{
    /// <summary>
    /// Create a metered subscription for pay-as-you-go billing
    /// </summary>
    /// <param name="organizationId">Organization to create subscription for</param>
    /// <returns>Stripe subscription ID</returns>
    Task<string> CreateMeteredSubscriptionAsync(Guid organizationId);

    /// <summary>
    /// Report unbilled usage to Stripe for an organization
    /// </summary>
    /// <param name="organizationId">Organization to report usage for</param>
    /// <returns>Number of usage records reported</returns>
    Task<int> ReportUsageAsync(Guid organizationId);

    /// <summary>
    /// Report unbilled usage to Stripe for all pay-as-you-go organizations
    /// </summary>
    /// <returns>Total number of usage records reported</returns>
    Task<int> ReportAllUsageAsync();

    /// <summary>
    /// Cancel a subscription (e.g., when downgrading or deleting account)
    /// </summary>
    Task CancelSubscriptionAsync(Guid organizationId);

    /// <summary>
    /// Handle successful invoice payment - mark usage as billed
    /// </summary>
    Task HandleInvoicePaidAsync(string subscriptionId, string invoiceId);

    /// <summary>
    /// Handle failed invoice payment
    /// </summary>
    Task HandleInvoicePaymentFailedAsync(string subscriptionId, string invoiceId);

    /// <summary>
    /// Handle subscription cancellation/deletion
    /// </summary>
    Task HandleSubscriptionDeletedAsync(string subscriptionId);
}
