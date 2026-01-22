using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgenticCommerce.API.Services;

public interface IPolicyEnforcementService
{
    Task<PolicyCheckResult> CheckPolicyAsync(Guid organizationId, string agentId, decimal amount, string recipientAddress);
    Task<decimal> GetDailySpendingAsync(Guid organizationId, string agentId);
}

public class PolicyEnforcementService : IPolicyEnforcementService
{
    private readonly AgenticCommerceDbContext _db;
    private readonly ILogger<PolicyEnforcementService> _logger;

    public PolicyEnforcementService(
        AgenticCommerceDbContext db,
        ILogger<PolicyEnforcementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PolicyCheckResult> CheckPolicyAsync(
        Guid organizationId,
        string agentId,
        decimal amount,
        string recipientAddress)
    {
        // Get all active policies for this organization
        var policies = await _db.Policies
            .Where(p => p.OrganizationId == organizationId && p.Enabled)
            .ToListAsync();

        if (!policies.Any())
        {
            // No policies = allow all
            return PolicyCheckResult.Allowed();
        }

        var violations = new List<string>();

        foreach (var policy in policies)
        {
            // Check max transaction amount
            if (policy.MaxTransactionAmount.HasValue && amount > policy.MaxTransactionAmount.Value)
            {
                violations.Add($"Transaction amount ${amount:F2} exceeds policy '{policy.Name}' limit of ${policy.MaxTransactionAmount.Value:F2}");
            }

            // Check daily spending limit
            if (policy.DailySpendingLimit.HasValue)
            {
                var dailySpending = await GetDailySpendingAsync(organizationId, agentId);
                var projectedTotal = dailySpending + amount;

                if (projectedTotal > policy.DailySpendingLimit.Value)
                {
                    violations.Add($"Transaction would exceed policy '{policy.Name}' daily limit of ${policy.DailySpendingLimit.Value:F2} (current: ${dailySpending:F2}, requested: ${amount:F2})");
                }
            }

            // Check requires approval
            if (policy.RequiresApproval)
            {
                violations.Add($"Policy '{policy.Name}' requires manual approval for transactions");
            }

            // Check allowed recipients
            var allowedRecipients = policy.AllowedRecipients;
            if (allowedRecipients.Any() && !string.IsNullOrEmpty(recipientAddress))
            {
                var isAllowed = allowedRecipients.Any(r =>
                    r.Equals(recipientAddress, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    violations.Add($"Recipient '{recipientAddress}' is not in the allowed list for policy '{policy.Name}'");
                }
            }
        }

        if (violations.Any())
        {
            _logger.LogWarning("Policy violation for org {OrgId}, agent {AgentId}: {Violations}",
                organizationId, agentId, string.Join("; ", violations));

            return PolicyCheckResult.Denied(violations);
        }

        _logger.LogInformation("Policy check passed for org {OrgId}, agent {AgentId}, amount ${Amount}",
            organizationId, agentId, amount);

        return PolicyCheckResult.Allowed();
    }

    public async Task<decimal> GetDailySpendingAsync(Guid organizationId, string agentId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Get all transactions for this agent today
        var dailySpending = await _db.Transactions
            .Where(t => t.AgentId == agentId &&
                       t.CreatedAt >= today &&
                       t.CreatedAt < tomorrow &&
                       t.Status == "Completed")
            .SumAsync(t => t.Amount);

        return dailySpending;
    }
}

public class PolicyCheckResult
{
    public bool IsAllowed { get; private set; }
    public List<string> Violations { get; private set; } = new();

    public static PolicyCheckResult Allowed()
    {
        return new PolicyCheckResult { IsAllowed = true };
    }

    public static PolicyCheckResult Denied(List<string> violations)
    {
        return new PolicyCheckResult
        {
            IsAllowed = false,
            Violations = violations
        };
    }

    public static PolicyCheckResult Denied(string violation)
    {
        return Denied(new List<string> { violation });
    }
}
