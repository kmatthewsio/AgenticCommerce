using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Collections.Concurrent;
using static AgenticCommerce.Infrastructure.Agents.PaymentPlugin;

namespace AgenticCommerce.Infrastructure.Agents;

/// <summary>
/// Service for managing autonomous AI agents with real reasoning capabilities
/// </summary>
public class AgentService : IAgentService
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<AgentService> _logger;
    private readonly AIOptions _aiOptions;
    private readonly Kernel? _kernel;

    private readonly ConcurrentDictionary<string, Agent> _agents = new();
    private readonly ConcurrentDictionary<string, List<string>> _agentTransactions = new();

    public AgentService(
        IArcClient arcClient,
        ILogger<AgentService> logger,
        IOptions<AIOptions> aiOptions)
    {
        _arcClient = arcClient;
        _logger = logger;
        _aiOptions = aiOptions.Value;

        // Initialize Semantic Kernel if API key is configured
        if (!string.IsNullOrEmpty(_aiOptions.OpenAIApiKey))
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(
                modelId: _aiOptions.OpenAIModel!,
                apiKey: _aiOptions.OpenAIApiKey!);

            // Add plugins
            builder.Plugins.AddFromObject(new PaymentPlugin(_arcClient), "Payment");
            builder.Plugins.AddFromObject(new ResearchPlugin(), "Research");

            _kernel = builder.Build();

            _logger.LogInformation("AgentService initialized with OpenAI ({Model})", _aiOptions.OpenAIModel);
        }
        else
        {
            _logger.LogWarning("No AI API key configured - agents will use simulation mode");
        }
    }

    public Task<Agent> CreateAgentAsync(AgentConfig config)
    {
        try
        {
            var agentId = $"agent_{Guid.NewGuid():N}";
            var walletAddress = _arcClient.GetAddress();

            var agent = new Agent
            {
                Id = agentId,
                Name = config.Name,
                Description = config.Description,
                Budget = config.Budget,
                CurrentBalance = config.Budget,
                WalletAddress = walletAddress,
                WalletId = agentId,
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

            agent.Status = AgentStatus.Busy;
            agent.LastActiveAt = DateTime.UtcNow;

            string result;
            decimal amountSpent = 0m;
            var transactionIds = new List<string>();

            if (_kernel != null)
            {
                // Execute with REAL AI
                _logger.LogInformation("Executing task with AI reasoning");
                (result, amountSpent, transactionIds) = await ExecuteWithAIAsync(agent, task);
            }
            else
            {
                // Fallback to simulation
                _logger.LogWarning("AI not configured - using simulation mode");
                result = $"[SIMULATED] Agent '{agent.Name}' analyzed task: '{task}'. Budget: ${agent.CurrentBalance:F2}. Ready to execute purchases.";
            }

            agent.Status = AgentStatus.Active;

            _logger.LogInformation(
                "Agent {AgentId} completed task. Result length: {Length} chars",
                agentId, result.Length);

            return new AgentRunResult
            {
                AgentId = agentId,
                TaskDescription = task,
                Success = true,
                Result = result,
                AmountSpent = amountSpent,
                TransactionIds = transactionIds,
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

            if (request.Amount > agent.CurrentBalance)
            {
                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient balance. Requested: ${request.Amount}, Available: ${agent.CurrentBalance}"
                };
            }

            var txId = await _arcClient.SendUsdcAsync(request.RecipientAddress, request.Amount);

            agent.CurrentBalance -= request.Amount;
            agent.LastActiveAt = DateTime.UtcNow;

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

        var transactionCount = _agentTransactions.TryGetValue(agentId, out var txs) ? txs.Count : 0;

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
            var transactionCount = _agentTransactions.TryGetValue(agent.Id, out var txs) ? txs.Count : 0;

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
    /// Execute agent task with real AI reasoning and tool use
    /// </summary>
    private async Task<(string Result, decimal AmountSpent, List<string> TransactionIds)> ExecuteWithAIAsync(
        Agent agent,
        string task)
    {
        var chatCompletion = _kernel!.GetRequiredService<IChatCompletionService>();

        var systemPrompt = $@"You are '{agent.Name}', an autonomous AI agent that can research and analyze options.

YOUR PROFILE:
- Name: {agent.Name}
- Description: {agent.Description}
- Budget: ${agent.CurrentBalance:F2} USDC
- Capabilities: {string.Join(", ", agent.Capabilities)}
- Wallet: {agent.WalletAddress}

YOUR TOOLS:
You have access to these functions:
- Payment.CheckBalance: Check current USDC balance
- Payment.GetWalletAddress: Get your wallet address
- Payment.CheckBudget: Verify if an amount is within budget
- Research.ResearchAIProviders: Get AI/LLM API pricing info
- Research.ResearchImageProviders: Get image generation API pricing
- Research.CompareServices: Compare two service options
- Research.CalculateCosts: Calculate usage cost estimates

YOUR TASK:
Analyze the user's request thoroughly:
1. Use your research tools to gather information
2. Compare options and calculate costs
3. Provide a detailed recommendation with reasoning
4. Be specific about which service to use and why
5. Stay within your budget constraints

Be thorough, analytical, and provide actionable recommendations.";

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(task);

        // Enable automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            _kernel);

        var result = response.Content ?? "No response generated";

        _logger.LogInformation("AI Response: {Response}",
            result.Length > 200 ? result.Substring(0, 200) + "..." : result);

        // For now, agents analyze but don't auto-purchase
        // Future: Parse response and execute purchases if agent decides to
        return (result, 0m, new List<string>());
    }
}