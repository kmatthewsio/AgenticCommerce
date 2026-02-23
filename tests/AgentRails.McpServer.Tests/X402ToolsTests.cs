using System.Net;
using System.Text.Json;
using AgentRails.McpServer.Tools;

namespace AgentRails.McpServer.Tests;

public class X402ToolsTests
{
    [Fact]
    public async Task GetPricing_ReturnsBody()
    {
        var expected = JsonSerializer.Serialize(new { endpoints = new[] { new { resource = "/api/x402/protected/analysis" } } });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        var result = await tools.get_x402_pricing();

        Assert.Equal(expected, result);
        Assert.Equal("/api/x402/pricing", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetPayments_NoFilters_NoQueryString()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        await tools.get_x402_payments();

        Assert.Equal("/api/x402/payments", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Null(handler.LastRequest.RequestUri.Query is "" ? null : handler.LastRequest.RequestUri.Query);
    }

    [Fact]
    public async Task GetPayments_WithFilters_BuildsQueryString()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "[]");
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        await tools.get_x402_payments(network: "base-sepolia", status: "completed", limit: 5);

        var query = handler.LastRequest!.RequestUri!.Query;
        Assert.Contains("network=base-sepolia", query);
        Assert.Contains("status=completed", query);
        Assert.Contains("limit=5", query);
    }

    [Fact]
    public async Task GetStats_ReturnsBody()
    {
        var expected = """{"totalPayments":42}""";
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        var result = await tools.get_x402_stats();

        Assert.Equal(expected, result);
        Assert.Equal("/api/x402/stats", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetStats_ReturnsError_On500()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.InternalServerError, "Internal Server Error");
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        var result = await tools.get_x402_stats();
        var error = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(error.GetProperty("error").GetBoolean());
        Assert.Equal(500, error.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task ExecuteTestPayment_PostsAmount()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, """{"txHash":"0xabc"}""");
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        var result = await tools.execute_test_payment(0.01m);

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
        var tools = new X402Tools(new FakeHttpClientFactory(handler));

        await tools.execute_test_payment();

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.False(payload.TryGetProperty("amountUsdc", out _));
    }
}
