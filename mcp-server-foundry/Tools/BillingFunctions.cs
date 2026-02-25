using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace AgentRails.McpServer.Foundry.Tools;

public class BillingFunctions(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [Function(nameof(GetBillingUsage))]
    public async Task<string> GetBillingUsage(
        [McpToolTrigger("get_billing_usage", "Get billing usage summary for the current organization over a number of days.")]
        ToolInvocationContext context,
        [McpToolProperty("days", "Number of days to look back (default: 30).")]
        string? days)
    {
        var query = !string.IsNullOrEmpty(days) ? $"?days={Uri.EscapeDataString(days)}" : "";
        var response = await Client.GetAsync($"/api/billing/usage{query}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
