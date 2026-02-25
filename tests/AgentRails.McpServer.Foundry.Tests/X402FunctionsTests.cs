using System.Net;
using System.Text.Json;
using AgentRails.McpServer.Foundry.Tools;

namespace AgentRails.McpServer.Foundry.Tests;

public class X402FunctionsTests
{
    [Fact]
    public async Task GetX402Pricing_ReturnsBody()
    {
        var expected = JsonSerializer.Serialize(new { endpoints = new[] { new { resource = "/api/x402/protected/analysis" } } });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        var result = await tools.GetX402Pricing(null!);

        Assert.Equal(expected, result);
        Assert.Equal("/api/x402/pricing", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetX402Payments_NoFilters_NoQueryString()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        await tools.GetX402Payments(null!, null, null, null);

        Assert.Equal("/api/x402/payments", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.True(string.IsNullOrEmpty(handler.LastRequest.RequestUri.Query));
    }

    [Fact]
    public async Task GetX402Payments_WithFilters_BuildsQueryString()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        await tools.GetX402Payments(null!, "base-sepolia", "completed", "5");

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("network=base-sepolia", query);
        Assert.Contains("status=completed", query);
        Assert.Contains("limit=5", query);
    }

    [Fact]
    public async Task GetX402Stats_ReturnsBody()
    {
        var expected = """{"totalPayments":42}""";
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        var result = await tools.GetX402Stats(null!);

        Assert.Equal(expected, result);
        Assert.Equal("/api/x402/stats", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetX402Stats_ReturnsError_On500()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "Internal Server Error");
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        var result = await tools.GetX402Stats(null!);
        var error = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(error.GetProperty("error").GetBoolean());
        Assert.Equal(500, error.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task ExecuteTestPayment_PostsAmount()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, """{"txHash":"0xabc"}""");
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        var result = await tools.ExecuteTestPayment(null!, "0.01");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/x402/test/execute-payment", handler.LastRequest.RequestUri!.AbsolutePath);

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal(0.01m, payload.GetProperty("amountUsdc").GetDecimal());
    }

    [Fact]
    public async Task ExecuteTestPayment_OmitsAmount_WhenNull()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "{}");
        var tools = new X402Functions(new FakeHttpClientFactory(handler));

        await tools.ExecuteTestPayment(null!, null);

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.False(payload.TryGetProperty("amountUsdc", out _));
    }
}
