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
}
