using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Infrastructure.Agents;
using AgenticCommerce.Infrastructure.Blockchain;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions( options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors( options =>
{
    options.AddDefaultPolicy( policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========================================
// CONFIGURE DATABASE
// ========================================
builder.Services.AddDbContext<AgenticCommerceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<CircleOptions>(
    builder.Configuration.GetSection("Circle"));

// ========================================
// CONFIGURE AI OPTIONS
// ========================================
builder.Services.Configure<AIOptions>(options =>
{
    options.OpenAIApiKey = builder.Configuration["OpenAI:ApiKey"];
    options.OpenAIModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o";
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IArcClient, ArcClient>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddHttpClient<ICircleGatewayClient, CircleGatewayClient>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgenticCommerce.API v1");
    });
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseCors();

app.MapGet("/", () => new
{
    service = "Agentic Commerce Backend",
    Version = "v1.0.0",
    status = "Running",
    features = new[]
    {
        "Circele Arc Blockchain",
        "Circle Gateway (cross-chain USDC)",
        "X402 Payment Facilitation",
        "AI Agent Orchestration"
    },
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health"
    }
});

try
{
    Log.Information("Starting Agentic Commerce API");

    var arcClient = app.Services.GetRequiredService<IArcClient>();
    var isConnected = await arcClient.IsConnectedAsync();
    var walletAddress = arcClient.GetAddress();

    if (isConnected)
    {
        Log.Information("Circle API Connected");
        Log.Information("Wallet Address: {Address}", walletAddress);

        try
        {
            var balance = await arcClient.GetBalanceAsync();
            Log.Information("Connected to ARC blockchain with wallet {WalletAddress}. Current USDC Balance: {Balance}", walletAddress, balance);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving balance for wallet {WalletAddress}", walletAddress);
        }
    }
    else
    {
        Log.Warning("Failed to connect to ARC blockchain.");
    }
} catch (Exception ex)
{
    Log.Fatal(ex, "Failed to test circle connection.");
}
finally
{
    Log.CloseAndFlush();
}

try
{
    app.Run();
    Log.Information("Starting Agentic Commerce API on https://localhost:7098");  

}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally { Log.CloseAndFlush(); }


