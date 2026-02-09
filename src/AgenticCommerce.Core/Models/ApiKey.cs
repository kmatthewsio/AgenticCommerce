using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for API key persistence
/// </summary>
[Table("api_keys")]
public class ApiKey
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The hashed API key. The raw key is only shown once at creation.
    /// </summary>
    [Required]
    [Column("key_hash")]
    [MaxLength(255)]
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>
    /// The key prefix for display (e.g., "ar_...abc123")
    /// </summary>
    [Required]
    [Column("key_prefix")]
    [MaxLength(20)]
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Environment: testnet or mainnet
    /// </summary>
    [Required]
    [Column("environment")]
    [MaxLength(50)]
    public string Environment { get; set; } = ApiKeyEnvironments.Testnet;

    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    [NotMapped]
    public bool IsActive => RevokedAt == null;

    [NotMapped]
    public string MaskedKey => $"{KeyPrefix}...";

    [NotMapped]
    public bool IsMainnet => Environment == ApiKeyEnvironments.Mainnet;
}

/// <summary>
/// API key environment types
/// </summary>
public static class ApiKeyEnvironments
{
    public const string Testnet = "testnet";
    public const string Mainnet = "mainnet";
}
