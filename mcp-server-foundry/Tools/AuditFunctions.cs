using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace AgentRails.McpServer.Foundry.Tools;

public class AuditFunctions(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [Function(nameof(GetAuditTimeline))]
    public async Task<string> GetAuditTimeline(
        [McpToolTrigger("get_audit_timeline", "Get a unified audit timeline of all payment events, policy decisions, and approval actions. Filterable by agent, date range, event type, and status.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "Optional agent ID to filter events for a specific agent.")]
        string? agentId,
        [McpToolProperty("from", "Optional start date (ISO 8601, e.g. '2026-03-01').")]
        string? from,
        [McpToolProperty("to", "Optional end date (ISO 8601, e.g. '2026-03-11').")]
        string? to,
        [McpToolProperty("event_type", "Optional event type filter: Payment, PolicyEvaluation, or Approval.")]
        string? eventType,
        [McpToolProperty("status", "Optional status filter (e.g. 'Settled', 'Denied', 'Approved').")]
        string? status,
        [McpToolProperty("page", "Page number (default 1).")]
        string? page,
        [McpToolProperty("page_size", "Page size (default 50, max 500).")]
        string? pageSize)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(agentId)) queryParams.Add($"agentId={Uri.EscapeDataString(agentId)}");
        if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={Uri.EscapeDataString(from)}");
        if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={Uri.EscapeDataString(to)}");
        if (!string.IsNullOrEmpty(eventType)) queryParams.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrEmpty(page) && int.TryParse(page, out var p)) queryParams.Add($"page={p}");
        if (!string.IsNullOrEmpty(pageSize) && int.TryParse(pageSize, out var ps)) queryParams.Add($"pageSize={ps}");

        var url = $"/api/audit/timeline?{string.Join("&", queryParams)}";
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetAgentAuditHistory))]
    public async Task<string> GetAgentAuditHistory(
        [McpToolTrigger("get_agent_history", "Get the full audit history for a specific agent including all payments, policy evaluations, and approval actions.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent.", isRequired: true)]
        string agentId,
        [McpToolProperty("from", "Optional start date (ISO 8601).")]
        string? from,
        [McpToolProperty("to", "Optional end date (ISO 8601).")]
        string? to,
        [McpToolProperty("page", "Page number (default 1).")]
        string? page,
        [McpToolProperty("page_size", "Page size (default 50, max 500).")]
        string? pageSize)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={Uri.EscapeDataString(from)}");
        if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={Uri.EscapeDataString(to)}");
        if (!string.IsNullOrEmpty(page) && int.TryParse(page, out var p)) queryParams.Add($"page={p}");
        if (!string.IsNullOrEmpty(pageSize) && int.TryParse(pageSize, out var ps)) queryParams.Add($"pageSize={ps}");

        var url = $"/api/audit/agents/{Uri.EscapeDataString(agentId)}/history?{string.Join("&", queryParams)}";
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetAuditSummary))]
    public async Task<string> GetAuditSummary(
        [McpToolTrigger("get_audit_summary", "Get aggregate audit summary statistics including total payments, approval rates, top denial reasons, and top spending agents.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "Optional agent ID to scope summary to a specific agent.")]
        string? agentId,
        [McpToolProperty("from", "Optional start date (ISO 8601).")]
        string? from,
        [McpToolProperty("to", "Optional end date (ISO 8601).")]
        string? to)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(agentId)) queryParams.Add($"agentId={Uri.EscapeDataString(agentId)}");
        if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={Uri.EscapeDataString(from)}");
        if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={Uri.EscapeDataString(to)}");

        var url = queryParams.Count > 0
            ? $"/api/audit/summary?{string.Join("&", queryParams)}"
            : "/api/audit/summary";
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
