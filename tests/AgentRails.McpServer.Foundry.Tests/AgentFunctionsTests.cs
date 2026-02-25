using System.Net;
using System.Text.Json;
using AgentRails.McpServer.Foundry.Tools;

namespace AgentRails.McpServer.Foundry.Tests;

public class AgentFunctionsTests
{
    [Fact]
    public async Task ListAgents_ReturnsBody_On200()
    {
        var expected = JsonSerializer.Serialize(new[] { new { id = "a1", name = "TestAgent" } });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.ListAgents(null!);

        Assert.Equal(expected, result);
        Assert.Equal("/api/agents", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
    }

    [Fact]
    public async Task ListAgents_ReturnsError_On401()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Unauthorized, "Unauthorized");
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.ListAgents(null!);
        var error = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(error.GetProperty("error").GetBoolean());
        Assert.Equal(401, error.GetProperty("statusCode").GetInt32());
    }

    [Fact]
    public async Task GetAgent_SendsCorrectPath()
    {
        var expected = JsonSerializer.Serialize(new { id = "abc", name = "MyAgent" });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.GetAgent(null!, "abc");

        Assert.Equal(expected, result);
        Assert.Equal("/api/agents/abc", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateAgent_PostsJsonBody()
    {
        var expected = JsonSerializer.Serialize(new { id = "new1", name = "Bot" });
        var handler = new FakeHttpHandler(HttpStatusCode.Created, expected);
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.CreateAgent(null!, "Bot", "A test bot", "100");

        Assert.Equal(expected, result);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/agents", handler.LastRequest.RequestUri!.AbsolutePath);

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Bot", payload.GetProperty("name").GetString());
        Assert.Equal("A test bot", payload.GetProperty("description").GetString());
        Assert.Equal(100m, payload.GetProperty("budget").GetDecimal());
    }

    [Fact]
    public async Task CreateAgent_OmitsOptionalFields_WhenNull()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.Created, "{}");
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        await tools.CreateAgent(null!, "Bot", null, null);

        var body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Bot", payload.GetProperty("name").GetString());
        Assert.False(payload.TryGetProperty("description", out _));
        Assert.False(payload.TryGetProperty("budget", out _));
    }

    [Fact]
    public async Task DeleteAgent_SendsDelete()
    {
        var handler = new FakeHttpHandler(HttpStatusCode.NoContent, "");
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.DeleteAgent(null!, "xyz");
        var parsed = JsonSerializer.Deserialize<JsonElement>(result);

        Assert.True(parsed.GetProperty("success").GetBoolean());
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("/api/agents/xyz", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RunAgent_PostsTask()
    {
        var expected = JsonSerializer.Serialize(new { output = "done" });
        var handler = new FakeHttpHandler(HttpStatusCode.OK, expected);
        var tools = new AgentFunctions(new FakeHttpClientFactory(handler));

        var result = await tools.RunAgent(null!, "a1", "Summarize sales data");

        Assert.Equal(expected, result);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/agents/a1/run", handler.LastRequest.RequestUri!.AbsolutePath);

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        Assert.Equal("Summarize sales data", payload.GetProperty("task").GetString());
    }
}
