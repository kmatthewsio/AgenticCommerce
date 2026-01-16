using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// Signs EIP-3009 transferWithAuthorization messages using EIP-712 typed data
/// </summary>
public interface IEip3009Signer
{
    /// <summary>
    /// Create a signed EIP-3009 authorization for a transfer
    /// </summary>
    /// <param name="from">Sender address (must match the signer's wallet)</param>
    /// <param name="to">Recipient address</param>
    /// <param name="value">Amount in smallest unit (e.g., USDC = 6 decimals)</param>
    /// <param name="validAfter">Unix timestamp after which the transfer is valid</param>
    /// <param name="validBefore">Unix timestamp before which the transfer is valid</param>
    /// <param name="nonce">Unique nonce (bytes32 hex)</param>
    /// <param name="network">Network identifier (e.g., "arc-testnet")</param>
    /// <param name="tokenContract">USDC contract address</param>
    /// <returns>The signature as a 0x-prefixed hex string</returns>
    Task<string> SignTransferAuthorizationAsync(
        string from,
        string to,
        string value,
        long validAfter,
        long validBefore,
        string nonce,
        string network,
        string tokenContract);
}

public class Eip3009Signer : IEip3009Signer
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<Eip3009Signer> _logger;

    // Token metadata by network (name, version, chainId)
    private static readonly Dictionary<string, (string Name, string Version, int ChainId)> TokenMetadata = new()
    {
        [X402Networks.EthereumMainnet] = ("USD Coin", "2", 1),
        [X402Networks.EthereumSepolia] = ("USD Coin", "2", 11155111),
        [X402Networks.BaseMainnet] = ("USD Coin", "2", 8453),
        [X402Networks.BaseSepolia] = ("USD Coin", "2", 84532),
        [X402Networks.ArcTestnet] = ("USD Coin", "2", 5042002),
        [X402Networks.ArcMainnet] = ("USD Coin", "2", 0),
    };

    public Eip3009Signer(IArcClient arcClient, ILogger<Eip3009Signer> logger)
    {
        _arcClient = arcClient;
        _logger = logger;
    }

    public async Task<string> SignTransferAuthorizationAsync(
        string from,
        string to,
        string value,
        long validAfter,
        long validBefore,
        string nonce,
        string network,
        string tokenContract)
    {
        try
        {
            _logger.LogInformation(
                "Signing EIP-3009 authorization: {Value} from {From} to {To} on {Network}",
                value, from, to, network);

            // Get network metadata
            if (!TokenMetadata.TryGetValue(network, out var metadata))
            {
                _logger.LogWarning("Unknown network {Network}, using default metadata", network);
                metadata = ("USD Coin", "2", 1);
            }

            // Build EIP-712 typed data JSON
            var typedData = BuildEip712TypedData(
                from, to, value, validAfter, validBefore, nonce,
                metadata.Name, metadata.Version, metadata.ChainId, tokenContract);

            var typedDataJson = JsonSerializer.Serialize(typedData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogDebug("EIP-712 typed data: {TypedData}", typedDataJson);

            // Sign using Circle's API
            var signature = await _arcClient.SignTypedDataAsync(typedDataJson);

            _logger.LogInformation("Successfully signed EIP-3009 authorization");
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sign EIP-3009 authorization");
            throw;
        }
    }

    /// <summary>
    /// Build EIP-712 typed data structure for TransferWithAuthorization
    /// </summary>
    private static Eip712TypedData BuildEip712TypedData(
        string from,
        string to,
        string value,
        long validAfter,
        long validBefore,
        string nonce,
        string tokenName,
        string tokenVersion,
        int chainId,
        string tokenContract)
    {
        return new Eip712TypedData
        {
            Types = new Eip712Types
            {
                EIP712Domain = new[]
                {
                    new Eip712TypeField { Name = "name", Type = "string" },
                    new Eip712TypeField { Name = "version", Type = "string" },
                    new Eip712TypeField { Name = "chainId", Type = "uint256" },
                    new Eip712TypeField { Name = "verifyingContract", Type = "address" }
                },
                TransferWithAuthorization = new[]
                {
                    new Eip712TypeField { Name = "from", Type = "address" },
                    new Eip712TypeField { Name = "to", Type = "address" },
                    new Eip712TypeField { Name = "value", Type = "uint256" },
                    new Eip712TypeField { Name = "validAfter", Type = "uint256" },
                    new Eip712TypeField { Name = "validBefore", Type = "uint256" },
                    new Eip712TypeField { Name = "nonce", Type = "bytes32" }
                }
            },
            PrimaryType = "TransferWithAuthorization",
            Domain = new Eip712Domain
            {
                Name = tokenName,
                Version = tokenVersion,
                ChainId = chainId,
                VerifyingContract = tokenContract
            },
            Message = new TransferWithAuthorizationMessage
            {
                From = from,
                To = to,
                Value = value,
                ValidAfter = validAfter.ToString(),
                ValidBefore = validBefore.ToString(),
                Nonce = nonce
            }
        };
    }
}

#region EIP-712 Typed Data Models

internal class Eip712TypedData
{
    [JsonPropertyName("types")]
    public Eip712Types Types { get; set; } = new();

    [JsonPropertyName("primaryType")]
    public string PrimaryType { get; set; } = string.Empty;

    [JsonPropertyName("domain")]
    public Eip712Domain Domain { get; set; } = new();

    [JsonPropertyName("message")]
    public TransferWithAuthorizationMessage Message { get; set; } = new();
}

internal class Eip712Types
{
    [JsonPropertyName("EIP712Domain")]
    public Eip712TypeField[] EIP712Domain { get; set; } = Array.Empty<Eip712TypeField>();

    [JsonPropertyName("TransferWithAuthorization")]
    public Eip712TypeField[] TransferWithAuthorization { get; set; } = Array.Empty<Eip712TypeField>();
}

internal class Eip712TypeField
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
}

internal class Eip712Domain
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("chainId")]
    public int ChainId { get; set; }

    [JsonPropertyName("verifyingContract")]
    public string VerifyingContract { get; set; } = string.Empty;
}

internal class TransferWithAuthorizationMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("validAfter")]
    public string ValidAfter { get; set; } = string.Empty;

    [JsonPropertyName("validBefore")]
    public string ValidBefore { get; set; } = string.Empty;

    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;
}

#endregion
