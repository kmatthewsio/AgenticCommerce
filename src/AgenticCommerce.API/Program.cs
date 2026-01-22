using System.Text;
using AgenticCommerce.API.Authentication;
using AgenticCommerce.API.Middleware;
using AgenticCommerce.API.Services;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Infrastructure.Agents;
using AgenticCommerce.Infrastructure.Blockchain;
using AgenticCommerce.Infrastructure.Data;
using AgenticCommerce.Infrastructure.Logging;
using AgenticCommerce.Infrastructure.Payments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
#if ENTERPRISE
using AgenticCommerce.Enterprise;
#endif

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

// ========================================
// CONFIGURE JWT + API KEY AUTHENTICATION
// ========================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AgentRails-Default-Secret-Key-Change-In-Production-32chars";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "MultiAuth";
    options.DefaultChallengeScheme = "MultiAuth";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgentRails",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AgentRails",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
})
.AddApiKey()
.AddPolicyScheme("MultiAuth", "JWT or API Key", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        // If X-API-Key header is present, use API key auth
        if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName))
        {
            return ApiKeyAuthenticationHandler.SchemeName;
        }
        // Otherwise use JWT
        return JwtBearerDefaults.AuthenticationScheme;
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPolicyEnforcementService, PolicyEnforcementService>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IArcClient, ArcClient>();
builder.Services.AddScoped<IAgentService, AgentService>();
// X402 Payment Services
builder.Services.AddSingleton<IX402PaymentService, X402PaymentService>(); // Legacy
builder.Services.AddX402Payments(); // V2 Spec-compliant with attribute support
builder.Services.AddHttpClient<ICircleGatewayClient, CircleGatewayClient>();
builder.Services.AddHealthChecks();

// Database logging service
builder.Services.AddScoped<IDbLogger, DbLogger>();

// ========================================
// ENTERPRISE FEATURES (Policy Engine)
// ========================================
#if ENTERPRISE
builder.Services.AddEnterpriseServices(connectionString!);
#endif

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgenticCommerceDbContext>();
    db.Database.Migrate();
}

// Apply enterprise migrations (Policy Engine tables)
#if ENTERPRISE
await app.Services.ApplyEnterpriseMigrationsAsync();
#endif

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgenticCommerce.API v1");
    });
//}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve admin dashboard
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantMiddleware();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseCors();

app.MapGet("/", () => new
{
    service = "Agentic Commerce Backend",
    version = "v1.1.0",
    status = "Running",
#if ENTERPRISE
    edition = "Enterprise",
#else
    edition = "Standard",
#endif
    features = new[]
    {
        "Circle Arc Blockchain",
        "Circle Gateway (cross-chain USDC)",
        "X402 Payment Facilitation",
        "AI Agent Orchestration",
#if ENTERPRISE
        "Policy Engine (Enterprise)"
#endif
    },
    endpoints = new
    {
        swagger = "/swagger",
        health = "/health",
#if ENTERPRISE
        policies = "/api/policies"
#endif
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


