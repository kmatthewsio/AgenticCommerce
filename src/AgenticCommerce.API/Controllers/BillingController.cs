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
    private readonly IConfiguration _configuration;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        AgenticCommerceDbContext db,
        IUsageTrackingService usageTrackingService,
        IStripeBillingService stripeBillingService,
        IConfiguration configuration,
        ILogger<BillingController> logger)
    {
        _db = db;
        _usageTrackingService = usageTrackingService;
        _stripeBillingService = stripeBillingService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get billing dashboard data for the authenticated user (JWT auth).
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize]
    public async Task<IActionResult> GetDashboard([FromQuery] int days = 30)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users
            .Include(u => u.Organization)
            .ThenInclude(o => o.ApiKeys.Where(k => k.RevokedAt == null))
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.Organization == null)
        {
            return NotFound(new { error = "Organization not found" });
        }

        var org = user.Organization;
        var from = DateTime.UtcNow.AddDays(-days);
        var to = DateTime.UtcNow;

        var summary = await _usageTrackingService.GetUsageSummaryAsync(org.Id, from, to);

        // Get current billing period info
        var currentPeriodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentPeriodEnd = currentPeriodStart.AddMonths(1).AddSeconds(-1);
        var currentPeriodSummary = await _usageTrackingService.GetUsageSummaryAsync(org.Id, currentPeriodStart, currentPeriodEnd);

        // Get daily usage for chart
        var dailyUsage = await _db.UsageEvents
            .Where(e => e.OrganizationId == org.Id && e.RecordedAt >= from && e.RecordedAt <= to)
            .GroupBy(e => e.RecordedAt.Date)
            .Select(g => new { date = g.Key, volume = g.Sum(e => e.TransactionAmount), fees = g.Sum(e => e.FeeAmount), count = g.Count() })
            .OrderBy(x => x.date)
            .ToListAsync();

        return Ok(new
        {
            organization = new
            {
                id = org.Id,
                name = org.Name,
                tier = org.Tier,
                hasStripeSubscription = !string.IsNullOrEmpty(org.StripeSubscriptionId),
                stripeCustomerId = org.StripeCustomerId
            },
            currentBillingPeriod = new
            {
                start = currentPeriodStart,
                end = currentPeriodEnd,
                transactionCount = currentPeriodSummary.TransactionCount,
                totalVolumeUsdc = currentPeriodSummary.TotalVolume,
                estimatedBillUsd = currentPeriodSummary.UnbilledFees
            },
            usage = new
            {
                period = new { from, to, days },
                transactionCount = summary.TransactionCount,
                totalVolumeUsdc = summary.TotalVolume,
                totalFeesUsd = summary.TotalFees,
                billedFeesUsd = summary.BilledFees,
                unbilledFeesUsd = summary.UnbilledFees
            },
            dailyUsage = dailyUsage.Select(d => new { d.date, d.volume, d.fees, d.count }),
            apiKeys = org.ApiKeys.Select(k => new { k.KeyPrefix, k.Environment, k.Name, k.CreatedAt })
        });
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

    /// <summary>
    /// Cron endpoint for monthly billing run.
    /// Secured with a secret token from configuration.
    ///
    /// Usage: GET /api/billing/cron/monthly?token=YOUR_CRON_SECRET
    ///
    /// Set up a cron job (e.g., Render Cron) to call this endpoint monthly:
    /// curl https://sandbox.agentrails.io/api/billing/cron/monthly?token=YOUR_CRON_SECRET
    /// </summary>
    [HttpGet("cron/monthly")]
    public async Task<IActionResult> CronMonthlyBilling([FromQuery] string? token)
    {
        // Validate cron secret
        var cronSecret = _configuration["Billing:CronSecret"];
        if (string.IsNullOrEmpty(cronSecret))
        {
            _logger.LogWarning("Billing:CronSecret not configured");
            return StatusCode(503, new { error = "Cron endpoint not configured" });
        }

        if (string.IsNullOrEmpty(token) || token != cronSecret)
        {
            _logger.LogWarning("Invalid cron token attempt");
            return Unauthorized(new { error = "Invalid token" });
        }

        _logger.LogInformation("Starting monthly billing cron job");
        var startTime = DateTime.UtcNow;

        try
        {
            var count = await _stripeBillingService.ReportAllUsageAsync();
            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "Monthly billing cron completed: {Count} usage records reported in {Duration}ms",
                count, duration.TotalMilliseconds);

            return Ok(new
            {
                success = true,
                usageRecordsReported = count,
                durationMs = duration.TotalMilliseconds,
                completedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Monthly billing cron failed");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                failedAt = DateTime.UtcNow
            });
        }
    }
}
