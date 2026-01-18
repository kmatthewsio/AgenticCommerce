using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Infrastructure.Logging;

/// <summary>
/// Interface for database logging service.
/// </summary>
public interface IDbLogger
{
    Task LogAsync(string level, string message, string? exception = null, string? source = null, string? requestPath = null, Dictionary<string, object>? properties = null);
    Task LogInfoAsync(string message, string? source = null);
    Task LogWarningAsync(string message, string? source = null, string? exception = null);
    Task LogErrorAsync(string message, string? source = null, string? exception = null);
    Task<List<LogEntry>> GetLogsAsync(int count = 100, string? level = null);
}
