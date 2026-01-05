using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Collections.Concurrent;

namespace AgenticCommerce.Infrastructure.Agents;

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
        IOptions<AIOptions> aiOptions,
        ILoggerFactory loggerFactory)
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

            // Add plugins with logger
            builder.Plugins.AddFromObject(
                new PaymentPlugin(_arcClient, loggerFactory.CreateLogger<PaymentPlugin>()),
                "Payment");
            builder.Plugins.AddFromObject(new ResearchPlugin(), "Research");

            _kernel = builder.Build();

            _logger.LogInformation("AgentService initialized with OpenAI ({Model})", _aiOptions.OpenAIModel);
        }
        else
        {
            _logger.LogWarning("No AI API key configured - agents will use simulation mode");
        }
    }

    public async Task<Agent> CreateAgentAsync(string name, string description, decimal budget, List<string> capabilities)
    {
        var walletAddress = _arcClient.GetAddress();

        var agent = new Agent
        {
            Id = $"agent_{Guid.NewGuid():N}",
            Name = name,
            Description = description,
            Budget = budget,
            CurrentBalance = budget,
            WalletAddress = walletAddress,
            Status = AgentStatus.Active,
            Capabilities = capabilities,
            CreatedAt = DateTime.UtcNow
        };

        _agents.TryAdd(agent.Id, agent);
        _agentTransactions.TryAdd(agent.Id, new List<string>());

        _logger.LogInformation(
            "Created agent {AgentId} ({Name}) with budget ${Budget}",
            agent.Id, agent.Name, agent.Budget);

        return agent;
    }

    public Task<Agent?> GetAgentAsync(string agentId)
    {
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    public Task<List<Agent>> GetAllAgentsAsync()
    {
        return Task.FromResult(_agents.Values.ToList());
    }

    public async Task<AgentRunResult> RunAgentAsync(string agentId, string task)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
        {
            return new AgentRunResult
            {
                Success = false,
                ErrorMessage = $"Agent {agentId} not found"
            };
        }

        if (agent.Status != AgentStatus.Active)
        {
            return new AgentRunResult
            {
                Success = false,
                ErrorMessage = $"Agent {agentId} is not active (current status: {agent.Status})"
            };
        }

        var startTime = DateTime.UtcNow;
        agent.Status = AgentStatus.Working;
        agent.LastActiveAt = startTime;

        try
        {
            _logger.LogInformation("Agent {AgentId} starting task: {Task}", agentId, task);

            (string result, decimal amountSpent, List<string> transactionIds) execution;

            // Use real AI if available, otherwise simulate
            if (_kernel != null)
            {
                execution = await ExecuteWithAIAsync(agent, task);
            }
            else
            {
                execution = await SimulateExecutionAsync(agent, task);
            }

            // Track transactions
            if (_agentTransactions.TryGetValue(agentId, out var txList))
            {
                txList.AddRange(execution.transactionIds);
            }

            agent.Status = AgentStatus.Active;
            agent.LastActiveAt = DateTime.UtcNow;

            return new AgentRunResult
            {
                Success = true,
                AgentId = agentId,
                Result = execution.result,
                AmountSpent = execution.amountSpent,
                TransactionIds = execution.transactionIds,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            agent.Status = AgentStatus.Error;
            _logger.LogError(ex, "Agent {AgentId} task failed", agentId);

            return new AgentRunResult
            {
                Success = false,
                AgentId = agentId,
                ErrorMessage = ex.Message,
                StartedAt = startTime,
                CompletedAt = DateTime.UtcNow
            };
        }
    }

    public Task<bool> DeleteAgentAsync(string agentId)
    {
        var removed = _agents.TryRemove(agentId, out var agent);

        if (removed)
        {
            _agentTransactions.TryRemove(agentId, out _);
            _logger.LogInformation("Deleted agent {AgentId} ({Name})", agentId, agent!.Name);
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

        var systemPrompt = $@"You are '{agent.Name}', an autonomous AI agent with the ability to execute purchases.

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
- Payment.ExecutePurchase: Execute a USDC payment (recipientAddress, amount, description)
- Research.ResearchAIProviders: Get AI/LLM API pricing info
- Research.ResearchImageProviders: Get image generation API pricing
- Research.CompareServices: Compare two service options
- Research.CalculateCosts: Calculate usage cost estimates

YOUR DECISION-MAKING AUTHORITY:
When the user asks you to ""buy"", ""purchase"", or ""execute"" something:
1. First, research and analyze options using your research tools
2. Make an informed decision about the best option
3. Verify it's within your budget using Payment.CheckBudget
4. If approved, USE Payment.ExecutePurchase to complete the transaction
5. Return the transaction ID and confirmation

IMPORTANT RULES:
- ALWAYS verify budget before purchasing
- ALWAYS provide clear reasoning for your decisions
- ONLY purchase if explicitly asked to (words like ""buy"", ""purchase"", ""get"")
- For test purchases, you can use your own wallet address: {agent.WalletAddress}
- Be transparent about what you're doing

Example autonomous flow:
User: ""Research and buy the best AI API under $50""
1. Research options (use Research tools)
2. Analyze and decide (Gemini 1.5 Flash at $37.50)
3. Check budget (Payment.CheckBudget with $37.50 and ${agent.CurrentBalance:F2})
4. Execute purchase (Payment.ExecutePurchase with recipient address, amount, description)
5. Report results with transaction ID

Be autonomous, be smart, stay within budget.";

        var chatHistory = new ChatHistory(systemPrompt);
        chatHistory.AddUserMessage(task);

        // Enable automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7,
            MaxTokens = 2000
        };

        _logger.LogInformation("Agent executing task with AI: {Task}", task);

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            _kernel);

        var result = response.Content ?? "No response generated";

        _logger.LogInformation("AI Response: {Response}",
            result.Length > 200 ? result.Substring(0, 200) + "..." : result);

        // Parse transaction IDs from the response if any purchases were made
        var transactionIds = new List<string>();
        decimal amountSpent = 0m;

        // Extract transaction IDs from response (format: "Transaction ID: xxx")
        var txIdMatches = System.Text.RegularExpressions.Regex.Matches(
            result,
           @"(?:\*\*)?Transaction ID(?:\*\*)?:\s*([a-f0-9-]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in txIdMatches)
        {
            if (match.Groups.Count > 1)
            {
                var txId = match.Groups[1].Value;
                transactionIds.Add(txId);
                _logger.LogInformation("Extracted transaction ID: {TxId}", txId);
            }
        }

        // Extract amount spent from response (format: "Amount: $xx.xx" or "xx.xx USDC")
        var amountMatches = System.Text.RegularExpressions.Regex.Matches(
            result,
             @"(?:-\s*)?(?:\*\*)?Amount(?:\*\*)?:\s*\$?(\d+(?:\.\d+)?)\s*(?:USDC)?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (amountMatches.Count > 0 &&
            decimal.TryParse(amountMatches[0].Groups[1].Value, out var extractedAmount))
        {
            amountSpent = extractedAmount;
            _logger.LogInformation("Extracted amount spent: ${Amount}", amountSpent);

            // Update agent balance
            agent.CurrentBalance -= amountSpent;
            _logger.LogInformation("Agent balance updated: ${Balance}", agent.CurrentBalance);
        }

        return (result, amountSpent, transactionIds);
    }

    /// <summary>
    /// Simulate agent execution (fallback when no AI API key configured)
    /// </summary>
    private async Task<(string Result, decimal AmountSpent, List<string> TransactionIds)> SimulateExecutionAsync(
        Agent agent,
        string task)
    {
        await Task.Delay(2000); // Simulate thinking time

        var simulatedResult = $@"[SIMULATION MODE - No AI API key configured]

Agent '{agent.Name}' analyzed task: '{task}'

Simulated reasoning:
1. Analyzed available budget: ${agent.CurrentBalance:F2} USDC
2. Evaluated task requirements
3. Determined appropriate action

Note: This is a simulation. Configure OpenAI API key in appsettings for real AI-powered execution.

To enable real autonomous agents:
1. Add OpenAI API key to appsettings.Development.json
2. Restart the application
3. Agents will use GPT-4o for real decision-making

Current Status: Simulation completed successfully";

        return (simulatedResult, 0m, new List<string>());
    }

    public async Task<Agent> CreateAgentAsync(AgentConfig config)
    {
        // Delegate to the main CreateAgentAsync method
        return await CreateAgentAsync(
            config.Name,
            config.Description ?? "Autonomous agent",
            config.Budget,
            config.Capabilities ?? new List<string> { "research", "analysis", "payments" }
        );
    }

    public async Task<PurchaseResult> MakePurchaseAsync(string agentId, PurchaseRequest request)
    {
        if (!_agents.TryGetValue(agentId, out var agent))
        {
            return new PurchaseResult
            {
                Success = false,
                ErrorMessage = $"Agent {agentId} not found"
            };
        }

        try
        {
            // Check budget
            if (request.Amount > agent.CurrentBalance)
            {
                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient budget. Required: ${request.Amount:F2}, Available: ${agent.CurrentBalance:F2}"
                };
            }

            _logger.LogInformation(
                "Agent {AgentId} executing manual purchase: ${Amount} to {Recipient}",
                agentId, request.Amount, request.RecipientAddress);

            // Execute transaction
            var txId = await _arcClient.SendUsdcAsync(request.RecipientAddress, request.Amount);

            // Update agent balance
            agent.CurrentBalance -= request.Amount;

            // Track transaction
            if (_agentTransactions.TryGetValue(agentId, out var txList))
            {
                txList.Add(txId);
            }

            _logger.LogInformation(
                "Agent {AgentId} purchase successful. TX: {TxId}, New balance: ${Balance}",
                agentId, txId, agent.CurrentBalance);

            return new PurchaseResult
            {
                Success = true,
                TransactionId = txId,
                AmountSpent = request.Amount,
                RecipientAddress = request.RecipientAddress,
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

    public async Task<AgentInfo?> GetAgentInfoAsync(string agentId)
    {
        var agent = await GetAgentAsync(agentId);

        if (agent == null)
            return null;

        _agentTransactions.TryGetValue(agentId, out var transactions);

        return new AgentInfo
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            Budget = agent.Budget,
            CurrentBalance = agent.CurrentBalance,
            Status = agent.Status.ToString(),
            WalletAddress = agent.WalletAddress,
            Capabilities = agent.Capabilities,
            TransactionCount = transactions?.Count ?? 0,
            TransactionIds = transactions ?? new List<string>(),
            CreatedAt = agent.CreatedAt,
            LastActiveAt = agent.LastActiveAt
        };
    }

    public async Task<List<AgentInfo>> ListAgentsAsync()
    {
        var agents = await GetAllAgentsAsync();
        var agentInfos = new List<AgentInfo>();

        foreach (var agent in agents)
        {
            var info = await GetAgentInfoAsync(agent.Id);
            if (info != null)
            {
                agentInfos.Add(info);
            }
        }

        return agentInfos;
    }
}