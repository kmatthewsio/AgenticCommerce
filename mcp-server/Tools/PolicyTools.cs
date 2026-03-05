using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AgentRails.McpServer.Tools;

[McpServerToolType]
public class PolicyTools(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("AgentRails");

    [McpServerTool, Description("List all organization policies that govern agent behavior and spending limits.")]
    public async Task<string> list_policies()
    {
        var response = await Client.GetAsync("/api/org-policies");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Create a new organization policy to control agent spending and approval workflows.")]
    public async Task<string> create_policy(
        [Description("Name of the policy.")] string name,
        [Description("Whether transactions under this policy require manual approval.")] bool requiresApproval,
        [Description("Optional description of the policy.")] string? description = null,
        [Description("Maximum amount in USD for a single transaction.")] decimal? maxTransactionAmount = null,
        [Description("Maximum total spend in USD per day.")] decimal? dailySpendingLimit = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["requiresApproval"] = requiresApproval
        };
        if (description is not null) payload["description"] = description;
        if (maxTransactionAmount is not null) payload["maxTransactionAmount"] = maxTransactionAmount;
        if (dailySpendingLimit is not null) payload["dailySpendingLimit"] = dailySpendingLimit;

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

    [McpServerTool, Description("Create a budget policy with per-agent spending limits (daily, weekly, monthly, lifetime). Uses the enterprise policy engine with fine-grained rule types.")]
    public async Task<string> create_budget_policy(
        [Description("Name of the budget policy (e.g. 'Production Agent $100/day cap').")] string name,
        [Description("Optional description of the policy.")] string? description = null,
        [Description("Maximum USDC an agent can spend per day.")] decimal? dailyLimitUsdc = null,
        [Description("Maximum USDC an agent can spend per week.")] decimal? weeklyLimitUsdc = null,
        [Description("Maximum USDC an agent can spend per month.")] decimal? monthlyLimitUsdc = null,
        [Description("Maximum USDC an agent can spend in total (lifetime).")] decimal? lifetimeLimitUsdc = null,
        [Description("Maximum USDC per single transaction.")] decimal? maxPerTransactionUsdc = null)
    {
        var rules = new List<object>();
        if (dailyLimitUsdc is not null)
            rules.Add(new { ruleType = "DailyLimit", parameter = new { amountUsdc = dailyLimitUsdc }, isEnabled = true });
        if (weeklyLimitUsdc is not null)
            rules.Add(new { ruleType = "WeeklyLimit", parameter = new { amountUsdc = weeklyLimitUsdc }, isEnabled = true });
        if (monthlyLimitUsdc is not null)
            rules.Add(new { ruleType = "MonthlyLimit", parameter = new { amountUsdc = monthlyLimitUsdc }, isEnabled = true });
        if (lifetimeLimitUsdc is not null)
            rules.Add(new { ruleType = "TotalLifetimeLimit", parameter = new { amountUsdc = lifetimeLimitUsdc }, isEnabled = true });
        if (maxPerTransactionUsdc is not null)
            rules.Add(new { ruleType = "MaxPerTransaction", parameter = new { amountUsdc = maxPerTransactionUsdc }, isEnabled = true });

        var payload = new Dictionary<string, object?> { ["name"] = name, ["rules"] = rules };
        if (description is not null) payload["description"] = description;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/policies", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Assign a budget policy to an agent. The agent's payments will be evaluated against this policy's rules.")]
    public async Task<string> assign_policy_to_agent(
        [Description("The unique identifier of the agent.")] string agentId,
        [Description("The unique identifier of the policy to assign.")] string policyId)
    {
        var payload = new { policyId };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/agents/{agentId}/policy", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get an agent's current spending summary showing amounts spent today, this week, this month, lifetime, and remaining budgets vs policy limits.")]
    public async Task<string> get_agent_spending(
        [Description("The unique identifier of the agent.")] string agentId)
    {
        var response = await Client.GetAsync($"/api/agents/{agentId}/spending");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Get the budget policy currently assigned to an agent, including all rules and limits.")]
    public async Task<string> get_agent_policy(
        [Description("The unique identifier of the agent.")] string agentId)
    {
        var response = await Client.GetAsync($"/api/agents/{agentId}/policy");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Dry-run evaluate whether a payment would be allowed by an agent's policy. Does not execute any payment.")]
    public async Task<string> evaluate_payment(
        [Description("The unique identifier of the agent making the payment.")] string agentId,
        [Description("The payment amount in USDC.")] decimal amountUsdc,
        [Description("Optional destination wallet address.")] string? destination = null,
        [Description("Optional blockchain network (e.g. 'eip155:84532').")] string? network = null,
        [Description("Optional API resource being accessed.")] string? resource = null)
    {
        var payload = new Dictionary<string, object?> { ["agentId"] = agentId, ["amountUsdc"] = amountUsdc };
        if (destination is not null) payload["destination"] = destination;
        if (network is not null) payload["network"] = network;
        if (resource is not null) payload["resource"] = resource;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/api/policies/evaluate", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Remove the budget policy from an agent. The agent will no longer have spending limits enforced.")]
    public async Task<string> remove_agent_policy(
        [Description("The unique identifier of the agent.")] string agentId)
    {
        var response = await Client.DeleteAsync($"/api/agents/{agentId}/policy");
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return string.IsNullOrWhiteSpace(body) ? JsonSerializer.Serialize(new { success = true }) : body;
    }

    // ========================================
    // APPROVAL WORKFLOW TOOLS
    // ========================================

    [McpServerTool, Description("List pending approval requests waiting for human review. Returns payments that exceeded the RequireApprovalAbove threshold and need to be approved or rejected before the agent can retry.")]
    public async Task<string> list_pending_approvals(
        [Description("Optional agent ID to filter approvals for a specific agent.")] string? agentId = null)
    {
        var url = "/api/approvals";
        if (!string.IsNullOrEmpty(agentId))
            url += $"?agentId={agentId}";

        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Approve a pending payment request. After approval, the agent can retry the payment and it will succeed. The approval remains valid until it expires (default 30 minutes).")]
    public async Task<string> approve_payment(
        [Description("The approval request ID (starts with 'approval_').")] string approvalId,
        [Description("Optional name or identifier of the person approving.")] string? reviewedBy = null)
    {
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(reviewedBy))
            payload["reviewedBy"] = reviewedBy;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/approvals/{approvalId}/approve", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }

    [McpServerTool, Description("Reject a pending payment request. The agent will need a new approval if it retries the payment.")]
    public async Task<string> reject_payment(
        [Description("The approval request ID (starts with 'approval_').")] string approvalId,
        [Description("Optional name or identifier of the person rejecting.")] string? reviewedBy = null,
        [Description("Optional reason for rejection.")] string? reason = null)
    {
        var payload = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(reviewedBy))
            payload["reviewedBy"] = reviewedBy;
        if (!string.IsNullOrEmpty(reason))
            payload["reason"] = reason;

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync($"/api/approvals/{approvalId}/reject", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            return JsonSerializer.Serialize(new { error = true, statusCode = (int)response.StatusCode, message = body });
        return body;
    }
}
