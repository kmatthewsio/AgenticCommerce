using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Service for tracking usage and calculating billing
/// </summary>
public interface IUsageTrackingService
{
    /// <summary>
    /// Record a transaction for billing purposes
    /// </summary>
    Task<UsageEvent> RecordTransactionAsync(
        Guid organizationId,
        Guid? apiKeyId,
        decimal transactionAmountUsdc,
        string? paymentId = null);

    /// <summary>
    /// Get unbilled usage for an organization
    /// </summary>
    Task<IEnumerable<UsageEvent>> GetUnbilledUsageAsync(Guid organizationId);

    /// <summary>
    /// Get total unbilled fees for an organization
    /// </summary>
    Task<decimal> GetUnbilledFeesAsync(Guid organizationId);

    /// <summary>
    /// Mark usage events as billed (called after Stripe billing)
    /// </summary>
    Task MarkAsBilledAsync(IEnumerable<Guid> usageEventIds);

    /// <summary>
    /// Get usage summary for an organization for a given period
    /// </summary>
    Task<UsageSummary> GetUsageSummaryAsync(Guid organizationId, DateTime from, DateTime to);
}

public class UsageSummary
{
    public Guid OrganizationId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int TransactionCount { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal TotalFees { get; set; }
    public decimal BilledFees { get; set; }
    public decimal UnbilledFees { get; set; }
}
