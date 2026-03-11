using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AgentRails.McpServer.Tools;

[McpServerToolType]
public class AuditTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [McpServerTool, Description("Get a unified audit timeline of all payment events, policy decisions, and approval actions. Filterable by agent, date range, event type, and status. Returns paginated results sorted by timestamp.")]
    public async Task<string> get_audit_timeline(
        [Description("Optional agent ID to filter events for a specific agent.")] string? agentId = null,
        [Description("Optional start date (ISO 8601, e.g. '2026-03-01').")] string? from = null,
        [Description("Optional end date (ISO 8601, e.g. '2026-03-11').")] string? to = null,
        [Description("Optional event type filter: Payment, PolicyEvaluation, or Approval.")] string? eventType = null,
        [Description("Optional status filter (e.g. 'Settled', 'Denied', 'Approved').")] string? status = null,
        [Description("Page number (default 1).")] int page = 1,
        [Description("Page size (default 50, max 500).")] int pageSize = 50)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(agentId)) queryParams.Add($"agentId={Uri.EscapeDataString(agentId)}");
        if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={Uri.EscapeDataString(from)}");
        if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={Uri.EscapeDataString(to)}");
        if (!string.IsNullOrEmpty(eventType)) queryParams.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"/api/audit/timeline?{string.Join("&", queryParams)}";
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get the full audit history for a specific agent including all payments, policy evaluations, and approval actions.")]
    public async Task<string> get_agent_history(
        [Description("The unique identifier of the agent.")] string agentId,
        [Description("Optional start date (ISO 8601).")] string? from = null,
        [Description("Optional end date (ISO 8601).")] string? to = null,
        [Description("Page number (default 1).")] int page = 1,
        [Description("Page size (default 50, max 500).")] int pageSize = 50)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(from)) queryParams.Add($"from={Uri.EscapeDataString(from)}");
        if (!string.IsNullOrEmpty(to)) queryParams.Add($"to={Uri.EscapeDataString(to)}");
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");

        var url = $"/api/audit/agents/{Uri.EscapeDataString(agentId)}/history?{string.Join("&", queryParams)}";
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get aggregate audit summary statistics including total payments, approval rates, top denial reasons, and top spending agents. Use for compliance dashboards and reporting.")]
    public async Task<string> get_audit_summary(
        [Description("Optional agent ID to scope summary to a specific agent.")] string? agentId = null,
        [Description("Optional start date (ISO 8601).")] string? from = null,
        [Description("Optional end date (ISO 8601).")] string? to = null)
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
