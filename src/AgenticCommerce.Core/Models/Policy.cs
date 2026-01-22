using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for organization spending/access policy persistence.
/// This is the base/simple policy model - Enterprise has a more advanced Policy system.
/// </summary>
[Table("organization_policies")]
public class Policy
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

    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Maximum amount allowed per transaction (in USDC)
    /// </summary>
    [Column("max_transaction_amount")]
    public decimal? MaxTransactionAmount { get; set; }

    /// <summary>
    /// Maximum spending allowed per day (in USDC)
    /// </summary>
    [Column("daily_spending_limit")]
    public decimal? DailySpendingLimit { get; set; }

    /// <summary>
    /// Whether transactions require manual approval
    /// </summary>
    [Required]
    [Column("requires_approval")]
    public bool RequiresApproval { get; set; } = false;

    /// <summary>
    /// JSON array of allowed recipient addresses
    /// </summary>
    [Column("allowed_recipients_json")]
    public string? AllowedRecipientsJson { get; set; }

    /// <summary>
    /// Whether the policy is currently active
    /// </summary>
    [Required]
    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(OrganizationId))]
    public virtual Organization Organization { get; set; } = null!;

    // Helper to get/set allowed recipients as a list
    [NotMapped]
    public List<string> AllowedRecipients
    {
        get => string.IsNullOrEmpty(AllowedRecipientsJson)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(AllowedRecipientsJson) ?? new List<string>();
        set => AllowedRecipientsJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}
