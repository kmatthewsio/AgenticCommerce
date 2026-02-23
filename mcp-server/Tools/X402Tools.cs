using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AgentRails.McpServer.Tools;

[McpServerToolType]
public class X402Tools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [McpServerTool, Description("Get x402 pricing information for API endpoints. No authentication required.")]
    public async Task<string> get_x402_pricing()
    {
        var response = await Client.GetAsync("/api/x402/pricing");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get x402 payment history with optional filters.")]
    public async Task<string> get_x402_payments(
        [Description("Filter by blockchain network (e.g. 'base-sepolia').")] string? network = null,
        [Description("Filter by payment status (e.g. 'completed', 'pending').")] string? status = null,
        [Description("Maximum number of payments to return.")] int? limit = null)
    {
        var queryParts = new List<string>();
        if (network is not null) queryParts.Add($"network={Uri.EscapeDataString(network)}");
        if (status is not null) queryParts.Add($"status={Uri.EscapeDataString(status)}");
        if (limit is not null) queryParts.Add($"limit={limit}");
        var query = queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : "";

        var response = await Client.GetAsync($"/api/x402/payments{query}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get aggregate x402 payment statistics including totals and volumes.")]
    public async Task<string> get_x402_stats()
    {
        var response = await Client.GetAsync("/api/x402/stats");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Execute a test x402 payment on the sandbox environment.")]
    public async Task<string> execute_test_payment(
        [Description("Amount in USDC to send (defaults to a small test amount).")] decimal? amountUsdc = null)
    {
        var payload = new Dictionary<string, object?>();
        if (amountUsdc is not null) payload["amountUsdc"] = amountUsdc;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/x402/test/execute-payment", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
