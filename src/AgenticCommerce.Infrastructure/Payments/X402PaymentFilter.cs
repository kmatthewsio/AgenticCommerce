using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// Action filter that enforces x402 payment requirements.
/// Automatically returns 402 if no payment, verifies and settles if payment provided.
/// </summary>
public class X402PaymentFilter : IAsyncActionFilter
{
    private readonly IX402Service _x402Service;
    private readonly ILogger<X402PaymentFilter> _logger;

    private decimal _amountUsdc;
    private string _description = "API Request";
    private string _network = "arc-testnet";
    private List<string> _networks = new();

    public X402PaymentFilter(
        IX402Service x402Service,
        ILogger<X402PaymentFilter> logger)
    {
        _x402Service = x402Service;
        _logger = logger;
    }

    /// <summary>
    /// Configure the filter from attribute values
    /// </summary>
    public void Configure(X402PaymentAttribute attribute)
    {
        _amountUsdc = (decimal)attribute.AmountUsdc;
        _description = attribute.Description ?? $"API Request - ${attribute.AmountUsdc} USDC";
        _network = attribute.Network;

        _networks = new List<string> { _network };
        if (attribute.AllowMultipleNetworks && !string.IsNullOrEmpty(attribute.AlternativeNetworks))
        {
            _networks.AddRange(attribute.AlternativeNetworks.Split(',').Select(n => n.Trim()));
        }
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var response = context.HttpContext.Response;
        var resource = request.Path.ToString();

        // Check for payment header
        var paymentHeader = request.Headers[X402Headers.Payment].FirstOrDefault();

        if (string.IsNullOrEmpty(paymentHeader))
        {
            // No payment provided - return 402
            _logger.LogInformation(
                "No payment for {Resource}, returning 402 for {Amount} USDC",
                resource, _amountUsdc);

            Return402(context, resource);
            return;
        }

        // Decode payment payload
        var payload = _x402Service.DecodePaymentPayload(paymentHeader);
        if (payload == null)
        {
            _logger.LogWarning("Invalid payment payload encoding for {Resource}", resource);
            context.Result = new BadRequestObjectResult(new
            {
                error = "Invalid payment payload encoding"
            });
            return;
        }

        // Verify network is accepted
        if (!_networks.Contains(payload.Network))
        {
            _logger.LogWarning(
                "Network {Network} not accepted for {Resource}",
                payload.Network, resource);
            context.Result = new BadRequestObjectResult(new
            {
                error = $"Network {payload.Network} not accepted. Accepted networks: {string.Join(", ", _networks)}"
            });
            return;
        }

        // Build requirement for verification
        var requirement = new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = payload.Network,
            MaxAmountRequired = ((long)(_amountUsdc * 1_000_000)).ToString(),
            PayTo = _x402Service.GetPayToAddress(),
            Resource = resource,
            Description = _description
        };

        // Verify payment
        var verifyResult = await _x402Service.VerifyPaymentAsync(payload, requirement);
        if (!verifyResult.IsValid)
        {
            _logger.LogWarning(
                "Payment verification failed for {Resource}: {Reason}",
                resource, verifyResult.InvalidReason);
            context.Result = new BadRequestObjectResult(new
            {
                error = "Payment verification failed",
                reason = verifyResult.InvalidReason
            });
            return;
        }

        // Settle payment
        var settleResult = await _x402Service.SettlePaymentAsync(payload, requirement);
        if (!settleResult.Success)
        {
            _logger.LogError(
                "Payment settlement failed for {Resource}: {Error}",
                resource, settleResult.ErrorMessage);
            context.Result = new ObjectResult(new
            {
                error = "Payment settlement failed",
                reason = settleResult.ErrorMessage
            })
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
            return;
        }

        // Add payment response header
        response.Headers[X402Headers.PaymentResponse] = System.Text.Json.JsonSerializer.Serialize(new
        {
            success = true,
            transactionHash = settleResult.TransactionHash,
            network = settleResult.NetworkId,
            amount = _amountUsdc
        });

        // Store payment info in HttpContext for the action to access if needed
        context.HttpContext.Items["X402_Payer"] = verifyResult.Payer;
        context.HttpContext.Items["X402_TxHash"] = settleResult.TransactionHash;
        context.HttpContext.Items["X402_Amount"] = _amountUsdc;

        _logger.LogInformation(
            "Payment successful for {Resource}: {Amount} USDC from {Payer}, tx: {TxHash}",
            resource, _amountUsdc, verifyResult.Payer, settleResult.TransactionHash);

        // Continue to the action
        await next();
    }

    private void Return402(ActionExecutingContext context, string resource)
    {
        // Create payment requirements for all accepted networks
        var paymentRequired = new X402PaymentRequired
        {
            X402Version = 2,
            Accepts = _networks.Select(network => new X402PaymentRequirement
            {
                Scheme = "exact",
                Network = network,
                MaxAmountRequired = ((long)(_amountUsdc * 1_000_000)).ToString(),
                Resource = resource,
                Description = _description,
                PayTo = _x402Service.GetPayToAddress(),
                Asset = GetUsdcAddress(network),
                Extra = new X402PaymentExtra
                {
                    Name = "AgenticCommerce",
                    Version = "1.0.0",
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
                }
            }).ToList()
        };

        // Set header
        var encoded = _x402Service.EncodePaymentRequired(paymentRequired);
        context.HttpContext.Response.Headers[X402Headers.PaymentRequired] = encoded;

        // Return 402 with body
        context.Result = new ObjectResult(paymentRequired)
        {
            StatusCode = StatusCodes.Status402PaymentRequired
        };
    }

    private static string GetUsdcAddress(string network)
    {
        return X402Assets.UsdcContracts.TryGetValue(network, out var address)
            ? address
            : "0x0000000000000000000000000000000000000000";
    }
}
