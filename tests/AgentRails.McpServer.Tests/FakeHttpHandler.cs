using System.Net;
using System.Text;

namespace AgentRails.McpServer.Tests;

/// <summary>
/// A fake HttpMessageHandler that returns a preconfigured response.
/// </summary>
public class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}

/// <summary>
/// An IHttpClientFactory that returns a client backed by FakeHttpHandler.
/// </summary>
public class FakeHttpClientFactory : IHttpClientFactory
{
    private readonly FakeHttpHandler _handler;

    public FakeHttpClientFactory(FakeHttpHandler handler)
    {
        _handler = handler;
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://sandbox.agentrails.io")
        };
        return client;
    }
}
