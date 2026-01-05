using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models;

/// <summary>
/// Circle Gateway balance request
/// </summary>
public class GatewayBalanceRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = "USDC";

    [JsonPropertyName("sources")]
    public List<GatewaySource> Sources { get; set; } = new();
}

/// <summary>
/// Source address to query balance for
/// </summary>
public class GatewaySource
{
    [JsonPropertyName("domain")]
    public int? Domain { get; set; }

    [JsonPropertyName("depositor")]
    public string Depositor { get; set; } = string.Empty;
}

/// <summary>
/// Circle Gateway balance response
/// </summary>
public class GatewayBalanceResponse
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("balances")]
    public List<GatewayBalance>? Balances { get; set; }
}

/// <summary>
/// Balance for a specific domain
/// </summary>
public class GatewayBalance
{
    [JsonPropertyName("domain")]
    public int Domain { get; set; }

    [JsonPropertyName("depositor")]
    public string? Depositor { get; set; }

    [JsonPropertyName("balance")]
    public string? Balance { get; set; }
}

/// <summary>
/// Circle domain identifiers for supported chains
/// </summary>
public static class CircleDomains
{
    public const int Ethereum = 0;
    public const int Avalanche = 1;
    public const int Optimism = 2;
    public const int Arbitrum = 3;
    public const int Base = 6;
    public const int Polygon = 7;
    public const int Unichain = 10;
    public const int Sonic = 13;
    public const int WorldChain = 14;
    public const int Sei = 16;
    public const int HyperEVM = 19;
    public const int Arc = 26;

    public static string GetChainName(int domain)
    {
        return domain switch
        {
            0 => "Ethereum",
            1 => "Avalanche",
            2 => "Optimism",
            3 => "Arbitrum",
            6 => "Base",
            7 => "Polygon",
            10 => "Unichain",
            13 => "Sonic",
            14 => "World Chain",
            16 => "Sei",
            19 => "HyperEVM",
            26 => "Arc",
            _ => $"Unknown ({domain})"
        };
    }

    public static List<int> GetAllDomains()
    {
        return new List<int>
        {
            Ethereum,
            Avalanche,
            Optimism,
            Arbitrum,
            Base,
            Polygon,
            Unichain,
            Sonic,
            WorldChain,
            Sei,
            HyperEVM,
            Arc
        };
    }

    /// <summary>
    /// Get domains that are available on testnet
    /// Based on error: domains 2, 3, 7, 10 are NOT available on testnet
    /// </summary>
    public static List<int> GetTestnetDomains()
    {
        return new List<int>
        {
            Ethereum,      // 0 - Available on testnet (Sepolia)
            Avalanche,     // 1 - Available on testnet (Fuji)
            // Optimism = 2 - NOT available on testnet
            // Arbitrum = 3 - NOT available on testnet
            Base,          // 6 - Available on testnet (Base Sepolia)
            // Polygon = 7 - NOT available on testnet
            // Unichain = 10 - NOT available on testnet
            Sonic,         // 13 - Available on testnet
            WorldChain,    // 14 - Available on testnet
            Sei,           // 16 - Available on testnet
            HyperEVM,      // 19 - Available on testnet
            Arc            // 26 - Available on testnet
        };
    }

}