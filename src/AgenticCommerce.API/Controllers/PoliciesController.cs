using AgenticCommerce.API.Middleware;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/org-policies")]
[Authorize]
public class PoliciesController : ControllerBase
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<PoliciesController> _logger;

    public PoliciesController(AgenticCommerceDbContext db, ILogger<PoliciesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> ListPolicies()
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var policies = await _db.Policies
            .Where(p => p.OrganizationId == orgId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.MaxTransactionAmount,
                p.DailySpendingLimit,
                p.RequiresApproval,
                allowedRecipients = p.AllowedRecipients,
                p.Enabled,
                p.CreatedAt
            })
            .ToListAsync();

        return Ok(policies);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPolicy(Guid id)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var policy = await _db.Policies
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId.Value);

        if (policy == null)
        {
            return NotFound(new { error = "Policy not found" });
        }

        return Ok(new
        {
            policy.Id,
            policy.Name,
            policy.Description,
            policy.MaxTransactionAmount,
            policy.DailySpendingLimit,
            policy.RequiresApproval,
            allowedRecipients = policy.AllowedRecipients,
            policy.Enabled,
            policy.CreatedAt,
            policy.UpdatedAt
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Name is required" });
        }

        var policy = new Policy
        {
            OrganizationId = orgId.Value,
            Name = request.Name,
            Description = request.Description,
            MaxTransactionAmount = request.MaxTransactionAmount,
            DailySpendingLimit = request.DailySpendingLimit,
            RequiresApproval = request.RequiresApproval,
            AllowedRecipients = request.AllowedRecipients ?? new List<string>(),
            Enabled = true
        };

        _db.Policies.Add(policy);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Policy created: {Name} for org {OrgId}", request.Name, orgId.Value);

        return Ok(new
        {
            policy.Id,
            policy.Name,
            policy.Description,
            policy.MaxTransactionAmount,
            policy.DailySpendingLimit,
            policy.RequiresApproval,
            allowedRecipients = policy.AllowedRecipients,
            policy.Enabled,
            policy.CreatedAt
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePolicy(Guid id, [FromBody] UpdatePolicyRequest request)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var policy = await _db.Policies
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId.Value);

        if (policy == null)
        {
            return NotFound(new { error = "Policy not found" });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            policy.Name = request.Name;
        }

        policy.Description = request.Description ?? policy.Description;
        policy.MaxTransactionAmount = request.MaxTransactionAmount ?? policy.MaxTransactionAmount;
        policy.DailySpendingLimit = request.DailySpendingLimit ?? policy.DailySpendingLimit;
        policy.RequiresApproval = request.RequiresApproval ?? policy.RequiresApproval;

        if (request.AllowedRecipients != null)
        {
            policy.AllowedRecipients = request.AllowedRecipients;
        }

        policy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Policy updated" });
    }

    [HttpPost("{id}/toggle")]
    public async Task<IActionResult> TogglePolicy(Guid id)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var policy = await _db.Policies
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId.Value);

        if (policy == null)
        {
            return NotFound(new { error = "Policy not found" });
        }

        policy.Enabled = !policy.Enabled;
        policy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Policy {Id} toggled to {Enabled}", id, policy.Enabled);

        return Ok(new { enabled = policy.Enabled });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var orgId = HttpContext.GetOrganizationId();
        if (!orgId.HasValue)
        {
            return Unauthorized();
        }

        var policy = await _db.Policies
            .FirstOrDefaultAsync(p => p.Id == id && p.OrganizationId == orgId.Value);

        if (policy == null)
        {
            return NotFound(new { error = "Policy not found" });
        }

        _db.Policies.Remove(policy);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Policy deleted: {Id}", id);

        return Ok(new { message = "Policy deleted" });
    }
}

public record CreatePolicyRequest(
    string Name,
    string? Description,
    decimal? MaxTransactionAmount,
    decimal? DailySpendingLimit,
    bool RequiresApproval,
    List<string>? AllowedRecipients
);

public record UpdatePolicyRequest(
    string? Name,
    string? Description,
    decimal? MaxTransactionAmount,
    decimal? DailySpendingLimit,
    bool? RequiresApproval,
    List<string>? AllowedRecipients
);
