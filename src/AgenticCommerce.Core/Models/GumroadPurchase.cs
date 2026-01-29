using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Tracks Gumroad purchases and links them to organizations/API keys
/// </summary>
[Table("gumroad_purchases")]
public class GumroadPurchase
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("sale_id")]
    [MaxLength(100)]
    public string SaleId { get; set; } = string.Empty;

    [Required]
    [Column("product_id")]
    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [Column("product_name")]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("license_key")]
    [MaxLength(100)]
    public string? LicenseKey { get; set; }

    [Column("price_cents")]
    public int PriceCents { get; set; }

    [Column("currency")]
    [MaxLength(10)]
    public string Currency { get; set; } = "usd";

    [Column("refunded")]
    public bool Refunded { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("api_key_id")]
    public Guid? ApiKeyId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("raw_payload")]
    public string? RawPayload { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization? Organization { get; set; }

    [ForeignKey(nameof(ApiKeyId))]
    public virtual ApiKey? ApiKey { get; set; }
}
