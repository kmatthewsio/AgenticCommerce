using AgenticCommerce.Infrastructure.Blockchain;
using AgenticCommerce.Core.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
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

builder.Services.Configure<CircleOptions>(
    builder.Configuration.GetSection("Circle"));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IArcClient, ArcClient>();
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


