using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AgenticCommerce.Infrastructure.Agents;

/// <summary>
/// Payment operations that agents can perform
/// </summary>
public class PaymentPlugin
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<PaymentPlugin> _logger;

    public PaymentPlugin(IArcClient arcClient, ILogger<PaymentPlugin> logger)
    {
        _arcClient = arcClient;
        _logger = logger;
    }

    [KernelFunction, Description("Check the current USDC balance in the wallet")]
    public async Task<string> CheckBalance()
    {
        var balance = await _arcClient.GetBalanceAsync();
        return $"Current wallet balance: {balance:F2} USDC";
    }

    [KernelFunction, Description("Get the wallet address for receiving payments")]
    public string GetWalletAddress()
    {
        var address = _arcClient.GetAddress();
        return $"Wallet address: {address}";
    }

    [KernelFunction, Description("Check if a specific amount is within budget")]
    public string CheckBudget(decimal amount, decimal currentBudget)
    {
        if (amount <= currentBudget)
        {
            return $"Amount ${amount:F2} is within budget (${currentBudget:F2} available)";
        }
        return $"Amount ${amount:F2} exceeds budget (only ${currentBudget:F2} available)";
    }

    [KernelFunction, Description("Execute a USDC payment to a recipient address")]
    public async Task<string> ExecutePurchase(
        string recipientAddress,
        decimal amount,
        string description)
    {
        try
        {
            _logger.LogInformation(
                "Agent executing purchase: {Amount} USDC to {Recipient} - {Description}",
                amount, recipientAddress, description);

            // Execute the transaction
            var txId = await _arcClient.SendUsdcAsync(recipientAddress, amount);

            return $"Purchase successful! Transaction ID: {txId}. Amount: {amount} USDC sent to {recipientAddress}. Description: {description}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent purchase failed");
            return $"Purchase failed: {ex.Message}";
        }
    }
}

/// <summary>
/// Research and analysis capabilities for agents
/// </summary>
public class ResearchPlugin
{
    [KernelFunction, Description("Research AI/LLM API providers and their current pricing")]
    public string ResearchAIProviders()
    {
        return @"AI/LLM API Providers (January 2026 pricing):

1. OpenAI
   - GPT-4o: $2.50/1M input tokens, $10/1M output tokens
   - GPT-4o-mini: $0.15/1M input tokens, $0.60/1M output tokens
   - Best for: General purpose, reasoning tasks

2. Anthropic Claude
   - Claude 3.5 Sonnet: $3/1M input tokens, $15/1M output tokens
   - Claude 3.5 Haiku: $0.80/1M input tokens, $4/1M output tokens
   - Best for: Long context, analysis, coding

3. Google Gemini
   - Gemini 1.5 Pro: $1.25/1M input tokens, $5/1M output tokens
   - Gemini 1.5 Flash: $0.075/1M input tokens, $0.30/1M output tokens
   - Best for: Multimodal, fast responses

4. Together.ai
   - Various open models: $0.20-$0.80/1M tokens
   - Best for: Cost optimization, experimentation

Budget Recommendations:
- $10: ~500K tokens with GPT-4o-mini
- $50: ~2.5M tokens with Claude Haiku  
- $100: ~5M tokens mixed usage";
    }

    [KernelFunction, Description("Research image generation API providers and pricing")]
    public string ResearchImageProviders()
    {
        return @"Image Generation API Providers (January 2026):

1. OpenAI DALL-E 3
   - Standard (1024x1024): $0.040 per image
   - HD (1024x1792): $0.080 per image
   - Best for: High quality, simple prompts

2. Midjourney API
   - Standard plan: $10/month for 200 images
   - Pro plan: $30/month for unlimited relaxed
   - Best for: Artistic images, consistency

3. Stability.ai
   - SDXL: $0.002 per image
   - SD 3: $0.035 per image
   - Best for: Cost efficiency, customization

4. Leonardo.ai
   - Free: 150 tokens/day
   - Apprentice: $12/month unlimited
   - Best for: Game assets, iteration

Budget Recommendations:
- $10: 250 DALL-E images OR 5,000 Stability images
- $50: Full Midjourney month + extra credits
- $100: Professional tier with custom models";
    }

