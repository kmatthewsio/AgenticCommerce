using System.Net;
using System.Text.Json;
using AgentRails.McpServer.Foundry.Tools;

namespace AgentRails.McpServer.Foundry.Tests;

public class BillingFunctionsTests
{
    [Fact]
    public async Task GetBillingUsage_DefaultDays_NoQueryString()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, """{"totalCost":12.50}""");
        var tools = new BillingFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.GetBillingUsage(null!, null);

        Assert.Contains("12.5", result);
        Assert.Equal("/api/billing/usage", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.True(string.IsNullOrEmpty(handler.LastRequest.RequestUri.Query));
    }

    [Fact]
    public async Task GetBillingUsage_WithDays_AddsQueryParam()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.OK, "{}");
        var tools = new BillingFunctions(new FakeHttpClientFactory(handler));

        await tools.GetBillingUsage(null!, "7");

        Assert.Contains("days=7", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetBillingUsage_ReturnsError_On403()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Forbidden, "Forbidden");
        var tools = new BillingFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.GetBillingUsage(null!, null);
        var error = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(error.GetProperty("error").GetBoolean());
        Assert.Equal(403, error.GetProperty("statusCode").GetInt32());
    }
}
