using System.Net;
using System.Text.Json;
using AgentRails.McpServer.Foundry.Tools;

namespace AgentRails.McpServer.Foundry.Tests;

public class PolicyFunctionsTests
{
    [Fact]
    public async Task ListPolicies_ReturnsBody()
    {
        var expected = JsonSerializer.Serialize(new[] { new { id = "p1", name = "Default" } });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new PolicyFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.ListPolicies(null!);

        Assert.Equal(expected, result);
        Assert.Equal("/api/org-policies", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ListPolicies_ReturnsError_On401()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized, "Unauthorized");
        var tools = new PolicyFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.ListPolicies(null!);
        var error = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(error.GetProperty("error").GetBoolean());
        Assert.Equal(401, error.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task CreatePolicy_PostsAllFields()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Created, """{"id":"p2"}""");
        var tools = new PolicyFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.CreatePolicy(null!,
            name: "Strict",
            requiresApproval: "true",
            description: "High-value policy",
            maxTransactionAmount: "500",
            dailySpendingLimit: "1000");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/org-policies", handler.LastRequest.RequestUri!.AbsolutePath);

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Strict", payload.GetProperty("name").GetString());
        Assert.True(payload.GetProperty("requiresApproval").GetBoolean());
        Assert.Equal("High-value policy", payload.GetProperty("description").GetString());
        Assert.Equal(500m, payload.GetProperty("maxTransactionAmount").GetDecimal());
        Assert.Equal(1000m, payload.GetProperty("dailySpendingLimit").GetDecimal());
    }

    [Fact]
    public async Task CreatePolicy_OmitsOptionalFields_WhenNull()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Created, "{}");
        var tools = new PolicyFunctions(new FakeHttpClientFactory(handler));

        await tools.CreatePolicy(null!, name: "Basic", requiresApproval: "false", null, null, null);

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Basic", payload.GetProperty("name").GetString());
        Assert.False(payload.GetProperty("requiresApproval").GetBoolean());
        Assert.False(payload.TryGetProperty("description", out _));
        Assert.False(payload.TryGetProperty("maxTransactionAmount", out _));
        Assert.False(payload.TryGetProperty("dailySpendingLimit", out _));
    }
}
