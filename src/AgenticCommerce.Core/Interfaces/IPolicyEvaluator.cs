namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Interface for policy evaluation in payment flows.
/// Implemented by enterprise policy engine, optional for basic x402 usage.
/// </summary>
public interface IPolicyEvaluator
{
    /// <summary>
    /// Evaluate a payment request against the payer's policy.
    /// </summary>
    /// <param name="request">The payment evaluation request</param>
    /// <returns>Result indicating if payment is allowed</returns>
    Task<PolicyEvaluationResult> EvaluateAsync(PaymentPolicyRequest request);

    /// <summary>
    /// Record successful spending after payment settles.
    /// </summary>
    Task RecordSpendingAsync(string payerAddress, decimal amountUsdc);
}

/// <summary>
/// Request for policy evaluation.
/// </summary>
public class PaymentPolicyRequest
{
    /// <summary>
    /// The payer's wallet address (used to look up agent and policy)
    /// </summary>
    public required string PayerAddress { get; set; }

    /// <summary>
    /// Amount in USDC
    /// </summary>
    public required decimal AmountUsdc { get; set; }

    /// <summary>
    /// Destination address receiving payment
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Network the payment is on
    /// </summary>
    public string? Network { get; set; }

    /// <summary>
    /// Resource being accessed (API endpoint)
    /// </summary>
    public string? Resource { get; set; }
}

/// <summary>
/// Result of policy evaluation.
/// </summary>
public class PolicyEvaluationResult
{
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }
    public bool RequiresApproval { get; set; }
    public string? PolicyId { get; set; }
    public string? AgentId { get; set; }

    public static PolicyEvaluationResult Allowed(string? policyId = null, string? agentId = null) => new()
    {
        IsAllowed = true,
        PolicyId = policyId,
        AgentId = agentId
    };

    public static PolicyEvaluationResult Denied(string reason) => new()
    {
        IsAllowed = false,
        DenialReason = reason
    };

    public static PolicyEvaluationResult NeedsApproval(string reason) => new()
    {
        IsAllowed = false,
        RequiresApproval = true,
        DenialReason = reason
    };
}
