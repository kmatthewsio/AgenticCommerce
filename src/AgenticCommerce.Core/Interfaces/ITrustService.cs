using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Service for checking trust and reputation of x402 services
/// </summary>
public interface ITrustService
{
    /// <summary>
    /// Check trust level for a service URL (combines registry + payment history)
    /// </summary>
    Task<TrustCheckResult> CheckTrustAsync(string serviceUrl);

    /// <summary>
    /// Get service details from the registry
    /// </summary>
    Task<ServiceRegistryEntity?> GetServiceAsync(string serviceUrl);

    /// <summary>
    /// Register a new service in the registry
    /// </summary>
    Task<ServiceRegistryEntity> RegisterServiceAsync(RegisterServiceRequest request);

    /// <summary>
    /// List registered services with optional filtering
    /// </summary>
    Task<List<ServiceRegistryEntity>> ListServicesAsync(bool? verifiedOnly = null, int limit = 50);

    /// <summary>
    /// Mark a service as verified (admin operation)
    /// </summary>
    Task<ServiceRegistryEntity?> VerifyServiceAsync(string serviceUrl);

    /// <summary>
    /// Get payment statistics for a service URL from x402 payments
    /// </summary>
    Task<ServicePaymentStats> GetPaymentStatsAsync(string serviceUrl);
}

/// <summary>
/// Result of a trust check combining registry and payment data
/// </summary>
public class TrustCheckResult
{
    public string ServiceUrl { get; set; } = string.Empty;
    public bool Registered { get; set; }
    public bool Verified { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? OwnerWallet { get; set; }
    public ServicePaymentStats Stats { get; set; } = new();
    public string TrustScore { get; set; } = "unknown"; // high, medium, low, unknown
}

/// <summary>
/// Payment statistics for a service
/// </summary>
public class ServicePaymentStats
{
    public int TotalPayments { get; set; }
    public int SettledPayments { get; set; }
    public int FailedPayments { get; set; }
    public decimal SuccessRate { get; set; }
    public decimal TotalVolumeUsdc { get; set; }
    public DateTime? FirstPayment { get; set; }
    public DateTime? LastPayment { get; set; }
}

/// <summary>
/// Request to register a new service
/// </summary>
public class RegisterServiceRequest
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string OwnerWallet { get; set; } = string.Empty;
    public decimal? PriceUsdc { get; set; }
}
