using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// x402 V2 Spec-compliant models
/// Based on: https://github.com/coinbase/x402 and https://www.x402.org/
/// </summary>

#region Payment Required (402 Response)

/// <summary>
/// Root object for 402 Payment Required response
/// Returned as JSON body and encoded in PAYMENT-REQUIRED header
/// </summary>
public class X402PaymentRequired
{
    [JsonPropertyName("x402Version")]
    public int X402Version { get; set; } = 2;

    [JsonPropertyName("accepts")]
    public List<X402PaymentRequirement> Accepts { get; set; } = new();
}

/// <summary>
/// Individual payment option the server accepts
/// </summary>
public class X402PaymentRequirement
{
    /// <summary>
    /// Payment scheme: "exact" (fixed amount) or "upto" (variable)
    /// </summary>
    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = "exact";

    /// <summary>
    /// Blockchain network (e.g., "base-sepolia", "ethereum-mainnet", "arc-testnet")
    /// </summary>
    [JsonPropertyName("network")]
    public string Network { get; set; } = string.Empty;

    /// <summary>
    /// Maximum amount required in token's smallest unit (e.g., 100000 for 0.10 USDC)
    /// </summary>
    [JsonPropertyName("maxAmountRequired")]
    public string MaxAmountRequired { get; set; } = "0";

    /// <summary>
    /// Resource being paid for
    /// </summary>
    [JsonPropertyName("resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Recipient address for payment
    /// </summary>
    [JsonPropertyName("payTo")]
    public string PayTo { get; set; } = string.Empty;

    /// <summary>
    /// Token contract address (e.g., USDC contract)
    /// </summary>
    [JsonPropertyName("asset")]
    public string Asset { get; set; } = string.Empty;

    /// <summary>
    /// Extra scheme-specific data
    /// </summary>
    [JsonPropertyName("extra")]
    public X402PaymentExtra? Extra { get; set; }
}

/// <summary>
/// Extra metadata for payment requirement
/// </summary>
public class X402PaymentExtra
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("expiresAt")]
    public long? ExpiresAt { get; set; }
}

#endregion

#region Payment Payload (Client Request)

/// <summary>
/// Payment payload sent by client in X-PAYMENT header
/// </summary>
public class X402PaymentPayload
{
    [JsonPropertyName("x402Version")]
    public int X402Version { get; set; } = 2;

    /// <summary>
    /// Payment scheme used
    /// </summary>
    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = "exact";

    /// <summary>
    /// Network payment was made on
    /// </summary>
    [JsonPropertyName("network")]
    public string Network { get; set; } = string.Empty;

    /// <summary>
    /// Scheme-specific payload (EIP-3009 or Permit2 for EVM)
    /// </summary>
    [JsonPropertyName("payload")]
    public X402EvmPayload? Payload { get; set; }
}

/// <summary>
/// EVM-specific payload for exact scheme (EIP-3009)
/// </summary>
public class X402EvmPayload
{
    /// <summary>
    /// 65-byte signature from transferWithAuthorization
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// EIP-3009 authorization parameters
    /// </summary>
    [JsonPropertyName("authorization")]
    public X402Eip3009Authorization? Authorization { get; set; }
}

/// <summary>
/// EIP-3009 transferWithAuthorization parameters
/// </summary>
public class X402Eip3009Authorization
{
    /// <summary>
    /// Sender address
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Recipient address
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Amount in smallest unit
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = "0";

    /// <summary>
    /// Unix timestamp after which transfer is valid
    /// </summary>
    [JsonPropertyName("validAfter")]
    public long ValidAfter { get; set; } = 0;

    /// <summary>
    /// Unix timestamp before which transfer is valid
    /// </summary>
    [JsonPropertyName("validBefore")]
    public long ValidBefore { get; set; }

    /// <summary>
    /// Unique nonce to prevent replay attacks
    /// </summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;
}

#endregion

#region Facilitator API Types

/// <summary>
/// Request to verify a payment payload
/// POST /verify
/// </summary>
public class X402VerifyRequest
{
    [JsonPropertyName("paymentPayload")]
    public X402PaymentPayload PaymentPayload { get; set; } = new();

    [JsonPropertyName("paymentRequirements")]
    public X402PaymentRequirement PaymentRequirements { get; set; } = new();
}

/// <summary>
/// Response from facilitator verification
/// </summary>
public class X402VerifyResponse
{
    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("invalidReason")]
    public string? InvalidReason { get; set; }

    [JsonPropertyName("payer")]
    public string? Payer { get; set; }
}

