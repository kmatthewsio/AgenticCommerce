using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for x402 payment persistence
/// Tracks all payments made through the x402 protocol
/// </summary>
[Table("x402_payments")]
public class X402PaymentEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>
    /// Unique payment ID (for idempotency)
    /// </summary>
    [Required]
    [Column("payment_id")]
    [MaxLength(100)]
    public string PaymentId { get; set; } = string.Empty;

    /// <summary>
    /// Resource/endpoint that was paid for
    /// </summary>
    [Required]
    [Column("resource")]
    [MaxLength(500)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Payment scheme (e.g., "exact")
    /// </summary>
    [Required]
    [Column("scheme")]
    [MaxLength(50)]
    public string Scheme { get; set; } = "exact";

    /// <summary>
    /// Blockchain network (e.g., "arc-testnet", "base-sepolia")
    /// </summary>
    [Required]
    [Column("network")]
    [MaxLength(50)]
    public string Network { get; set; } = string.Empty;

    /// <summary>
    /// Amount in USDC (human-readable, e.g., 0.01)
    /// </summary>
    [Required]
    [Column("amount_usdc", TypeName = "decimal(18,8)")]
    public decimal AmountUsdc { get; set; }

    /// <summary>
    /// Amount in smallest unit (e.g., 10000 for 0.01 USDC)
    /// </summary>
    [Required]
    [Column("amount_smallest_unit")]
    public long AmountSmallestUnit { get; set; }

    /// <summary>
    /// Payer wallet address
    /// </summary>
    [Required]
    [Column("payer_address")]
    [MaxLength(100)]
    public string PayerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Recipient wallet address
    /// </summary>
    [Required]
    [Column("recipient_address")]
    [MaxLength(100)]
    public string RecipientAddress { get; set; } = string.Empty;

    /// <summary>
    /// EIP-3009 nonce (for replay prevention)
    /// </summary>
    [Column("nonce")]
    [MaxLength(100)]
    public string? Nonce { get; set; }

    /// <summary>
    /// Blockchain transaction hash
    /// </summary>
    [Column("transaction_hash")]
    [MaxLength(100)]
    public string? TransactionHash { get; set; }

    /// <summary>
    /// Payment status: Pending, Verified, Settled, Failed
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Description of what was paid for
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Client IP address (for analytics)
    /// </summary>
    [Column("client_ip")]
    [MaxLength(50)]
    public string? ClientIp { get; set; }

    /// <summary>
    /// User agent (for analytics)
    /// </summary>
    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("settled_at")]
    public DateTime? SettledAt { get; set; }
}

/// <summary>
/// x402 payment status constants
/// </summary>
public static class X402PaymentStatus
{
    public const string Pending = "Pending";
    public const string Verified = "Verified";
    public const string Settled = "Settled";
    public const string Failed = "Failed";
}
