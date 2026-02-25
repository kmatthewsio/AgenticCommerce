using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace AgentRails.McpServer.Foundry.Tools;

public class X402Functions(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [Function(nameof(GetX402Pricing))]
    public async Task<string> GetX402Pricing(
        [McpToolTrigger("get_x402_pricing", "Get x402 pricing information for API endpoints. No authentication required.")]
        ToolInvocationContext context)
    {
        var response = await Client.GetAsync("/api/x402/pricing");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetX402Payments))]
    public async Task<string> GetX402Payments(
        [McpToolTrigger("get_x402_payments", "Get x402 payment history with optional filters.")]
        ToolInvocationContext context,
        [McpToolProperty("network", "Filter by blockchain network (e.g. 'base-sepolia').")]
        string? network,
        [McpToolProperty("status", "Filter by payment status (e.g. 'completed', 'pending').")]
        string? status,
        [McpToolProperty("limit", "Maximum number of payments to return.")]
        string? limit)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrEmpty(network)) queryParts.Add($"network={Uri.EscapeDataString(network)}");
        if (!string.IsNullOrEmpty(status)) queryParts.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrEmpty(limit)) queryParts.Add($"limit={Uri.EscapeDataString(limit)}");
        var query = queryParts.Count > 0 ? "?" + string.Join("&", queryParts) : "";

        var response = await Client.GetAsync($"/api/x402/payments{query}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetX402Stats))]
    public async Task<string> GetX402Stats(
        [McpToolTrigger("get_x402_stats", "Get aggregate x402 payment statistics including totals and volumes.")]
        ToolInvocationContext context)
    {
        var response = await Client.GetAsync("/api/x402/stats");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(ExecuteTestPayment))]
    public async Task<string> ExecuteTestPayment(
        [McpToolTrigger("execute_test_payment", "Execute a test x402 payment on the sandbox environment.")]
        ToolInvocationContext context,
        [McpToolProperty("amount_usdc", "Amount in USDC to send (defaults to a small test amount).")]
        string? amountUsdc)
    {
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(amountUsdc) && decimal.TryParse(amountUsdc, out var amount))
            payload["amountUsdc"] = amount;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/x402/test/execute-payment", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
