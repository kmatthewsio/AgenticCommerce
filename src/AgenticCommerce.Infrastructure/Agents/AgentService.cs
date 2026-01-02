using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AgenticCommerce.Infrastructure.Agents;

/// <summary>
/// Service for managing autonomous AI agents with Circle wallets and Arc blockchain
/// </summary>
public class AgentService : IAgentService
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<AgentService> _logger;

    // In-memory storage (for prototype - replace with database later)
    private readonly ConcurrentDictionary<string, Agent> _agents = new();
    private readonly ConcurrentDictionary<string, List<string>> _agentTransactions = new();

    public AgentService(IArcClient arcClient, ILogger<AgentService> logger)
    {
        _arcClient = arcClient;
        _logger = logger;
    }

    public Task<Agent> CreateAgentAsync(AgentConfig config)
    {
        try
        {
            // Generate unique agent ID
            var agentId = $"agent_{Guid.NewGuid():N}";

            // For prototype, agents share the main wallet
            // In production, each agent would have its own Circle wallet
            var walletAddress = _arcClient.GetAddress();

            var agent = new Agent
            {
                Id = agentId,
                Name = config.Name,
                Description = config.Description,
                Budget = config.Budget,
                CurrentBalance = config.Budget,
                WalletAddress = walletAddress,
                WalletId = agentId, // Placeholder for now
                Status = AgentStatus.Created,
                CreatedAt = DateTime.UtcNow,
                Capabilities = config.Capabilities
            };

            _agents[agentId] = agent;
            _agentTransactions[agentId] = new List<string>();

            _logger.LogInformation(
                "Created agent {AgentId} ({Name}) with budget ${Budget}",
                agentId, config.Name, config.Budget);

            return Task.FromResult(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent");
            throw;
        }
    }

    public async Task<AgentRunResult> RunAgentAsync(string agentId, string task)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            if (!_agents.TryGetValue(agentId, out var agent))
            {
                throw new Exception($"Agent {agentId} not found");
            }

            _logger.LogInformation("Agent {AgentId} starting task: {Task}", agentId, task);

            // Update agent status
            agent.Status = AgentStatus.Busy;
            agent.LastActiveAt = DateTime.UtcNow;

            // For prototype: Simple AI simulation
            // In production: This would call Azure OpenAI with Microsoft Agent Framework
            var result = await SimulateAgentTaskAsync(agent, task);

            // Update agent status
            agent.Status = AgentStatus.Active;

            _logger.LogInformation(
                "Agent {AgentId} completed task. Spent: ${Amount}",
                agentId, result.AmountSpent);

            return new AgentRunResult
            {
                AgentId = agentId,
                TaskDescription = task,
                Success = true,
                Result = result.Result,
                AmountSpent = result.AmountSpent,
                TransactionIds = result.TransactionIds,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} failed task", agentId);

            if (_agents.TryGetValue(agentId, out var agent))
            {
                agent.Status = AgentStatus.Failed;
            }

            return new AgentRunResult
            {
                AgentId = agentId,
                TaskDescription = task,
                Success = false,
                ErrorMessage = ex.Message,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<PurchaseResult> MakePurchaseAsync(string agentId, PurchaseRequest request)
    {
        try
        {
            if (!_agents.TryGetValue(agentId, out var agent))
            {
                throw new Exception($"Agent {agentId} not found");
            }

            _logger.LogInformation(
                "Agent {AgentId} attempting purchase: ${Amount} to {Recipient}",
                agentId, request.Amount, request.RecipientAddress);

            // Check budget
            if (request.Amount > agent.CurrentBalance)
            {
                _logger.LogWarning(
                    "Agent {AgentId} insufficient balance. Requested: ${Amount}, Available: ${Balance}",
                    agentId, request.Amount, agent.CurrentBalance);

                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient balance. Requested: ${request.Amount}, Available: ${agent.CurrentBalance}"
                };
            }

            // Execute purchase via Arc
            var txId = await _arcClient.SendUsdcAsync(
                request.RecipientAddress,
                request.Amount);

            // Update agent balance
            agent.CurrentBalance -= request.Amount;
            agent.LastActiveAt = DateTime.UtcNow;

            // Track transaction
            if (_agentTransactions.TryGetValue(agentId, out var transactions))
            {
                transactions.Add(txId);
            }

            _logger.LogInformation(
                "Agent {AgentId} purchase successful. TX: {TxId}, Remaining: ${Balance}",
                agentId, txId, agent.CurrentBalance);

            return new PurchaseResult
            {
                Success = true,
                TransactionId = txId,
                AmountSpent = request.Amount,
                RemainingBalance = agent.CurrentBalance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentId} purchase failed", agentId);

            return new PurchaseResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<Agent?> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<AgentInfo?> GetAgentInfoAsync(string agentId)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
        {
            return Task.FromResult<AgentInfo?>(null);
        }

        var transactionCount = _agentTransactions.TryGetValue(agentId, out var txs)
            ? txs.Count
            : 0;

        var info = new AgentInfo
        {
            Id = agent.Id,
            Name = agent.Name,
            Budget = agent.Budget,
            CurrentBalance = agent.CurrentBalance,
            Status = agent.Status,
            TotalTransactions = transactionCount,
            TotalSpent = agent.Budget - agent.CurrentBalance
        };

        return Task.FromResult<AgentInfo?>(info);
    }

    public Task<List<AgentInfo>> ListAgentsAsync()
    {
        var agentInfos = _agents.Values.Select(agent =>
        {
            var transactionCount = _agentTransactions.TryGetValue(agent.Id, out var txs)
                ? txs.Count
                : 0;

            return new AgentInfo
            {
                Id = agent.Id,
                Name = agent.Name,
                Budget = agent.Budget,
                CurrentBalance = agent.CurrentBalance,
                Status = agent.Status,
                TotalTransactions = transactionCount,
                TotalSpent = agent.Budget - agent.CurrentBalance
            };
        }).ToList();

        return Task.FromResult(agentInfos);
    }

    public Task<bool> DeleteAgentAsync(string agentId)
    {
        var removed = _agents.TryRemove(agentId, out _);
        if (removed)
        {
            _agentTransactions.TryRemove(agentId, out _);
            _logger.LogInformation("Deleted agent {AgentId}", agentId);
        }
        return Task.FromResult(removed);
    }

    /// <summary>
    /// Simulate agent task execution (replace with real AI later)
    /// </summary>
    private Task<(string Result, decimal AmountSpent, List<string> TransactionIds)> SimulateAgentTaskAsync(
        Agent agent,
        string task)
    {
        // For prototype: Return simulation
        // In production: Call Azure OpenAI with Agent Framework

        _logger.LogInformation("Simulating AI task execution for agent {AgentId}", agent.Id);

        var result = $"[SIMULATED] Agent '{agent.Name}' analyzed task: '{task}'. " +
                    $"Budget: ${agent.CurrentBalance}. " +
                    $"Ready to execute purchases within budget.";

        return Task.FromResult((result, 0m, new List<string>()));
    }
}