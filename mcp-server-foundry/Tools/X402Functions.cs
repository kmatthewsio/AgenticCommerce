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

    [Function(nameof(AnalyzeSentiment))]
    public async Task<string> AnalyzeSentiment(
        [McpToolTrigger("analyze_sentiment", "Analyze sentiment of text. Returns a score from -1.0 (negative) to 1.0 (positive). Costs $0.001 USDC via x402.")]
        ToolInvocationContext context,
        [McpToolProperty("text", "The text to analyze for sentiment.")]
        string text)
    {
        var response = await Client.GetAsync($"/api/x402/utility/sentiment?text={Uri.EscapeDataString(text)}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(SummarizeText))]
    public async Task<string> SummarizeText(
        [McpToolTrigger("summarize_text", "Summarize text by extracting the most important sentences. Costs $0.005 USDC via x402.")]
        ToolInvocationContext context,
        [McpToolProperty("text", "The text to summarize.")]
        string text,
        [McpToolProperty("sentences", "Number of sentences to return (default 3).")]
        string? sentences)
    {
        var query = $"text={Uri.EscapeDataString(text)}";
        if (!string.IsNullOrEmpty(sentences)) query += $"&sentences={Uri.EscapeDataString(sentences)}";
        var response = await Client.GetAsync($"/api/x402/utility/summarize?{query}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(TransformJson))]
    public async Task<string> TransformJson(
        [McpToolTrigger("transform_json", "Transform JSON data with operations: flatten, sort_keys, remove_nulls, count_keys. Costs $0.002 USDC via x402.")]
        ToolInvocationContext context,
        [McpToolProperty("data", "JSON object to transform.")]
        string data,
        [McpToolProperty("operations", "Comma-separated operations: flatten, sort_keys, remove_nulls, count_keys.")]
        string operations)
    {
        var opsArray = operations.Split(',').Select(o => o.Trim()).ToArray();
        var payload = $"{{\"data\":{data},\"operations\":{JsonSerializer.Serialize(opsArray)}}}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/x402/utility/json-transform", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(HashText))]
    public async Task<string> HashText(
        [McpToolTrigger("hash_text", "Compute a cryptographic hash of text. Supports md5, sha1, sha256, sha384, sha512. Costs $0.001 USDC via x402.")]
        ToolInvocationContext context,
        [McpToolProperty("text", "The text to hash.")]
        string text,
        [McpToolProperty("algorithm", "Hash algorithm: md5, sha1, sha256, sha384, sha512 (default sha256).")]
        string? algorithm)
    {
        var query = $"text={Uri.EscapeDataString(text)}";
        if (!string.IsNullOrEmpty(algorithm)) query += $"&algorithm={Uri.EscapeDataString(algorithm)}";
        var response = await Client.GetAsync($"/api/x402/utility/hash?{query}");
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
