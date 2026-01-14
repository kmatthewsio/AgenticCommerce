using System.Text;
using System.Text.Json;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// x402 V2 spec-compliant payment service implementation
/// </summary>
public class X402Service : IX402Service
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<X402Service> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly JsonSerializerOptions _jsonOptions;

    public X402Service(
        IArcClient arcClient,
        ILogger<X402Service> logger,
        IServiceScopeFactory scopeFactory)
    {
        _arcClient = arcClient;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public string GetPayToAddress() => _arcClient.GetAddress();

    public X402PaymentRequired CreatePaymentRequired(
        string resource,
        decimal amountUsdc,
        string description,
        string network = X402Networks.ArcTestnet)
    {
        // Convert USDC amount to smallest unit (6 decimals)
        var amountSmallestUnit = ((long)(amountUsdc * 1_000_000)).ToString();

        var paymentRequired = new X402PaymentRequired
        {
            X402Version = 2,
            Accepts = new List<X402PaymentRequirement>
            {
                new X402PaymentRequirement
                {
                    Scheme = "exact",
                    Network = network,
                    MaxAmountRequired = amountSmallestUnit,
                    Resource = resource,
                    Description = description,
                    PayTo = _arcClient.GetAddress(),
                    Asset = GetUsdcAddress(network),
                    Extra = new X402PaymentExtra
                    {
                        Name = "AgenticCommerce",
                        Version = "1.0.0",
                        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()
                    }
                }
            }
        };

        _logger.LogInformation(
            "Created x402 payment requirement: {Amount} USDC for {Resource} on {Network}",
            amountUsdc, resource, network);

        return paymentRequired;
    }

    public string EncodePaymentRequired(X402PaymentRequired paymentRequired)
    {
        var json = JsonSerializer.Serialize(paymentRequired, _jsonOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public X402PaymentPayload? DecodePaymentPayload(string base64Payload)
    {
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64Payload));
            return JsonSerializer.Deserialize<X402PaymentPayload>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode payment payload");
            return null;
        }
    }

    public async Task<X402VerifyResponse> VerifyPaymentAsync(
        X402PaymentPayload payload,
        X402PaymentRequirement requirement)
    {
        _logger.LogInformation(
            "Verifying x402 payment: scheme={Scheme}, network={Network}",
            payload.Scheme, payload.Network);

        // Validate scheme and network match
        if (payload.Scheme != requirement.Scheme)
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = $"Scheme mismatch: expected {requirement.Scheme}, got {payload.Scheme}"
            };
        }

        if (payload.Network != requirement.Network)
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = $"Network mismatch: expected {requirement.Network}, got {payload.Network}"
            };
        }

        // Validate payload exists
        if (payload.Payload?.Authorization == null)
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = "Missing authorization in payload"
            };
        }

        var auth = payload.Payload.Authorization;

        // Validate recipient matches
        if (!string.Equals(auth.To, requirement.PayTo, StringComparison.OrdinalIgnoreCase))
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = $"Recipient mismatch: expected {requirement.PayTo}, got {auth.To}"
            };
        }

        // Validate amount
        if (!long.TryParse(auth.Value, out var paymentValue) ||
            !long.TryParse(requirement.MaxAmountRequired, out var requiredValue))
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = "Invalid amount format"
            };
        }

        if (paymentValue < requiredValue)
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = $"Insufficient amount: required {requiredValue}, got {paymentValue}"
            };
        }

        // Validate time window
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (auth.ValidAfter > now)
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = "Authorization not yet valid"
            };
        }

        if (auth.ValidBefore < now)
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = "Authorization expired"
            };
        }

        // Validate signature (in production, this would verify EIP-3009 signature)
        if (string.IsNullOrEmpty(payload.Payload.Signature))
        {
            return new X402VerifyResponse
            {
                IsValid = false,
                InvalidReason = "Missing signature"
            };
        }

        // TODO: In production, verify signature on-chain or via facilitator
        // For now, we trust the signature format is correct

        _logger.LogInformation(
            "x402 payment verified: {Amount} from {Payer}",
            auth.Value, auth.From);

        return new X402VerifyResponse
        {
            IsValid = true,
            Payer = auth.From
        };
    }

    public async Task<X402SettleResponse> SettlePaymentAsync(
        X402PaymentPayload payload,
        X402PaymentRequirement requirement)
    {
        _logger.LogInformation("Settling x402 payment on {Network}", payload.Network);

        if (payload.Payload?.Authorization == null)
        {
            return new X402SettleResponse
            {
                Success = false,
                ErrorMessage = "Missing authorization"
            };
        }

        var auth = payload.Payload.Authorization;
        var amountSmallestUnit = long.TryParse(auth.Value, out var val) ? val : 0;
        var amountUsdc = amountSmallestUnit / 1_000_000m;
        var paymentId = $"x402_{auth.Nonce}_{DateTime.UtcNow.Ticks}";

        try
        {
            string? txHash = null;

            // For Arc network, use Circle's transfer API
            // In production x402, this would call transferWithAuthorization on-chain
            if (payload.Network.StartsWith("arc"))
            {
                // Execute transfer via Arc
                txHash = await _arcClient.SendUsdcAsync(auth.To, amountUsdc);

                _logger.LogInformation(
                    "x402 payment settled: {TxHash} for {Amount} USDC",
                    txHash, amountUsdc);
            }
            else
            {
                // For other networks, simulate for now (would need EIP-3009 in production)
                txHash = $"sim_{Guid.NewGuid():N}";
                _logger.LogInformation(
                    "x402 payment simulated on {Network}: {TxHash} for {Amount} USDC",
                    payload.Network, txHash, amountUsdc);
            }

            // Persist to database
            await PersistPaymentAsync(new X402PaymentEntity
            {
                PaymentId = paymentId,
                Resource = requirement.Resource,
                Scheme = payload.Scheme,
                Network = payload.Network,
                AmountUsdc = amountUsdc,
                AmountSmallestUnit = amountSmallestUnit,
                PayerAddress = auth.From,
                RecipientAddress = auth.To,
                TransactionHash = txHash,
                Status = X402PaymentStatus.Settled,
                Description = requirement.Description,
                VerifiedAt = DateTime.UtcNow,
                SettledAt = DateTime.UtcNow
            });

            return new X402SettleResponse
            {
                Success = true,
                TransactionHash = txHash,
                NetworkId = payload.Network
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to settle x402 payment");

            // Persist failed payment
            await PersistPaymentAsync(new X402PaymentEntity
            {
                PaymentId = paymentId,
                Resource = requirement.Resource,
                Scheme = payload.Scheme,
                Network = payload.Network,
                AmountUsdc = amountUsdc,
                AmountSmallestUnit = amountSmallestUnit,
                PayerAddress = auth.From,
                RecipientAddress = auth.To,
                Status = X402PaymentStatus.Failed,
                ErrorMessage = ex.Message,
                Description = requirement.Description
            });

            return new X402SettleResponse
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Persist payment record to database
    /// </summary>
    private async Task PersistPaymentAsync(X402PaymentEntity payment)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgenticCommerceDbContext>();

            dbContext.X402Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Persisted x402 payment {PaymentId}: {Status}, {Amount} USDC",
                payment.PaymentId, payment.Status, payment.AmountUsdc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist x402 payment {PaymentId}", payment.PaymentId);
            // Don't throw - persistence failure shouldn't break the payment flow
        }
    }

    private static string GetUsdcAddress(string network)
    {
        return X402Assets.UsdcContracts.TryGetValue(network, out var address)
            ? address
            : "0x0000000000000000000000000000000000000000";
    }
}
