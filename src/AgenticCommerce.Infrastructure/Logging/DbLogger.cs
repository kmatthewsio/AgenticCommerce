using System.Text.Json;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Logging;

/// <summary>
/// Database logging service implementation.
/// Writes logs directly to the app_logs table.
/// </summary>
public class DbLogger : IDbLogger
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<DbLogger> _logger;

    public DbLogger(AgenticCommerceDbContext db, ILogger<DbLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(
        string level,
        string message,
        string? exception = null,
        string? source = null,
        string? requestPath = null,
        Dictionary<string, object>? properties = null)
    {
        try
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Message = message,
                Exception = exception,
                Source = source,
                RequestPath = requestPath,
                PropertiesJson = properties != null ? JsonSerializer.Serialize(properties) : null
            };

            _db.AppLogs.Add(entry);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log to file/console as fallback
            _logger.LogError(ex, "Failed to write log to database: {Message}", message);
        }
    }

    public Task LogInfoAsync(string message, string? source = null)
        => LogAsync("Information", message, source: source);

    public Task LogWarningAsync(string message, string? source = null, string? exception = null)
        => LogAsync("Warning", message, exception: exception, source: source);

    public Task LogErrorAsync(string message, string? source = null, string? exception = null)
        => LogAsync("Error", message, exception: exception, source: source);

    public async Task<List<LogEntry>> GetLogsAsync(int count = 100, string? level = null)
    {
        var query = _db.AppLogs.AsQueryable();

        if (!string.IsNullOrEmpty(level))
        {
            query = query.Where(l => l.Level == level);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();
    }
}
