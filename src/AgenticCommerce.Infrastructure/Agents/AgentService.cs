using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace AgenticCommerce.Infrastructure.Agents;

public class AgentService : IAgentService
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<AgentService> _logger;
    private readonly AIOptions _aiOptions;
    private readonly Kernel? _kernel;
    private readonly AgenticCommerceDbContext _dbContext;

    public AgentService(
        IArcClient arcClient,
        ILogger<AgentService> logger,
        IOptions<AIOptions> aiOptions,
        ILoggerFactory loggerFactory,
        AgenticCommerceDbContext dbContext)
    {
        _arcClient = arcClient;
        _logger = logger;
        _aiOptions = aiOptions.Value;
        _dbContext = dbContext;

        // Initialize Semantic Kernel if API key is configured
        if (!string.IsNullOrEmpty(_aiOptions.OpenAIApiKey))
        {
            var builder = Kernel.CreateBuilder();

            builder.AddOpenAIChatCompletion(
                modelId: _aiOptions.OpenAIModel!,
                apiKey: _aiOptions.OpenAIApiKey!);

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

        var agentEntity = new AgentEntity
        {
            Id = $"agent_{Guid.NewGuid():N}",
            Name = name,
            Description = description,
            Budget = budget,
            CurrentBalance = budget,
            WalletAddress = walletAddress,
            WalletId = walletAddress, // Using same for now
            Status = AgentStatus.Active.ToString(),
            CapabilitiesJson = JsonSerializer.Serialize(capabilities),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Agents.Add(agentEntity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Created agent {AgentId} ({Name}) with budget ${Budget}",
            agentEntity.Id, agentEntity.Name, agentEntity.Budget);

        return MapToAgent(agentEntity);
    }

    public async Task<Agent?> GetAgentAsync(string agentId)
    {
        var agentEntity = await _dbContext.Agents
            .Include(a => a.Transactions)  // ← This loads transactions
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agentEntity == null)
            return null;

        var agent = MapToAgent(agentEntity);

        // Add transaction IDs from database
        agent.TransactionIds = agentEntity.Transactions
            .Select(t => t.TransactionId)
            .ToList();

        return agent;
    }

    public async Task<List<Agent>> GetAllAgentsAsync()
    {
        var agentEntities = await _dbContext.Agents
            .Include(a => a.Transactions)
            .ToListAsync();

        return agentEntities.Select(MapToAgent).ToList();
    }

    public async Task<AgentRunResult> RunAgentAsync(string agentId, string task)
    {
        var agentEntity = await _dbContext.Agents.FindAsync(agentId);

        if (agentEntity == null)
        {
            return new AgentRunResult
            {
                Success = false,
                ErrorMessage = $"Agent {agentId} not found"
            };
        }

        var agent = MapToAgent(agentEntity);

        if (agent.Status != AgentStatus.Active)
        {
            return new AgentRunResult
            {
                Success = false,
                ErrorMessage = $"Agent {agentId} is not active (current status: {agent.Status})"
            };
        }

        var startTime = DateTime.UtcNow;
        agentEntity.Status = AgentStatus.Working.ToString();
        agentEntity.LastActiveAt = startTime;
        await _dbContext.SaveChangesAsync();

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

            // Save transactions to database
            foreach (var txId in execution.transactionIds)
            {
                var transaction = new TransactionEntity
                {
                    AgentId = agentId,
                    TransactionId = txId,
                    Amount = execution.amountSpent,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };
                _dbContext.Transactions.Add(transaction);
            }

            // Update agent balance
            agentEntity.CurrentBalance -= execution.amountSpent;
            agentEntity.Status = AgentStatus.Active.ToString();
            agentEntity.LastActiveAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

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
            agentEntity.Status = AgentStatus.Error.ToString();
            await _dbContext.SaveChangesAsync();

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

    public async Task<bool> DeleteAgentAsync(string agentId)
    {
        var agentEntity = await _dbContext.Agents.FindAsync(agentId);

        if (agentEntity == null)
            return false;

        _dbContext.Agents.Remove(agentEntity);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Deleted agent {AgentId} ({Name})", agentId, agentEntity.Name);
        return true;
    }

    public async Task<Agent> CreateAgentAsync(AgentConfig config)
    {
        return await CreateAgentAsync(
            config.Name,
            config.Description ?? "Autonomous agent",
            config.Budget,
            config.Capabilities ?? new List<string> { "research", "analysis", "payments" }
        );
    }

    public async Task<PurchaseResult> MakePurchaseAsync(string agentId, PurchaseRequest request)
    {
        var agentEntity = await _dbContext.Agents.FindAsync(agentId);

        if (agentEntity == null)
        {
            return new PurchaseResult
            {
                Success = false,
                ErrorMessage = $"Agent {agentId} not found"
            };
        }

        try
        {
            if (request.Amount > agentEntity.CurrentBalance)
            {
                return new PurchaseResult
                {
                    Success = false,
                    ErrorMessage = $"Insufficient budget. Required: ${request.Amount:F2}, Available: ${agentEntity.CurrentBalance:F2}"
                };
            }

            _logger.LogInformation(
                "Agent {AgentId} executing manual purchase: ${Amount} to {Recipient}",
                agentId, request.Amount, request.RecipientAddress);

            var txId = await _arcClient.SendUsdcAsync(request.RecipientAddress, request.Amount);

            agentEntity.CurrentBalance -= request.Amount;

            var transaction = new TransactionEntity
            {
                AgentId = agentId,
                TransactionId = txId,
                Amount = request.Amount,
                RecipientAddress = request.RecipientAddress,
                Description = request.Description,
                Status = "Completed",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Agent {AgentId} purchase successful. TX: {TxId}, New balance: ${Balance}",
                agentId, txId, agentEntity.CurrentBalance);

            return new PurchaseResult
            {
                Success = true,
                TransactionId = txId,
                AmountSpent = request.Amount,
                RecipientAddress = request.RecipientAddress,
                RemainingBalance = agentEntity.CurrentBalance
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
        var agentEntity = await _dbContext.Agents
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agentEntity == null)
            return null;

        return new AgentInfo
        {
            Id = agentEntity.Id,
            Name = agentEntity.Name,
            Description = agentEntity.Description,
            Budget = agentEntity.Budget,
            CurrentBalance = agentEntity.CurrentBalance,
            Status = agentEntity.Status,
            WalletAddress = agentEntity.WalletAddress,
            Capabilities = JsonSerializer.Deserialize<List<string>>(agentEntity.CapabilitiesJson ?? "[]") ?? new(),
            TransactionCount = agentEntity.Transactions.Count,
            TransactionIds = agentEntity.Transactions.Select(t => t.TransactionId).ToList(),
            CreatedAt = agentEntity.CreatedAt,
            LastActiveAt = agentEntity.LastActiveAt
        };
    }

    public async Task<List<AgentInfo>> ListAgentsAsync()
    {
        var agentEntities = await _dbContext.Agents
            .Include(a => a.Transactions)
            .ToListAsync();

        var agentInfos = new List<AgentInfo>();
        foreach (var entity in agentEntities)
        {
            agentInfos.Add(new AgentInfo
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Budget = entity.Budget,
                CurrentBalance = entity.CurrentBalance,
                Status = entity.Status,
                WalletAddress = entity.WalletAddress,
                Capabilities = JsonSerializer.Deserialize<List<string>>(entity.CapabilitiesJson ?? "[]") ?? new(),
                TransactionCount = entity.Transactions.Count,
                TransactionIds = entity.Transactions.Select(t => t.TransactionId).ToList(),
                CreatedAt = entity.CreatedAt,
                LastActiveAt = entity.LastActiveAt
            });
        }

        return agentInfos;
    }

    // Helper method to map AgentEntity to Agent
    // Helper method to map AgentEntity to Agent
    private Agent MapToAgent(AgentEntity entity)
    {
        return new Agent
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Budget = entity.Budget,
            CurrentBalance = entity.CurrentBalance,
            WalletAddress = entity.WalletAddress,
            WalletId = entity.WalletId,
            Status = Enum.Parse<AgentStatus>(entity.Status),
            Capabilities = !string.IsNullOrEmpty(entity.CapabilitiesJson)
                ? JsonSerializer.Deserialize<List<string>>(entity.CapabilitiesJson) ?? new()
                : new(),
            CreatedAt = entity.CreatedAt,
            LastActiveAt = entity.LastActiveAt,
            Metadata = !string.IsNullOrEmpty(entity.MetadataJson)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.MetadataJson)
                : null
        };
    }

    // ExecuteWithAIAsync and SimulateExecutionAsync stay the same...
    // (keeping the existing implementations)

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

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.7,
            MaxTokens = 2000
        };

        _logger.LogInformation("🤖 Agent executing task with AI: {Task}", task);

        var response = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            _kernel);

        var result = response.Content ?? "No response generated";

        _logger.LogInformation("🤖 AI Response: {Response}",
            result.Length > 200 ? result.Substring(0, 200) + "..." : result);

        var transactionIds = new List<string>();
        decimal amountSpent = 0m;

        // Extract transaction IDs - handle markdown and bullet points
        var txIdMatches = System.Text.RegularExpressions.Regex.Matches(
            result,
            @"(?:-\s*)?(?:\*\*)?Transaction ID(?:\*\*)?:\s*([a-f0-9-]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in txIdMatches)
        {
            if (match.Groups.Count > 1)
            {
                var txId = match.Groups[1].Value;
                transactionIds.Add(txId);
                _logger.LogInformation("✅ Extracted transaction ID: {TxId}", txId);
            }
        }

        // Extract amount - handle markdown, bullets, and "USDC" suffix
        var amountMatches = System.Text.RegularExpressions.Regex.Matches(
            result,
            @"(?:-\s*)?(?:\*\*)?Amount(?:\*\*)?:\s*\$?(\d+(?:\.\d+)?)\s*(?:USDC)?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (amountMatches.Count > 0 &&
            decimal.TryParse(amountMatches[0].Groups[1].Value, out var extractedAmount))
        {
            amountSpent = extractedAmount;
            _logger.LogInformation("💰 Extracted amount spent: ${Amount}", amountSpent);
        }

        return (result, amountSpent, transactionIds);
    }

    private async Task<(string Result, decimal AmountSpent, List<string> TransactionIds)> SimulateExecutionAsync(
        Agent agent,
        string task)
    {
        await Task.Delay(2000);

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
}