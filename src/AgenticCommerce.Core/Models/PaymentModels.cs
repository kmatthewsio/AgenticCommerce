namespace AgenticCommerce.Core.Models;

/// <summary>
/// Represents a payment requirement (HTTP 402 response)
/// </summary>
public class PaymentRequirement
{
    /// <summary>
    /// Unique identifier for this payment request
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Amount required in USDC
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Recipient address for payment
    /// </summary>
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the payment is for
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// API endpoint this payment is for
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Expiration time for this payment request
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Optional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Represents proof that payment was made
/// </summary>
public class PaymentProof
{
    /// <summary>
    /// Payment ID this proof is for
    /// </summary>
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Arc blockchain transaction ID
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Amount paid in USDC
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Sender address (payer)
    /// </summary>
    public string SenderAddress { get; set; } = string.Empty;

    /// <summary>
    /// Recipient address (payee)
    /// </summary>
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when payment was made
    /// </summary>
    public DateTime PaidAt { get; set; }

    /// <summary>
    /// Optional proof signature
    /// </summary>
    public string? Signature { get; set; }
}

/// <summary>
/// Result of payment verification
/// </summary>
public class PaymentVerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public PaymentProof? Proof { get; set; }
    public DateTime? VerifiedAt { get; set; }
}