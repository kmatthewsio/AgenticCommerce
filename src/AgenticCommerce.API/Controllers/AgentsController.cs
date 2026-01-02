using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentsController> _logger;

    public AgentsController(IAgentService agentService, ILogger<AgentsController> logger)
    {
        _agentService = agentService;
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
    /// List all agents
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AgentInfo>>> ListAgents()
    {
        var agents = await _agentService.ListAgentsAsync();
        return Ok(agents);
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
}

/// <summary>
/// Request model for running an agent task
/// </summary>
public class AgentTaskRequest
{
    public string Task { get; set; } = string.Empty;
}