    [KernelFunction, Description("Analyze and compare two service options based on requirements")]
    public string CompareServices(
        string service1Name,
        decimal service1Cost,
        string service2Name,
        decimal service2Cost,
        decimal budget,
        string requirements)
    {
        var recommendation = service1Cost <= service2Cost ? service1Name : service2Name;
        var savings = Math.Abs(service1Cost - service2Cost);

        return $@"Service Comparison Analysis:

Option 1: {service1Name} - ${service1Cost:F2}
Option 2: {service2Name} - ${service2Cost:F2}

Budget Available: ${budget:F2}
Requirements: {requirements}

Analysis:
- Both options are {(service1Cost <= budget && service2Cost <= budget ? "within" : "exceeding")} budget
- Cost difference: ${savings:F2}
- Recommended: {recommendation} (better value)

Next Steps:
1. Verify service capabilities meet requirements
2. Check API availability and limits
3. Consider trial period if available
4. Make purchase decision";
    }

    [KernelFunction, Description("Calculate cost estimates for API usage")]
    public string CalculateCosts(string serviceType, int estimatedUsage, decimal pricePerUnit)
    {
        var totalCost = estimatedUsage * pricePerUnit;

        return $@"Cost Estimation:

Service Type: {serviceType}
Estimated Usage: {estimatedUsage:N0} units
Price Per Unit: ${pricePerUnit:F6}

Total Estimated Cost: ${totalCost:F2}

Usage Tiers:
- Light: {estimatedUsage * 0.5:N0} units = ${totalCost * 0.5m:F2}
- Medium: {estimatedUsage:N0} units = ${totalCost:F2}
- Heavy: {estimatedUsage * 2:N0} units = ${totalCost * 2:F2}";
    }

}

/// <summary>
/// HTTP operations with x402 V2 spec-compliant auto-pay support.
/// Uses EIP-3009 signed authorizations for payment.
/// </summary>
public class HttpPlugin
{
    private readonly Payments.X402Client _x402Client;
    private readonly ILogger<HttpPlugin> _logger;
    private readonly string _baseUrl;

    public HttpPlugin(
        Payments.X402Client x402Client,
        ILogger<HttpPlugin> logger,
        string baseUrl)
    {
        _x402Client = x402Client;
        _logger = logger;
        _baseUrl = baseUrl;
    }

    [KernelFunction, Description("Make HTTP GET request with automatic x402 payment. If the API requires payment (402), it will automatically sign and pay using EIP-3009, then retry. Pass your budget limit in USDC.")]
    public async Task<string> GetWithAutoPay(
        [Description("API endpoint path (e.g., /api/x402-example/simple) or full URL")] string endpoint,
        [Description("Maximum USDC to spend on this request (default 0.10)")] decimal maxBudgetUsdc = 0.10m)
    {
        var url = endpoint.StartsWith("http") ? endpoint : $"{_baseUrl}{endpoint}";

        _logger.LogInformation("Agent x402 request to {Url} with budget {Budget} USDC", url, maxBudgetUsdc);

        var response = await _x402Client.GetWithPaymentAsync<System.Text.Json.JsonElement>(url, maxBudgetUsdc);

        if (response.IsSuccess)
        {
            var result = response.Data.GetRawText();
            if (response.AmountPaidUsdc > 0)
            {
                _logger.LogInformation("Auto-pay successful: {Amount} USDC, TX: {TxHash}",
                    response.AmountPaidUsdc, response.TransactionHash);
                return $"[PAID {response.AmountPaidUsdc} USDC | TX: {response.TransactionHash}]\n\n{result}";
            }
            return result;
        }

        _logger.LogWarning("x402 request failed: {Error}", response.Error);
        return $"Request failed: {response.Error}";
    }
}