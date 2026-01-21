using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for User persistence
/// </summary>
[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(255)]
    public string? Name { get; set; }

    [Required]
    [Column("role")]
    [MaxLength(50)]
    public string Role { get; set; } = "member";

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

/// <summary>
/// User roles for authorization
/// </summary>
public static class UserRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Member = "member";
}
