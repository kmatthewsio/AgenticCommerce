using AgenticCommerce.API.Middleware;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Infrastructure.Agents;
using AgenticCommerce.Infrastructure.Blockchain;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Email;
using AgenticCommerce.Infrastructure.Gumroad;
using AgenticCommerce.Infrastructure.Payments;
using AgenticCommerce.Infrastructure.Trust;
using AgenticCommerce.Infrastructure.Billing;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Resend;
using Serilog;

// Load environment variables from .env file (if it exists)
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Get connection string for database logging
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Initial logger (console + file only, database added after app build)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AgentRails Sandbox API",
        Version = "v1",
        Description = "Sandbox environment for testing x402 payment protocol and AI agent capabilities. For production access, contact sales@agentrails.io"
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
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

// ========================================
// CORE SERVICES (No Auth for Sandbox)
// ========================================
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IArcClient, ArcClient>();
builder.Services.AddScoped<IAgentService, AgentService>();

// X402 Payment Services
builder.Services.AddSingleton<IX402PaymentService, X402PaymentService>();
builder.Services.AddX402Payments();
builder.Services.AddHttpClient<ICircleGatewayClient, CircleGatewayClient>();
builder.Services.AddHealthChecks();

// Trust Layer Services
builder.Services.AddScoped<ITrustService, TrustService>();

// Gumroad integration
builder.Services.AddScoped<IApiKeyGenerationService, ApiKeyGenerationService>();

// Stripe + Email integration
builder.Services.AddOptions();
builder.Services.AddHttpClient<ResendClient>();
builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken = builder.Configuration["Resend:ApiKey"] ?? "";
});
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Usage tracking for billing
builder.Services.AddScoped<IUsageTrackingService, UsageTrackingService>();
builder.Services.AddScoped<IStripeBillingService, StripeBillingService>();

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgenticCommerceDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgentRails Sandbox API v1");
    c.DocumentTitle = "AgentRails Sandbox API";
});

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseTenantMiddleware(); // Still needed for extension methods
app.MapControllers();
app.MapHealthChecks("/health");
app.UseCors();


try
{
    Log.Information("Starting AgentRails Sandbox API");

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
            Log.Information("Connected to ARC blockchain (Testnet) with wallet {WalletAddress}. Current USDC Balance: {Balance}", walletAddress, balance);
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
}
catch (Exception ex)
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
