using AgenticCommerce.API.Authentication;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.API.Controllers;

/// <summary>
/// Billing and usage management endpoints.
/// </summary>
[ApiController]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly AgenticCommerceDbContext _db;
    private readonly IUsageTrackingService _usageTrackingService;
    private readonly IStripeBillingService _stripeBillingService;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        AgenticCommerceDbContext db,
        IUsageTrackingService usageTrackingService,
        IStripeBillingService stripeBillingService,
        ILogger<BillingController> logger)
    {
        _db = db;
        _usageTrackingService = usageTrackingService;
        _stripeBillingService = stripeBillingService;
        _logger = logger;
    }

    /// <summary>
    /// Get usage summary for the authenticated organization.
    /// </summary>
    [HttpGet("usage")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
    public async Task<IActionResult> GetUsage([FromQuery] int days = 30)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (orgId == null)
        {
            return Unauthorized(new { error = "Organization not found" });
        }

        var from = DateTime.UtcNow.AddDays(-days);
        var to = DateTime.UtcNow;

        var summary = await _usageTrackingService.GetUsageSummaryAsync(orgId.Value, from, to);

        var org = await _db.Organizations.FindAsync(orgId.Value);

        return Ok(new
        {
            organization = new { id = orgId.Value, tier = org?.Tier },
            period = new { from, to, days },
            usage = new
            {
                transactionCount = summary.TransactionCount,
                totalVolumeUsdc = summary.TotalVolume,
                totalFeesUsd = summary.TotalFees,
                billedFeesUsd = summary.BilledFees,
                unbilledFeesUsd = summary.UnbilledFees
            }
        });
    }

    /// <summary>
    /// Get usage summary by email (public endpoint for account holders).
    /// </summary>
    [HttpGet("usage/{email}")]
    public async Task<IActionResult> GetUsageByEmail(string email, [FromQuery] int days = 30)
    {
        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant());

        if (user?.Organization == null)
        {
            return NotFound(new { error = "Account not found" });
        }

        var orgId = user.Organization.Id;
        var from = DateTime.UtcNow.AddDays(-days);
        var to = DateTime.UtcNow;

        var summary = await _usageTrackingService.GetUsageSummaryAsync(orgId, from, to);

        return Ok(new
        {
            email = user.Email,
            tier = user.Organization.Tier,
            period = new { from, to, days },
            usage = new
            {
                transactionCount = summary.TransactionCount,
                totalVolumeUsdc = summary.TotalVolume,
                totalFeesUsd = summary.TotalFees,
                billedFeesUsd = summary.BilledFees,
                unbilledFeesUsd = summary.UnbilledFees
            }
        });
    }

    /// <summary>
    /// Report unbilled usage to Stripe for a specific organization.
    /// Admin endpoint for manual billing triggers.
    /// </summary>
    [HttpPost("report-usage/{organizationId}")]
    public async Task<IActionResult> ReportUsage(Guid organizationId)
    {
        try
        {
            var count = await _stripeBillingService.ReportUsageAsync(organizationId);
            return Ok(new { success = true, usageRecordsReported = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report usage for org {OrgId}", organizationId);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Report unbilled usage to Stripe for all pay-as-you-go organizations.
    /// Admin endpoint for monthly billing run.
    /// </summary>
    [HttpPost("report-all-usage")]
    public async Task<IActionResult> ReportAllUsage()
    {
        try
        {
            var count = await _stripeBillingService.ReportAllUsageAsync();
            return Ok(new { success = true, totalUsageRecordsReported = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report all usage");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get unbilled fees for an organization.
    /// </summary>
    [HttpGet("unbilled/{organizationId}")]
    public async Task<IActionResult> GetUnbilledFees(Guid organizationId)
    {
        var org = await _db.Organizations.FindAsync(organizationId);
        if (org == null)
        {
            return NotFound(new { error = "Organization not found" });
        }

        var unbilledFees = await _usageTrackingService.GetUnbilledFeesAsync(organizationId);
        var unbilledUsage = await _usageTrackingService.GetUnbilledUsageAsync(organizationId);

        return Ok(new
        {
            organizationId,
            tier = org.Tier,
            unbilledFeesUsd = unbilledFees,
            unbilledTransactionCount = unbilledUsage.Count(),
            hasSubscription = !string.IsNullOrEmpty(org.StripeSubscriptionId)
        });
    }
}
