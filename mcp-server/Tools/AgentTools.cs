using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AgentRails.McpServer.Tools;

[McpServerToolType]
public class AgentTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [McpServerTool, Description("List all agents in the current organization.")]
    public async Task<string> list_agents()
    {
        var response = await Client.GetAsync("/api/agents");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get details of a specific agent by ID.")]
    public async Task<string> get_agent(
        [Description("The unique identifier of the agent.")] string agentId)
    {
        var response = await Client.GetAsync($"/api/agents/{agentId}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Create a new agent with the given name and optional configuration.")]
    public async Task<string> create_agent(
        [Description("Name of the agent to create.")] string name,
        [Description("Optional description of the agent's purpose.")] string? description = null,
        [Description("Optional spending budget for the agent in USD.")] decimal? budget = null)
    {
        var payload = new Dictionary<string, object?> { ["name"] = name };
        if (description is not null) payload["description"] = description;
        if (budget is not null) payload["budget"] = budget;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/agents", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Delete an agent by ID.")]
    public async Task<string> delete_agent(
        [Description("The unique identifier of the agent to delete.")] string agentId)
    {
        var response = await Client.DeleteAsync($"/api/agents/{agentId}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return string.IsNullOrWhiteSpace(body) ? JsonSerializer.Serialize(new { success = true }) : body;
    }

    [McpServerTool, Description("Run an agent with a specific task. The agent will execute the task and return results.")]
    public async Task<string> run_agent(
        [Description("The unique identifier of the agent to run.")] string agentId,
        [Description("The task or prompt for the agent to execute.")] string task)
    {
        var payload = new Dictionary<string, object> { ["task"] = task };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/agents/{agentId}/run", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
