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

    [McpServerTool, Description("Analyze sentiment of text. Returns a score from -1.0 (negative) to 1.0 (positive). Costs $0.001 USDC via x402.")]
    public async Task<string> analyze_sentiment(
        [Description("The text to analyze for sentiment.")] string text)
    {
        var response = await Client.GetAsync($"/api/x402/utility/sentiment?text={Uri.EscapeDataString(text)}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Summarize text by extracting the most important sentences. Costs $0.005 USDC via x402.")]
    public async Task<string> summarize_text(
        [Description("The text to summarize.")] string text,
        [Description("Number of sentences to return (default 3).")] int? sentences = null)
    {
        var query = $"text={Uri.EscapeDataString(text)}";
        if (sentences is not null) query += $"&sentences={sentences}";
        var response = await Client.GetAsync($"/api/x402/utility/summarize?{query}");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Transform JSON data with operations: flatten, sort_keys, remove_nulls, count_keys. Costs $0.002 USDC via x402.")]
    public async Task<string> transform_json(
        [Description("JSON object to transform.")] string data,
        [Description("Comma-separated operations: flatten, sort_keys, remove_nulls, count_keys.")] string operations)
    {
        var payload = new { data = JsonSerializer.Deserialize<JsonElement>(data), operations = operations.Split(',').Select(o => o.Trim()).ToArray() };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/x402/utility/json-transform", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Compute a cryptographic hash of text. Supports md5, sha1, sha256, sha384, sha512. Costs $0.001 USDC via x402.")]
    public async Task<string> hash_text(
        [Description("The text to hash.")] string text,
        [Description("Hash algorithm: md5, sha1, sha256, sha384, sha512 (default sha256).")] string? algorithm = null)
    {
        var query = $"text={Uri.EscapeDataString(text)}";
        if (algorithm is not null) query += $"&algorithm={Uri.EscapeDataString(algorithm)}";
        var response = await Client.GetAsync($"/api/x402/utility/hash?{query}");
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
