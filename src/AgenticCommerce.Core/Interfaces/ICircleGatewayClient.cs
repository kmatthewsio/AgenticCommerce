namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Circle Gateway API for unified cross-chain balance aggregation
/// </summary>
public interface ICircleGatewayClient
{
    /// <summary>
    /// Get total USDC balance across all supported chains
    /// </summary>
    Task<decimal> GetTotalBalanceAsync();

    /// <summary>
    /// Get USDC balance breakdown by chain
    /// </summary>
    Task<Dictionary<string, decimal>> GetBalancesByChainAsync();

    /// <summary>
    /// Get supported blockchains
    /// </summary>
    Task<List<string>> GetSupportedChainsAsync();

    /// <summary>
    /// Check if Gateway API is available
    /// </summary>
    Task<bool> IsAvailableAsync();
}