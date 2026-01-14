using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// x402 V2 spec-compliant payment service
/// </summary>
public interface IX402Service
{
    /// <summary>
    /// Create a spec-compliant payment requirement for a resource
    /// </summary>
    X402PaymentRequired CreatePaymentRequired(
        string resource,
        decimal amountUsdc,
        string description,
        string network = X402Networks.ArcTestnet);

    /// <summary>
    /// Encode payment required to Base64 for header
    /// </summary>
    string EncodePaymentRequired(X402PaymentRequired paymentRequired);

    /// <summary>
    /// Decode payment payload from Base64 header
    /// </summary>
    X402PaymentPayload? DecodePaymentPayload(string base64Payload);

    /// <summary>
    /// Verify a payment payload against requirements
    /// </summary>
    Task<X402VerifyResponse> VerifyPaymentAsync(
        X402PaymentPayload payload,
        X402PaymentRequirement requirement);

    /// <summary>
    /// Settle a verified payment (execute blockchain transfer)
    /// </summary>
    Task<X402SettleResponse> SettlePaymentAsync(
        X402PaymentPayload payload,
        X402PaymentRequirement requirement);

    /// <summary>
    /// Get the recipient address for payments
    /// </summary>
    string GetPayToAddress();
}
