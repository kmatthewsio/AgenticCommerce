using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace AgentRails.McpServer.Foundry.Tools;

public class PolicyFunctions(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [Function(nameof(ListPolicies))]
    public async Task<string> ListPolicies(
        [McpToolTrigger("list_policies", "List all organization policies that govern agent behavior and spending limits.")]
        ToolInvocationContext context)
    {
        var response = await Client.GetAsync("/api/org-policies");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(CreatePolicy))]
    public async Task<string> CreatePolicy(
        [McpToolTrigger("create_policy", "Create a new organization policy to control agent spending and approval workflows.")]
        ToolInvocationContext context,
        [McpToolProperty("name", "Name of the policy.", isRequired: true)]
        string name,
        [McpToolProperty("requires_approval", "Whether transactions under this policy require manual approval (true/false).", isRequired: true)]
        string requiresApproval,
        [McpToolProperty("description", "Optional description of the policy.")]
        string? description,
        [McpToolProperty("max_transaction_amount", "Maximum amount in USD for a single transaction.")]
        string? maxTransactionAmount,
        [McpToolProperty("daily_spending_limit", "Maximum total spend in USD per day.")]
        string? dailySpendingLimit)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["requiresApproval"] = bool.TryParse(requiresApproval, out var approval) && approval
        };
        if (!string.IsNullOrEmpty(description)) payload["description"] = description;
        if (!string.IsNullOrEmpty(maxTransactionAmount) && decimal.TryParse(maxTransactionAmount, out var maxTx))
            payload["maxTransactionAmount"] = maxTx;
        if (!string.IsNullOrEmpty(dailySpendingLimit) && decimal.TryParse(dailySpendingLimit, out var dailyLimit))
            payload["dailySpendingLimit"] = dailyLimit;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/org-policies", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    // ========================================
    // ENTERPRISE BUDGET CAP TOOLS
    // ========================================

    [Function(nameof(CreateBudgetPolicy))]
    public async Task<string> CreateBudgetPolicy(
        [McpToolTrigger("create_budget_policy", "Create a budget policy with per-agent spending limits (daily, weekly, monthly, lifetime). Uses the enterprise policy engine.")]
        ToolInvocationContext context,
        [McpToolProperty("name", "Name of the budget policy (e.g. 'Production Agent $100/day cap').", isRequired: true)]
        string name,
        [McpToolProperty("description", "Optional description of the policy.")]
        string? description,
        [McpToolProperty("daily_limit_usdc", "Maximum USDC an agent can spend per day.")]
        string? dailyLimitUsdc,
        [McpToolProperty("weekly_limit_usdc", "Maximum USDC an agent can spend per week.")]
        string? weeklyLimitUsdc,
        [McpToolProperty("monthly_limit_usdc", "Maximum USDC an agent can spend per month.")]
        string? monthlyLimitUsdc,
        [McpToolProperty("lifetime_limit_usdc", "Maximum USDC an agent can spend in total (lifetime).")]
        string? lifetimeLimitUsdc,
        [McpToolProperty("max_per_transaction_usdc", "Maximum USDC per single transaction.")]
        string? maxPerTransactionUsdc)
    {
        var rules = new List<object>();
        if (!string.IsNullOrEmpty(dailyLimitUsdc) && decimal.TryParse(dailyLimitUsdc, out var daily))
            rules.Add(new { ruleType = "DailyLimit", parameter = new { amountUsdc = daily }, isEnabled = true });
        if (!string.IsNullOrEmpty(weeklyLimitUsdc) && decimal.TryParse(weeklyLimitUsdc, out var weekly))
            rules.Add(new { ruleType = "WeeklyLimit", parameter = new { amountUsdc = weekly }, isEnabled = true });
        if (!string.IsNullOrEmpty(monthlyLimitUsdc) && decimal.TryParse(monthlyLimitUsdc, out var monthly))
            rules.Add(new { ruleType = "MonthlyLimit", parameter = new { amountUsdc = monthly }, isEnabled = true });
        if (!string.IsNullOrEmpty(lifetimeLimitUsdc) && decimal.TryParse(lifetimeLimitUsdc, out var lifetime))
            rules.Add(new { ruleType = "TotalLifetimeLimit", parameter = new { amountUsdc = lifetime }, isEnabled = true });
        if (!string.IsNullOrEmpty(maxPerTransactionUsdc) && decimal.TryParse(maxPerTransactionUsdc, out var maxTx))
            rules.Add(new { ruleType = "MaxPerTransaction", parameter = new { amountUsdc = maxTx }, isEnabled = true });

        var payload = new Dictionary<string, object?> { ["name"] = name, ["rules"] = rules };
        if (!string.IsNullOrEmpty(description)) payload["description"] = description;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/policies", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(AssignPolicyToAgent))]
    public async Task<string> AssignPolicyToAgent(
        [McpToolTrigger("assign_policy_to_agent", "Assign a budget policy to an agent. The agent's payments will be evaluated against this policy's rules.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent.", isRequired: true)]
        string agentId,
        [McpToolProperty("policy_id", "The unique identifier of the policy to assign.", isRequired: true)]
        string policyId)
    {
        var payload = new { policyId };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/agents/{Uri.EscapeDataString(agentId)}/policy", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetAgentSpending))]
    public async Task<string> GetAgentSpending(
        [McpToolTrigger("get_agent_spending", "Get an agent's current spending summary showing amounts spent today, this week, this month, lifetime, and remaining budgets vs policy limits.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent.", isRequired: true)]
        string agentId)
    {
        var response = await Client.GetAsync($"/api/agents/{Uri.EscapeDataString(agentId)}/spending");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(GetAgentPolicy))]
    public async Task<string> GetAgentPolicy(
        [McpToolTrigger("get_agent_policy", "Get the budget policy currently assigned to an agent, including all rules and limits.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent.", isRequired: true)]
        string agentId)
    {
        var response = await Client.GetAsync($"/api/agents/{Uri.EscapeDataString(agentId)}/policy");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(EvaluatePayment))]
    public async Task<string> EvaluatePayment(
        [McpToolTrigger("evaluate_payment", "Dry-run evaluate whether a payment would be allowed by an agent's policy. Does not execute any payment.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent making the payment.", isRequired: true)]
        string agentId,
        [McpToolProperty("amount_usdc", "The payment amount in USDC.", isRequired: true)]
        string amountUsdc,
        [McpToolProperty("destination", "Optional destination wallet address.")]
        string? destination,
        [McpToolProperty("network", "Optional blockchain network (e.g. 'eip155:84532').")]
        string? network,
        [McpToolProperty("resource", "Optional API resource being accessed.")]
        string? resource)
    {
        if (!decimal.TryParse(amountUsdc, out var amount))
            return JsonSerializer.Serialize(new { error = true, message = "Invalid amount_usdc value" });

        var payload = new Dictionary<string, object?> { ["agentId"] = agentId, ["amountUsdc"] = amount };
        if (!string.IsNullOrEmpty(destination)) payload["destination"] = destination;
        if (!string.IsNullOrEmpty(network)) payload["network"] = network;
        if (!string.IsNullOrEmpty(resource)) payload["resource"] = resource;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/policies/evaluate", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(RemoveAgentPolicy))]
    public async Task<string> RemoveAgentPolicy(
        [McpToolTrigger("remove_agent_policy", "Remove the budget policy from an agent. The agent will no longer have spending limits enforced.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "The unique identifier of the agent.", isRequired: true)]
        string agentId)
    {
        var response = await Client.DeleteAsync($"/api/agents/{Uri.EscapeDataString(agentId)}/policy");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return string.IsNullOrWhiteSpace(body) ? JsonSerializer.Serialize(new { success = true }) : body;
    }

    // ========================================
    // APPROVAL WORKFLOW TOOLS
    // ========================================

    [Function(nameof(ListPendingApprovals))]
    public async Task<string> ListPendingApprovals(
        [McpToolTrigger("list_pending_approvals", "List pending approval requests waiting for human review. Returns payments that exceeded the RequireApprovalAbove threshold.")]
        ToolInvocationContext context,
        [McpToolProperty("agent_id", "Optional agent ID to filter approvals for a specific agent.")]
        string? agentId)
    {
        var url = "/api/approvals";
        if (!string.IsNullOrEmpty(agentId))
            url += $"?agentId={Uri.EscapeDataString(agentId)}";

        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(ApprovePayment))]
    public async Task<string> ApprovePayment(
        [McpToolTrigger("approve_payment", "Approve a pending payment request. After approval, the agent can retry the payment and it will succeed.")]
        ToolInvocationContext context,
        [McpToolProperty("approval_id", "The approval request ID (starts with 'approval_').", isRequired: true)]
        string approvalId,
        [McpToolProperty("reviewed_by", "Optional name or identifier of the person approving.")]
        string? reviewedBy)
    {
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(reviewedBy))
            payload["reviewedBy"] = reviewedBy;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/approvals/{Uri.EscapeDataString(approvalId)}/approve", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [Function(nameof(RejectPayment))]
    public async Task<string> RejectPayment(
        [McpToolTrigger("reject_payment", "Reject a pending payment request. The agent will need a new approval if it retries.")]
        ToolInvocationContext context,
        [McpToolProperty("approval_id", "The approval request ID (starts with 'approval_').", isRequired: true)]
        string approvalId,
        [McpToolProperty("reviewed_by", "Optional name or identifier of the person rejecting.")]
        string? reviewedBy,
        [McpToolProperty("reason", "Optional reason for rejection.")]
        string? reason)
    {
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(reviewedBy))
            payload["reviewedBy"] = reviewedBy;
        if (!string.IsNullOrEmpty(reason))
            payload["reason"] = reason;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/approvals/{Uri.EscapeDataString(approvalId)}/reject", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
