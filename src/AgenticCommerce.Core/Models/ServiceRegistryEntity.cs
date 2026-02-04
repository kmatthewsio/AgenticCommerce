using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Service registry entry for trust layer - allows services to register
/// and be verified for agent-to-agent commerce.
/// </summary>
[Table("service_registry")]
public class ServiceRegistryEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>
    /// The API endpoint URL (unique identifier for the service)
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("service_url")]
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable service name
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what the service does
    /// </summary>
    [MaxLength(1000)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Wallet address of the service owner
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("owner_wallet")]
    public string OwnerWallet { get; set; } = string.Empty;

    /// <summary>
    /// Typical price per request in USDC
    /// </summary>
    [Column("price_usdc", TypeName = "decimal(18,8)")]
    public decimal? PriceUsdc { get; set; }

    /// <summary>
    /// Whether AgentRails has verified this service
    /// </summary>
    [Column("verified")]
    public bool Verified { get; set; } = false;

    /// <summary>
    /// When the service was verified
    /// </summary>
    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// When the service was registered
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the service was last updated
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
