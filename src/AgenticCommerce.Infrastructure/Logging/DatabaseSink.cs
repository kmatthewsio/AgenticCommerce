using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using System.Text.Json;

namespace AgenticCommerce.Infrastructure.Logging;

/// <summary>
/// Custom Serilog sink that writes logs to the database via EF Core.
/// </summary>
public class DatabaseSink : ILogEventSink
{
    private readonly IServiceProvider _serviceProvider;
    private readonly LogEventLevel _minimumLevel;

    public DatabaseSink(IServiceProvider serviceProvider, LogEventLevel minimumLevel = LogEventLevel.Warning)
    {
        _serviceProvider = serviceProvider;
        _minimumLevel = minimumLevel;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < _minimumLevel)
            return;

        // Run in background to not block the logging call
        Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AgenticCommerceDbContext>();

                var entry = new LogEntry
                {
                    Timestamp = logEvent.Timestamp.UtcDateTime,
                    Level = logEvent.Level.ToString(),
                    Message = logEvent.RenderMessage(),
                    Exception = logEvent.Exception?.ToString(),
                    Source = GetPropertyValue(logEvent, "SourceContext"),
                    RequestPath = GetPropertyValue(logEvent, "RequestPath"),
                    PropertiesJson = SerializeProperties(logEvent)
                };

                db.AppLogs.Add(entry);
                await db.SaveChangesAsync();
            }
            catch
            {
                // Silently fail - don't crash the app if logging fails
            }
        });
    }

    private static string? GetPropertyValue(LogEvent logEvent, string propertyName)
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var value))
        {
            return value.ToString().Trim('"');
        }
        return null;
    }

    private static string? SerializeProperties(LogEvent logEvent)
    {
        if (!logEvent.Properties.Any())
            return null;

        var dict = logEvent.Properties.ToDictionary(
            p => p.Key,
            p => p.Value.ToString());

        return JsonSerializer.Serialize(dict);
    }
}

/// <summary>
/// Extension methods for configuring database logging.
/// </summary>
public static class DatabaseSinkExtensions
{
    public static Serilog.LoggerConfiguration DatabaseSink(
        this Serilog.Configuration.LoggerSinkConfiguration sinkConfig,
        IServiceProvider serviceProvider,
        LogEventLevel minimumLevel = LogEventLevel.Warning)
    {
        return sinkConfig.Sink(new DatabaseSink(serviceProvider, minimumLevel));
    }
}
