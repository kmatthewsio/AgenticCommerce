using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Records a Stripe checkout session purchase and links to provisioned resources
/// </summary>
[Table("stripe_purchases")]
public class StripePurchase
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Stripe Checkout Session ID (cs_xxx)
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("session_id")]
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Stripe Payment Intent ID (pi_xxx)
    /// </summary>
    [MaxLength(255)]
    [Column("payment_intent_id")]
    public string? PaymentIntentId { get; set; }

    /// <summary>
    /// Stripe Customer ID (cus_xxx)
    /// </summary>
    [MaxLength(255)]
    [Column("customer_id")]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Customer email from Stripe
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Product name at time of purchase
    /// </summary>
    [Required]
    [MaxLength(255)]
    [Column("product_name")]
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Amount paid in cents
    /// </summary>
    [Column("amount_cents")]
    public int AmountCents { get; set; }

    /// <summary>
    /// Currency code (usd, etc.)
    /// </summary>
    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "usd";

    /// <summary>
    /// Whether this purchase has been refunded
    /// </summary>
    [Column("refunded")]
    public bool Refunded { get; set; }

    /// <summary>
    /// The organization created for this purchase
    /// </summary>
    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [ForeignKey(nameof(OrganizationId))]
    public Organization? Organization { get; set; }

    /// <summary>
    /// The API key provisioned for this purchase
    /// </summary>
    [Column("api_key_id")]
    public Guid? ApiKeyId { get; set; }

    [ForeignKey(nameof(ApiKeyId))]
    public ApiKey? ApiKey { get; set; }

    /// <summary>
    /// Timestamp of purchase
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Raw Stripe event JSON for audit trail
    /// </summary>
    [Column("raw_event")]
    public string? RawEvent { get; set; }
}
