namespace AgenticCommerce.Core.Models;

/// <summary>
/// Represents an autonomous AI agent with spending capabilities
/// </summary>
public class Agent
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal CurrentBalance { get; set; }
    public string WalletId { get; set; } = string.Empty;
    public string WalletAddress { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Agent status
/// </summary>
public enum AgentStatus
{
    Created,
    Active,
    Busy,
    Paused,
    Completed,
    Failed
}

/// <summary>
/// Configuration for creating a new agent
/// </summary>
public class AgentConfig
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public List<string> Capabilities { get; set; } = new();
}

/// <summary>
/// Result of an agent task execution
/// </summary>
public class AgentRunResult
{
    public string AgentId { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Result { get; set; } = string.Empty;
    public decimal AmountSpent { get; set; }
    public List<string> TransactionIds { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Purchase request from an agent
/// </summary>
public class PurchaseRequest
{
    public string RecipientAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Result of a purchase
/// </summary>
public class PurchaseResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public decimal AmountSpent { get; set; }
    public decimal RemainingBalance { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Agent information summary
/// </summary>
public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal CurrentBalance { get; set; }
    public AgentStatus Status { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalSpent { get; set; }
}