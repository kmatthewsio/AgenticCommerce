using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Database entity for Organization (tenant) persistence
/// </summary>
[Table("organizations")]
public class Organization
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("slug")]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Pricing tier: sandbox, payg, pro, enterprise
    /// </summary>
    [Required]
    [Column("tier")]
    [MaxLength(50)]
    public string Tier { get; set; } = OrganizationTiers.Sandbox;

    /// <summary>
    /// Stripe customer ID for billing
    /// </summary>
    [Column("stripe_customer_id")]
    [MaxLength(255)]
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Stripe subscription ID for usage-based billing
    /// </summary>
    [Column("stripe_subscription_id")]
    [MaxLength(255)]
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Stripe subscription item ID for reporting metered usage
    /// </summary>
    [Column("stripe_subscription_item_id")]
    [MaxLength(255)]
    public string? StripeSubscriptionItemId { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<AgentEntity> Agents { get; set; } = new List<AgentEntity>();
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
    public virtual ICollection<UsageEvent> UsageEvents { get; set; } = new List<UsageEvent>();
}

/// <summary>
/// Organization pricing tiers
/// </summary>
public static class OrganizationTiers
{
    /// <summary>Testnet only, free</summary>
    public const string Sandbox = "sandbox";

    /// <summary>Pay-as-you-go, 0.5% per transaction</summary>
    public const string PayAsYouGo = "payg";

    /// <summary>$49/mo unlimited transactions</summary>
    public const string Pro = "pro";

    /// <summary>$2,500 one-time, self-hosted</summary>
    public const string Enterprise = "enterprise";
}
