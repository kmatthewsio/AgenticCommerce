using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for Agent persistence
/// </summary>
[Table("agents")]
public class AgentEntity
{
    [Key]
    [Column("id")]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column("budget", TypeName = "decimal(18,8)")]
    public decimal Budget { get; set; }

    [Required]
    [Column("current_balance", TypeName = "decimal(18,8)")]
    public decimal CurrentBalance { get; set; }

    [Required]
    [Column("wallet_address")]
    [MaxLength(100)]
    public string WalletAddress { get; set; } = string.Empty;

    [Required]
    [Column("wallet_id")]
    [MaxLength(100)]
    public string WalletId { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    [Column("capabilities")]
    [MaxLength(500)]
    public string? CapabilitiesJson { get; set; } // JSON array

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_active_at")]
    public DateTime? LastActiveAt { get; set; }

    [Column("metadata")]
    public string? MetadataJson { get; set; } // JSON object

    // Navigation property
    public virtual ICollection<TransactionEntity> Transactions { get; set; } = new List<TransactionEntity>();
}

/// <summary>
/// Database entity for Transaction persistence
/// </summary>
[Table("transactions")]
public class TransactionEntity
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("agent_id")]
    [MaxLength(100)]
    public string AgentId { get; set; } = string.Empty;

    [Required]
    [Column("transaction_id")]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [Column("amount", TypeName = "decimal(18,8)")]
    public decimal Amount { get; set; }

    [Column("recipient_address")]
    [MaxLength(100)]
    public string? RecipientAddress { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    // Navigation property
    public virtual AgentEntity Agent { get; set; } = null!;
}