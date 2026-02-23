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
}
