using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

var baseUrl = Environment.GetEnvironmentVariable("AGENTRAILS_BASE_URL")
    ?? "https://sandbox.agentrails.io";
var apiKey = Environment.GetEnvironmentVariable("AGENTRAILS_API_KEY") ?? "";

builder.Services.AddHttpClient("AgentRails", client =>
{
    client.BaseAddress = new Uri(baseUrl);
    if (!string.IsNullOrEmpty(apiKey))
    {
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    }
});

builder.Build().Run();
