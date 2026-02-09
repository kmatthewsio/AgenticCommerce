using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Tracks transaction usage for billing purposes
/// </summary>
[Table("usage_events")]
public class UsageEvent
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The API key used for this transaction (optional, for tracking)
    /// </summary>
    [Column("api_key_id")]
    public Guid? ApiKeyId { get; set; }

    /// <summary>
    /// The x402 payment ID if associated
    /// </summary>
    [Column("payment_id")]
    [MaxLength(255)]
    public string? PaymentId { get; set; }

    /// <summary>
    /// Transaction amount in USDC (6 decimals)
    /// </summary>
    [Required]
    [Column("transaction_amount")]
    public decimal TransactionAmount { get; set; }

    /// <summary>
    /// Fee amount (0.5% of transaction) in USD
    /// </summary>
    [Required]
    [Column("fee_amount")]
    public decimal FeeAmount { get; set; }

    /// <summary>
    /// Whether this has been reported to Stripe for billing
    /// </summary>
    [Required]
    [Column("billed")]
    public bool Billed { get; set; } = false;

    /// <summary>
    /// When this was billed to Stripe (if applicable)
    /// </summary>
    [Column("billed_at")]
    public DateTime? BilledAt { get; set; }

    [Required]
    [Column("recorded_at")]
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [ForeignKey(nameof(ApiKeyId))]
    public virtual ApiKey? ApiKey { get; set; }
}
