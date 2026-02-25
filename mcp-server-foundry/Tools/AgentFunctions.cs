using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace AgentRails.McpServer.Foundry.Tools;

public class AgentFunctions(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [Function(nameof(ListAgents))]
    public async Task<string> ListAgents(
        [McpToolTrigger("list_agents", "List all agents in the current organization.")]
        ToolInvocationContext context)
    {
        var response = await Client.GetAsync("/api/agents");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetAgent))]
    public async Task<string> GetAgent(
        [McpToolTrigger("get_agent", "Get details of a specific agent by ID.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent.", isRequired: true)]
        string agentId)
    {
        var response = await Client.GetAsync($"/api/agents/{Uri.EscapeDataString(agentId)}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(CreateAgent))]
    public async Task<string> CreateAgent(
        [McpToolTrigger("create_agent", "Create a new agent with the given name and optional configuration.")]
        ToolInvocationContext context,
        [McpToolProperty("name", "Name of the agent to create.", isRequired: true)]
        string name,
        [McpToolProperty("description", "Optional description of the agent's purpose.")]
        string? description,
        [McpToolProperty("budget", "Optional spending budget for the agent in USD.")]
        string? budget)
    {
        var payload = new Dictionary<string, object?> { ["name"] = name };
        if (!string.IsNullOrEmpty(description)) payload["description"] = description;
        if (!string.IsNullOrEmpty(budget) && decimal.TryParse(budget, out var budgetValue))
            payload["budget"] = budgetValue;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/agents", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(DeleteAgent))]
    public async Task<string> DeleteAgent(
        [McpToolTrigger("delete_agent", "Delete an agent by ID.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent to delete.", isRequired: true)]
        string agentId)
    {
        var response = await Client.DeleteAsync($"/api/agents/{Uri.EscapeDataString(agentId)}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return string.IsNullOrWhiteSpace(body) ? JsonSerializer.Serialize(new { success = true }) : body;
    }

    [Function(nameof(RunAgent))]
    public async Task<string> RunAgent(
        [McpToolTrigger("run_agent", "Run an agent with a specific task. The agent will execute the task and return results.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent to run.", isRequired: true)]
        string agentId,
        [McpToolProperty("task", "The task or prompt for the agent to execute.", isRequired: true)]
        string task)
    {
        var payload = new Dictionary<string, object> { ["task"] = task };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/agents/{Uri.EscapeDataString(agentId)}/run", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
