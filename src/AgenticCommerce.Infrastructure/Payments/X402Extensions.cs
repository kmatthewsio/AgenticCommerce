using AgenticCommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// Extension methods for x402 payment integration
/// </summary>
public static class X402Extensions
{
    /// <summary>
    /// Adds x402 payment services to the service collection.
    /// Call this in Program.cs to enable x402 payments.
    ///
    /// Usage:
    ///   builder.Services.AddX402Payments();
    /// </summary>
    public static IServiceCollection AddX402Payments(this IServiceCollection services)
    {
        // EIP-3009 signature verifier for cryptographic validation
        services.AddSingleton<IEip3009SignatureVerifier, Eip3009SignatureVerifier>();

        // EIP-3009 signer for creating signed authorizations
        services.AddSingleton<IEip3009Signer, Eip3009Signer>();

        // Core x402 service
        services.AddSingleton<IX402Service, X402Service>();

        // x402 client for making paid API requests
        services.AddTransient<X402Client>();

        // Filter for attribute-based payments (must be transient for per-request configuration)
        services.AddTransient<X402PaymentFilter>();

        return services;
    }

    /// <summary>
    /// Gets the payer address from a paid request.
    /// Use in controller actions protected by [X402Payment] to get who paid.
    /// </summary>
    public static string? GetX402Payer(this HttpContext context)
    {
        return context.Items.TryGetValue("X402_Payer", out var payer)
            ? payer as string
            : null;
    }

    /// <summary>
    /// Gets the transaction hash from a paid request.
    /// </summary>
    public static string? GetX402TransactionHash(this HttpContext context)
    {
        return context.Items.TryGetValue("X402_TxHash", out var txHash)
            ? txHash as string
            : null;
    }

    /// <summary>
    /// Gets the amount paid in USDC from a paid request.
    /// </summary>
    public static decimal GetX402AmountPaid(this HttpContext context)
    {
        return context.Items.TryGetValue("X402_Amount", out var amount)
            ? (decimal)amount
            : 0m;
    }

    /// <summary>
    /// Checks if the current request was paid via x402.
    /// </summary>
    public static bool IsX402Paid(this HttpContext context)
    {
        return context.Items.ContainsKey("X402_Payer");
    }
}
