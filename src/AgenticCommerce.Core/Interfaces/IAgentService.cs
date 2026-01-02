using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Service for managing autonomous AI agents with spending capabilities
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Create a new autonomous agent with a budget
    /// </summary>
    Task<Agent> CreateAgentAsync(AgentConfig config);

    /// <summary>
    /// Run an agent to perform a task
    /// </summary>
    Task<AgentRunResult> RunAgentAsync(string agentId, string task);

    /// <summary>
    /// Have an agent make a purchase
    /// </summary>
    Task<PurchaseResult> MakePurchaseAsync(string agentId, PurchaseRequest request);

    /// <summary>
    /// Get agent status and current state
    /// </summary>
    Task<Agent?> GetAgentAsync(string agentId);

    /// <summary>
    /// Get agent summary information
    /// </summary>
    Task<AgentInfo?> GetAgentInfoAsync(string agentId);

    /// <summary>
    /// List all agents
    /// </summary>
    Task<List<AgentInfo>> ListAgentsAsync();

    /// <summary>
    /// Delete an agent
    /// </summary>
    Task<bool> DeleteAgentAsync(string agentId);
}