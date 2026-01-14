using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// Marks an endpoint as requiring x402 payment.
/// Apply to controller actions to automatically handle 402 responses.
///
/// Usage:
///   [X402Payment(0.01)] // Requires $0.01 USDC
///   [X402Payment(0.001, Description = "Data API call")] // With description
///   [X402Payment(0.05, Network = "base-sepolia")] // Specific network
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class X402PaymentAttribute : Attribute, IFilterFactory
{
    /// <summary>
    /// Amount required in USDC (e.g., 0.01 for one cent)
    /// </summary>
    public double AmountUsdc { get; }

    /// <summary>
    /// Human-readable description of what the payment is for
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Blockchain network to accept payment on (default: arc-testnet)
    /// </summary>
    public string Network { get; set; } = "arc-testnet";

    /// <summary>
    /// Whether to also accept payments on alternative networks
    /// </summary>
    public bool AllowMultipleNetworks { get; set; } = true;

    /// <summary>
    /// Alternative networks to accept (comma-separated)
    /// Only used if AllowMultipleNetworks is true
    /// </summary>
    public string? AlternativeNetworks { get; set; } = "base-sepolia";

    public bool IsReusable => false;

    public X402PaymentAttribute(double amountUsdc)
    {
        if (amountUsdc <= 0)
            throw new ArgumentException("Amount must be greater than 0", nameof(amountUsdc));

        AmountUsdc = amountUsdc;
    }

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filter = serviceProvider.GetRequiredService<X402PaymentFilter>();
        filter.Configure(this);
        return filter;
    }
}
