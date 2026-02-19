using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;



/// <summary>
/// Trust layer endpoints for checking service reputation and managing the service registry
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Trust")]
public class TrustController : ControllerBase
{
    private readonly ITrustService _trustService;
    private readonly ILogger<TrustController> _logger;

    public TrustController(ITrustService trustService, ILogger<TrustController> logger)
    {
        _trustService = trustService;
        _logger = logger;
    }

    /// <summary>
    /// Check trust level for a service URL
    /// </summary>
    /// <remarks>
    /// Combines service registry data with payment history to return a trust score.
    ///
    /// Trust scores:
    /// - **high**: Verified service with 100+ settled payments and 95%+ success rate
    /// - **medium**: Registered service with 50+ settled payments and 90%+ success rate
    /// - **low**: Registered service or 10+ payments but doesn't meet higher criteria
    /// - **unknown**: Not registered and fewer than 10 payments
    /// </remarks>
    /// <param name="serviceUrl">The service URL to check (e.g., https://api.example.com/data)</param>
    [HttpGet("check")]
    [ProducesResponseType(typeof(TrustCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TrustCheckResult>> CheckTrust([FromQuery] string serviceUrl)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            return BadRequest(new { error = "serviceUrl is required" });
        }

        try
        {
            var result = await _trustService.CheckTrustAsync(serviceUrl);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check trust for {ServiceUrl}", serviceUrl);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get service details from the registry
    /// </summary>
    /// <param name="url">The service URL to look up</param>
    [HttpGet("service")]
    [ProducesResponseType(typeof(ServiceRegistryEntity), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRegistryEntity>> GetService([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new { error = "url is required" });
        }

        try
        {
            var service = await _trustService.GetServiceAsync(url);
            if (service == null)
            {
                return NotFound(new { error = $"Service not found: {url}" });
            }
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service {Url}", url);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Register a new service in the registry
    /// </summary>
    /// <remarks>
    /// Services can self-register to be discoverable. Registration does not imply verification.
    /// Use the verify endpoint (admin only) to mark a service as verified.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ServiceRegistryEntity), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ServiceRegistryEntity>> RegisterService([FromBody] RegisterServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceUrl))
        {
            return BadRequest(new { error = "serviceUrl is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.OwnerWallet))
        {
            return BadRequest(new { error = "ownerWallet is required" });
        }

        try
        {
            var service = await _trustService.RegisterServiceAsync(request);
            _logger.LogInformation("Service registered: {ServiceUrl}", request.ServiceUrl);
            return StatusCode(StatusCodes.Status201Created, service);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceUrl}", request.ServiceUrl);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// List registered services
    /// </summary>
    /// <param name="verified">Filter by verification status (optional)</param>
    /// <param name="limit">Maximum number of services to return (default: 50, max: 500)</param>
    [HttpGet("registry")]
    [ProducesResponseType(typeof(List<ServiceRegistryEntity>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceRegistryEntity>>> ListServices(
        [FromQuery] bool? verified = null,
        [FromQuery] int limit = 50)
    {
        limit = Math.Min(limit, 500);

        try
        {
            var services = await _trustService.ListServicesAsync(verified, limit);
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list services");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mark a service as verified (admin operation — requires Trust:AdminToken)
    /// </summary>
    /// <remarks>
    /// Verification indicates that AgentRails has confirmed the service endpoint
    /// responds correctly to x402 payment flows.
    /// </remarks>
    [HttpPost("verify")]
    [ProducesResponseType(typeof(ServiceRegistryEntity), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceRegistryEntity>> VerifyService(
        [FromBody] VerifyServiceRequest request,
        [FromServices] IConfiguration configuration)
    {
        var adminToken = configuration["Trust:AdminToken"];
        if (string.IsNullOrEmpty(adminToken))
            return StatusCode(500, new { error = "Trust:AdminToken not configured" });

        var providedToken = Request.Headers["X-Admin-Token"].FirstOrDefault();
        if (providedToken != adminToken)
            return Unauthorized(new { error = "Invalid or missing X-Admin-Token header" });

        if (string.IsNullOrWhiteSpace(request.ServiceUrl))
        {
            return BadRequest(new { error = "serviceUrl is required" });
        }

        try
        {
            var service = await _trustService.VerifyServiceAsync(request.ServiceUrl);
            if (service == null)
            {
                return NotFound(new { error = $"Service not found: {request.ServiceUrl}" });
            }

            _logger.LogInformation("Service verified: {ServiceUrl}", request.ServiceUrl);
            return Ok(service);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify service {ServiceUrl}", request.ServiceUrl);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get payment statistics for a service URL
    /// </summary>
    /// <param name="serviceUrl">The service URL to get stats for</param>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ServicePaymentStats), StatusCodes.Status200OK)]
    public async Task<ActionResult<ServicePaymentStats>> GetPaymentStats([FromQuery] string serviceUrl)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            return BadRequest(new { error = "serviceUrl is required" });
        }

        try
        {
            var stats = await _trustService.GetPaymentStatsAsync(serviceUrl);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment stats for {ServiceUrl}", serviceUrl);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request to verify a service
/// </summary>
public class VerifyServiceRequest
{
    public string ServiceUrl { get; set; } = string.Empty;
}
