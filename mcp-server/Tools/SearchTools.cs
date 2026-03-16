using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AgentRails.McpServer.Tools;

[McpServerToolType]
public class SearchTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [McpServerTool, Description("Search the audit trail (policy decisions) using natural language. Returns AI-powered analysis of spending approvals, denials, and policy enforcement patterns. Supports optional filters for agent, date range, and decision result.")]
    public async Task<string> search_audit_trail(
        [Description("Natural language query, e.g. 'Show me all denied transactions related to budget limits'.")] string query,
        [Description("Optional agent ID to scope the search.")] string? agentId = null,
        [Description("Optional start date (ISO 8601, e.g. '2026-03-01').")] string? from = null,
        [Description("Optional end date (ISO 8601, e.g. '2026-03-16').")] string? to = null,
        [Description("Optional result filter: Approved, Denied, PendingApproval, ApprovalTimeout.")] string? result = null,
        [Description("Maximum records to analyze (default 200, max 500).")] int maxRecords = 200)
    {
        var body = new Dictionary<string, object?> { ["query"] = query, ["maxRecords"] = maxRecords };
        if (!string.IsNullOrEmpty(agentId)) body["agentId"] = agentId;
        if (!string.IsNullOrEmpty(from)) body["from"] = from;
        if (!string.IsNullOrEmpty(to)) body["to"] = to;
        if (!string.IsNullOrEmpty(result)) body["result"] = result;

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/search/audit", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = responseBody });
        return responseBody;
    }

    [McpServerTool, Description("Search anomaly alerts using natural language. Returns AI-powered analysis of detected spending anomalies, risk patterns, and recommended actions. Supports optional filters for agent, date range, alert type, and severity.")]
    public async Task<string> search_anomalies(
        [Description("Natural language query, e.g. 'Which agents had spending spikes this week?'.")] string query,
        [Description("Optional agent ID to scope the search.")] string? agentId = null,
        [Description("Optional start date (ISO 8601, e.g. '2026-03-01').")] string? from = null,
        [Description("Optional end date (ISO 8601, e.g. '2026-03-16').")] string? to = null,
        [Description("Optional alert type: SpendingSpike, VelocitySpike, LargeTransaction, RapidBudgetBurn, NewDestination, DuplicatePayment, PredictiveFreeze.")] string? alertType = null,
        [Description("Optional severity: Low, Medium, High, Critical.")] string? severity = null,
        [Description("Maximum records to analyze (default 200, max 500).")] int maxRecords = 200)
    {
        var body = new Dictionary<string, object?> { ["query"] = query, ["maxRecords"] = maxRecords };
        if (!string.IsNullOrEmpty(agentId)) body["agentId"] = agentId;
        if (!string.IsNullOrEmpty(from)) body["from"] = from;
        if (!string.IsNullOrEmpty(to)) body["to"] = to;
        if (!string.IsNullOrEmpty(alertType)) body["alertType"] = alertType;
        if (!string.IsNullOrEmpty(severity)) body["severity"] = severity;

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/search/anomalies", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = responseBody });
        return responseBody;
    }
}
