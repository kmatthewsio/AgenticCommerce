using AgenticCommerce.API.Middleware;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(
        IAgentService agentService,
        AgenticCommerceDbContext db,
        ILogger<AgentsController> logger)
    {
        _agentService = agentService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Create a new autonomous agent with a budget
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Agent>> CreateAgent([FromBody] AgentConfig config)
    {
        try
        {
            // Associate with organization if authenticated
            var orgId = HttpContext.GetOrganizationId();
            if (orgId.HasValue)
            {
                config.OrganizationId = orgId.Value;
            }

            var agent = await _agentService.CreateAgentAsync(config);
            return Ok(agent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create agent");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Run an agent to perform a task
    /// </summary>
    [HttpPost("{agentId}/run")]
    public async Task<ActionResult<AgentRunResult>> RunAgent(
        string agentId,
        [FromBody] AgentTaskRequest request)
    {
        try
        {
            var result = await _agentService.RunAgentAsync(agentId, request.Task);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run agent {AgentId}", agentId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Have an agent make a purchase
    /// </summary>
    [HttpPost("{agentId}/purchase")]
    public async Task<ActionResult<PurchaseResult>> MakePurchase(
        string agentId,
        [FromBody] PurchaseRequest request)
    {
        try
        {
            // Get agent to check organization
            var agent = await _db.Agents.FirstOrDefaultAsync(a => a.Id == agentId);
            if (agent == null)
            {
                return NotFound(new { error = "Agent not found" });
            }

            // Sandbox: No policy checks - direct purchase
            var result = await _agentService.MakePurchaseAsync(agentId, request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process purchase for agent {AgentId}", agentId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get agent details
    /// </summary>
    [HttpGet("{agentId}")]
    public async Task<ActionResult<Agent>> GetAgent(string agentId)
    {
        var agent = await _agentService.GetAgentAsync(agentId);

        if (agent == null)
        {
            return NotFound(new { error = $"Agent {agentId} not found" });
        }

        return Ok(agent);
    }

    /// <summary>
    /// Get agent summary info
    /// </summary>
    [HttpGet("{agentId}/info")]
    public async Task<ActionResult<AgentInfo>> GetAgentInfo(string agentId)
    {
        var info = await _agentService.GetAgentInfoAsync(agentId);

        if (info == null)
        {
            return NotFound(new { error = $"Agent {agentId} not found" });
        }

        return Ok(info);
    }

    /// <summary>
    /// List all agents (optionally filtered by organization if authenticated)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AgentInfo>>> ListAgents()
    {
        var orgId = HttpContext.GetOrganizationId();

        if (orgId.HasValue)
        {
            // User is authenticated - filter by organization
            var agents = await _db.Agents
                .Where(a => a.OrganizationId == orgId.Value)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AgentInfo
                {
                    Id = a.Id,
                    Name = a.Name,
                    Status = a.Status,
                    CurrentBalance = a.CurrentBalance,
                    Budget = a.Budget,
                    WalletAddress = a.WalletAddress,
                    LastActiveAt = a.LastActiveAt
                })
                .ToListAsync();
            return Ok(agents);
        }

        // No auth - return all agents (backward compatibility)
        var allAgents = await _agentService.ListAgentsAsync();
        return Ok(allAgents);
    }

    /// <summary>
    /// List agents for dashboard (requires authentication)
    /// </summary>
    // Sandbox: No auth required
    [HttpGet("dashboard")]
    public async Task<ActionResult<object>> ListAgentsForDashboard()
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var agents = await _db.Agents
            .Where(a => a.OrganizationId == orgId.Value)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var totalBudget = agents.Sum(a => a.Budget);
        var totalSpent = agents.Sum(a => a.Budget - a.CurrentBalance);
        var activeCount = agents.Count(a => a.Status == "Active");

        return Ok(new
        {
            agents = agents.Select(a => new
            {
                id = a.Id,
                name = a.Name,
                description = a.Description,
                status = a.Status,
                budget = a.Budget,
                currentBalance = a.CurrentBalance,
                spent = a.Budget - a.CurrentBalance,
                walletAddress = a.WalletAddress,
                createdAt = a.CreatedAt,
                lastActiveAt = a.LastActiveAt
            }),
            summary = new
            {
                totalAgents = agents.Count,
                activeAgents = activeCount,
                totalBudget,
                totalSpent
            }
        });
    }

    /// <summary>
    /// Delete an agent
    /// </summary>
    [HttpDelete("{agentId}")]
    public async Task<ActionResult> DeleteAgent(string agentId)
    {
        var deleted = await _agentService.DeleteAgentAsync(agentId);

        if (!deleted)
        {
            return NotFound(new { error = $"Agent {agentId} not found" });
        }

        return Ok(new { message = $"Agent {agentId} deleted" });
    }

    /// <summary>
    /// Get transactions for dashboard (requires authentication)
    /// </summary>
    // Sandbox: No auth required
    [HttpGet("transactions")]
    public async Task<ActionResult> GetTransactionsForDashboard([FromQuery] int limit = 100)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        // Get agent IDs for this organization
        var agentIds = await _db.Agents
            .Where(a => a.OrganizationId == orgId.Value)
            .Select(a => a.Id)
            .ToListAsync();

        // Get transactions for those agents
        var transactions = await _db.Transactions
            .Where(t => agentIds.Contains(t.AgentId))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new
            {
                id = t.Id,
                agentId = t.AgentId,
                transactionId = t.TransactionId,
                amount = t.Amount,
                recipientAddress = t.RecipientAddress,
                description = t.Description,
                status = t.Status,
                createdAt = t.CreatedAt,
                completedAt = t.CompletedAt
            })
            .ToListAsync();

        return Ok(transactions);
    }

    /// <summary>
    /// Seed test transactions for dashboard testing (development only)
    /// </summary>
    [HttpPost("seed-transactions")]
    public async Task<ActionResult> SeedTestTransactions()
    {
        var orgId = HttpContext.GetOrganizationId();

        // Get agents for this organization
        var agents = await _db.Agents
            .Where(a => orgId == null || a.OrganizationId == orgId)
            .Take(4)
            .ToListAsync();

        if (!agents.Any())
        {
            return BadRequest(new { error = "No agents found to seed transactions for" });
        }

        var testTransactions = new List<AgenticCommerce.Core.Models.TransactionEntity>();
        var random = new Random();
        var statuses = new[] { "Completed", "Completed", "Completed", "Pending", "Failed" };
        var descriptions = new[]
        {
            "API data subscription",
            "Cloud services payment",
            "Analytics tool license",
            "Product research fee",
            "Premium data access",
            "Infrastructure costs",
            "Machine learning credits",
            "Storage allocation"
        };

        for (int i = 0; i < 8; i++)
        {
            var agent = agents[random.Next(agents.Count)];
            var status = statuses[random.Next(statuses.Length)];
            var hoursAgo = random.Next(1, 72);
            var createdAt = DateTime.UtcNow.AddHours(-hoursAgo);

            var txId = $"tx_test_{Guid.NewGuid():N}";
            var recipientAddr = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}";
            testTransactions.Add(new AgenticCommerce.Core.Models.TransactionEntity
            {
                AgentId = agent.Id,
                TransactionId = txId.Length > 20 ? txId.Substring(0, 20) : txId,
                Amount = Math.Round((decimal)(random.NextDouble() * 200 + 5), 2),
                RecipientAddress = recipientAddr.Length >= 42 ? recipientAddr.Substring(0, 42) : recipientAddr,
                Description = descriptions[random.Next(descriptions.Length)],
                Status = status,
                CreatedAt = createdAt,
                CompletedAt = status == "Completed" ? createdAt.AddMinutes(random.Next(1, 30)) : null
            });
        }

        await _db.Transactions.AddRangeAsync(testTransactions);
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Seeded {testTransactions.Count} test transactions", transactions = testTransactions.Select(t => new { t.TransactionId, t.Amount, t.Status }) });
    }

    /// <summary>
    /// Claim orphaned agents (agents without organization) for current user's organization
    /// </summary>
    // Sandbox: No auth required
    [HttpPost("claim-orphaned")]
    public async Task<ActionResult> ClaimOrphanedAgents()
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var orphanedAgents = await _db.Agents
            .Where(a => a.OrganizationId == null)
            .ToListAsync();

        foreach (var agent in orphanedAgents)
        {
            agent.OrganizationId = orgId.Value;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Claimed {orphanedAgents.Count} orphaned agents",
            agentIds = orphanedAgents.Select(a => a.Id).ToList()
        });
    }
}

/// <summary>
/// Request model for running an agent task
/// </summary>
public class AgentTaskRequest
{
    public string Task { get; set; } = string.Empty;
}