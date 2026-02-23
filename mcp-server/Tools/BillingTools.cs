using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AgentRails.McpServer.Tools;

[McpServerToolType]
public class BillingTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [McpServerTool, Description("Get billing usage summary for the current organization over a number of days.")]
    public async Task<string> get_billing_usage(
        [Description("Number of days to look back (default: 30).")] int? days = null)
    {
        var query = days is not null ? $"?days={days}" : "";
        var response = await Client.GetAsync($"/api/billing/usage{query}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