/// <summary>
/// Request to settle a payment
/// POST /settle
/// </summary>
public class X402SettleRequest
{
    [JsonPropertyName("paymentPayload")]
    public X402PaymentPayload PaymentPayload { get; set; } = new();

    [JsonPropertyName("paymentRequirements")]
    public X402PaymentRequirement PaymentRequirements { get; set; } = new();
}

/// <summary>
/// Response from facilitator settlement
/// </summary>
public class X402SettleResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("transactionHash")]
    public string? TransactionHash { get; set; }

    [JsonPropertyName("networkId")]
    public string? NetworkId { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

#endregion

#region Header Constants

/// <summary>
/// x402 HTTP header names
/// </summary>
public static class X402Headers
{
    /// <summary>
    /// Server sends: Base64-encoded PaymentRequired
    /// </summary>
    public const string PaymentRequired = "X-PAYMENT-REQUIRED";

    /// <summary>
    /// Client sends: Base64-encoded PaymentPayload
    /// </summary>
    public const string Payment = "X-PAYMENT";

    /// <summary>
    /// Server sends: Settlement confirmation
    /// </summary>
    public const string PaymentResponse = "X-PAYMENT-RESPONSE";
}

#endregion

#region Network Constants

/// <summary>
/// Supported network identifiers
/// </summary>
public static class X402Networks
{
    // === TESTNET (Free - Sandbox) ===
    public const string ArcTestnet = "arc-testnet";
    public const string BaseSepolia = "base-sepolia";
    public const string EthereumSepolia = "ethereum-sepolia";

    // === MAINNET (Requires Production Access - https://agentrails.io/#pricing) ===
    // Contact kematth.007@protonmail.com for production API keys
    public const string ArcMainnet = "arc-mainnet";
    public const string BaseMainnet = "base-mainnet";
    public const string EthereumMainnet = "ethereum-mainnet";

    /// <summary>
    /// Testnet networks available in the free sandbox
    /// </summary>
    public static readonly HashSet<string> TestnetNetworks = new()
    {
        ArcTestnet,
        BaseSepolia,
        EthereumSepolia
    };

    /// <summary>
    /// Mainnet networks requiring production access
    /// </summary>
    public static readonly HashSet<string> MainnetNetworks = new()
    {
        ArcMainnet,
        BaseMainnet,
        EthereumMainnet
    };

    /// <summary>
    /// Chain IDs for each network (used in EIP-712 domain separator)
    /// </summary>
    public static readonly Dictionary<string, int> ChainIds = new()
    {
        // Testnets (free)
        [ArcTestnet] = 5042002,
        [BaseSepolia] = 84532,
        [EthereumSepolia] = 11155111,
        // Mainnets (production access required)
        [ArcMainnet] = 0,         // Arc mainnet - not yet launched
        [BaseMainnet] = 8453,
        [EthereumMainnet] = 1
    };

    /// <summary>
    /// Check if a network requires production access
    /// </summary>
    public static bool RequiresProductionAccess(string network) => MainnetNetworks.Contains(network);
}

/// <summary>
/// USDC contract addresses by network
/// </summary>
public static class X402Assets
{
    /// <summary>
    /// USDC contract addresses by network.
    /// Note: On Arc, USDC is the native gas token with an ERC-20 interface at a special address.
    /// Mainnet contracts require production access - see https://agentrails.io/#pricing
    /// </summary>
    public static readonly Dictionary<string, string> UsdcContracts = new()
    {
        // Testnets (free sandbox)
        [X402Networks.ArcTestnet] = "0x3600000000000000000000000000000000000000", // Arc native USDC
        [X402Networks.BaseSepolia] = "0x036CbD53842c5426634e7929541eC2318f3dCF7e",
        [X402Networks.EthereumSepolia] = "0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238",
        // Mainnets (production access required)
        [X402Networks.ArcMainnet] = "0x3600000000000000000000000000000000000000",
        [X402Networks.BaseMainnet] = "0x833589fCD6eDb6E08f4c7C32D4f71b54bdA02913",
        [X402Networks.EthereumMainnet] = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48"
    };

    /// <summary>
    /// EURC (Euro Coin) contract addresses by network
    /// </summary>
    public static readonly Dictionary<string, string> EurcContracts = new()
    {
        [X402Networks.ArcTestnet] = "0x89B50855Aa3bE2F677cD6303Cec089B5F319D72a"
    };
}

#endregion
