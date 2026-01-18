using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Logging;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly IDbLogger _dbLogger;
    private readonly ILogger<LogsController> _logger;

    public LogsController(IDbLogger dbLogger, ILogger<LogsController> logger)
    {
        _dbLogger = dbLogger;
        _logger = logger;
    }

    /// <summary>
    /// Get recent logs from the database
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<LogEntry>>> GetLogs(
        [FromQuery] int count = 100,
        [FromQuery] string? level = null)
    {
        var logs = await _dbLogger.GetLogsAsync(count, level);
        return Ok(logs);
    }

    /// <summary>
    /// Get logs filtered by level
    /// </summary>
    [HttpGet("errors")]
    public async Task<ActionResult<List<LogEntry>>> GetErrors([FromQuery] int count = 50)
    {
        var logs = await _dbLogger.GetLogsAsync(count, "Error");
        return Ok(logs);
    }

    [HttpGet("warnings")]
    public async Task<ActionResult<List<LogEntry>>> GetWarnings([FromQuery] int count = 50)
    {
        var logs = await _dbLogger.GetLogsAsync(count, "Warning");
        return Ok(logs);
    }

    /// <summary>
    /// Test endpoint to verify database logging works
    /// </summary>
    [HttpPost("test")]
    public async Task<ActionResult> TestLog([FromBody] TestLogRequest request)
    {
        await _dbLogger.LogAsync(
            request.Level ?? "Information",
            request.Message ?? "Test log entry",
            source: "LogsController.TestLog",
            requestPath: HttpContext.Request.Path);

        return Ok(new { message = "Log entry created", level = request.Level ?? "Information" });
    }
}

public class TestLogRequest
{
    public string? Level { get; set; }
    public string? Message { get; set; }
}
