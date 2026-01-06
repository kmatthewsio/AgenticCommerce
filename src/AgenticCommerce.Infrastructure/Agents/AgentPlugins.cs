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

    /// <summary>
    /// HTTP operations for agents with x402 auto-pay support
    /// </summary>
    public class HttpPlugin
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IArcClient _arcClient;
        private readonly ILogger<HttpPlugin> _logger;
        private readonly string _baseUrl;

        public HttpPlugin(
            IHttpClientFactory httpClientFactory,
            IArcClient arcClient,
            ILogger<HttpPlugin> logger,
            string baseUrl)
        {
            _httpClientFactory = httpClientFactory;
            _arcClient = arcClient;
            _logger = logger;
            _baseUrl = baseUrl;
        }

        [KernelFunction, Description("Make HTTP GET request with automatic x402 payment handling")]
        public async Task<string> GetWithAutoPay(string endpoint)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{_baseUrl}{endpoint}";

            _logger.LogInformation("Agent making HTTP GET request to {Url}", url);

            // First attempt - might get 402
            var response = await client.GetAsync(url);

            // Check for 402 Payment Required
            if ((int)response.StatusCode == 402)
            {
                _logger.LogInformation("Received 402 Payment Required, initiating auto-pay");

                // Parse payment requirement
                var paymentReqJson = await response.Content.ReadAsStringAsync();
                var paymentReq = System.Text.Json.JsonSerializer.Deserialize<PaymentRequirement>(
                    paymentReqJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (paymentReq == null)
                {
                    return "Failed to parse payment requirement from 402 response";
                }

                _logger.LogInformation(
                    "Payment required: {Amount} USDC to {Recipient} for {Description}",
                    paymentReq.Amount, paymentReq.RecipientAddress, paymentReq.Description);

                // Make payment
                _logger.LogInformation("Auto-paying {Amount} USDC", paymentReq.Amount);
                var txId = await _arcClient.SendUsdcAsync(paymentReq.RecipientAddress, paymentReq.Amount);

                _logger.LogInformation("Payment complete. TX: {TxId}", txId);

                // Create payment proof
                var proof = new PaymentProof
                {
                    PaymentId = paymentReq.PaymentId,
                    TransactionId = txId,
                    Amount = paymentReq.Amount,
                    SenderAddress = _arcClient.GetAddress(),
                    RecipientAddress = paymentReq.RecipientAddress,
                    PaidAt = DateTime.UtcNow
                };

                // Verify payment
                var verifyUrl = $"{_baseUrl}/api/x402-demo/verify-payment";
                var proofJson = System.Text.Json.JsonSerializer.Serialize(proof);
                var proofContent = new StringContent(
                    proofJson,
                    System.Text.Encoding.UTF8,
                    "application/json");

                var verifyResponse = await client.PostAsync(verifyUrl, proofContent);
                var verifyResult = await verifyResponse.Content.ReadAsStringAsync();

                _logger.LogInformation("Payment verification: {Result}", verifyResult);

                // Retry original request with proof
                var retryUrl = $"{url}?paymentProof=verified";
                var retryResponse = await client.GetAsync(retryUrl);

                if (retryResponse.IsSuccessStatusCode)
                {
                    var result = await retryResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("Auto-pay successful, received API response");

                    return $"Auto-pay successful!\n\nPaid: {paymentReq.Amount} USDC\nTransaction: {txId}\n\nAPI Response:\n{result}";
                }
                else
                {
                    return $"Payment succeeded but API call failed: {retryResponse.StatusCode}";
                }
            }
            else if (response.IsSuccessStatusCode)
            {
                // No payment required, return response
                var result = await response.Content.ReadAsStringAsync();
                return result;
            }
            else
            {
                return $"HTTP request failed: {response.StatusCode}";
            }
        }
    }
}