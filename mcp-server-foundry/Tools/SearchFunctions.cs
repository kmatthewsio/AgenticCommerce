using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace AgentRails.McpServer.Foundry.Tools;

public class SearchFunctions(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [Function(nameof(SearchAuditTrail))]
    public async Task<string> SearchAuditTrail(
        [McpToolTrigger("search_audit_trail", "Search the audit trail (policy decisions) using natural language. Returns AI-powered analysis of spending approvals, denials, and policy enforcement patterns.")]
        ToolInvocationContext context,
        [McpToolProperty("query", "Natural language query, e.g. 'Show me all denied transactions related to budget limits'.", isRequired: true)]
        string query,
        [McpToolProperty("agent_id", "Optional agent ID to scope the search.")]
        string? agentId,
        [McpToolProperty("from", "Optional start date (ISO 8601, e.g. '2026-03-01').")]
        string? from,
        [McpToolProperty("to", "Optional end date (ISO 8601, e.g. '2026-03-16').")]
        string? to,
        [McpToolProperty("result", "Optional result filter: Approved, Denied, PendingApproval, ApprovalTimeout.")]
        string? result,
        [McpToolProperty("max_records", "Maximum records to analyze (default 200, max 500).")]
        string? maxRecords)
    {
        var body = new Dictionary<string, object?> { ["query"] = query };
        if (!string.IsNullOrEmpty(agentId)) body["agentId"] = agentId;
        if (!string.IsNullOrEmpty(from)) body["from"] = from;
        if (!string.IsNullOrEmpty(to)) body["to"] = to;
        if (!string.IsNullOrEmpty(result)) body["result"] = result;
        if (!string.IsNullOrEmpty(maxRecords) && int.TryParse(maxRecords, out var mr)) body["maxRecords"] = mr;

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/search/audit", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = responseBody });
        return responseBody;
    }

    [Function(nameof(SearchAnomalies))]
    public async Task<string> SearchAnomalies(
        [McpToolTrigger("search_anomalies", "Search anomaly alerts using natural language. Returns AI-powered analysis of detected spending anomalies, risk patterns, and recommended actions.")]
        ToolInvocationContext context,
        [McpToolProperty("query", "Natural language query, e.g. 'Which agents had spending spikes this week?'.", isRequired: true)]
        string query,
        [McpToolProperty("agent_id", "Optional agent ID to scope the search.")]
        string? agentId,
        [McpToolProperty("from", "Optional start date (ISO 8601, e.g. '2026-03-01').")]
        string? from,
        [McpToolProperty("to", "Optional end date (ISO 8601, e.g. '2026-03-16').")]
        string? to,
        [McpToolProperty("alert_type", "Optional alert type: SpendingSpike, VelocitySpike, LargeTransaction, RapidBudgetBurn, NewDestination, DuplicatePayment, PredictiveFreeze.")]
        string? alertType,
        [McpToolProperty("severity", "Optional severity: Low, Medium, High, Critical.")]
        string? severity,
        [McpToolProperty("max_records", "Maximum records to analyze (default 200, max 500).")]
        string? maxRecords)
    {
        var body = new Dictionary<string, object?> { ["query"] = query };
        if (!string.IsNullOrEmpty(agentId)) body["agentId"] = agentId;
        if (!string.IsNullOrEmpty(from)) body["from"] = from;
        if (!string.IsNullOrEmpty(to)) body["to"] = to;
        if (!string.IsNullOrEmpty(alertType)) body["alertType"] = alertType;
        if (!string.IsNullOrEmpty(severity)) body["severity"] = severity;
        if (!string.IsNullOrEmpty(maxRecords) && int.TryParse(maxRecords, out var mr)) body["maxRecords"] = mr;

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/search/anomalies", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = responseBody });
        return responseBody;
    }
}
