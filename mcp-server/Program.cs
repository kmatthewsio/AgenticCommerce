using AgentRails.McpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var useHttp = args.Contains("--http");

if (useHttp)
{
    var builder = WebApplication.CreateBuilder(args);
    ConfigureServices(builder.Services);
    builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();
    var app = builder.Build();
    app.MapMcp();
    app.Run();
}
else
{
    var builder = Host.CreateApplicationBuilder(args);
    ConfigureServices(builder.Services);
    builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();
    await builder.Build().RunAsync();
}

static void ConfigureServices(IServiceCollection services)
{
    var config = new AgentRailsConfig
    {
        BaseUrl = Environment.GetEnvironmentVariable("AGENTRAILS_BASE_URL") ?? "https://sandbox.agentrails.io",
        ApiKey = Environment.GetEnvironmentVariable("AGENTRAILS_API_KEY") ?? ""
    };

    services.AddSingleton(config);

    services.AddHttpClient("AgentRails", client =>
    {
        client.BaseAddress = new Uri(config.BaseUrl);
        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        }
    });
}
