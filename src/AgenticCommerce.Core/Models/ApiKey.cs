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
}
