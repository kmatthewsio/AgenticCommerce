namespace AgenticCommerce.Core.Interfaces;

/// <summary>
/// Interface for generic EVM blockchain client operations.
/// Supports multiple EVM chains (Base, Ethereum, etc.) for x402 payment settlement.
/// </summary>
public interface IEvmClient
{
    /// <summary>
    /// Execute EIP-3009 transferWithAuthorization on-chain.
    /// This settles an x402 payment by calling the USDC contract.
    /// </summary>
    /// <param name="tokenContract">USDC contract address</param>
    /// <param name="from">Sender address (from signed authorization)</param>
    /// <param name="to">Recipient address</param>
    /// <param name="value">Amount in smallest unit (6 decimals for USDC)</param>
    /// <param name="validAfter">Unix timestamp after which transfer is valid</param>
    /// <param name="validBefore">Unix timestamp before which transfer is valid</param>
    /// <param name="nonce">Unique nonce (bytes32)</param>
    /// <param name="signature">65-byte ECDSA signature</param>
    /// <returns>Transaction hash</returns>
    Task<string> ExecuteTransferWithAuthorizationAsync(
        string tokenContract,
        string from,
        string to,
        string value,
        long validAfter,
        long validBefore,
        string nonce,
        string signature);

    /// <summary>
    /// Get USDC balance for an address
    /// </summary>
    Task<decimal> GetUsdcBalanceAsync(string address, string tokenContract);

    /// <summary>
    /// Check if a transaction has been confirmed
    /// </summary>
    Task<bool> IsTransactionConfirmedAsync(string txHash);

    /// <summary>
    /// Get the network identifier this client is connected to
    /// </summary>
    string GetNetwork();

    /// <summary>
    /// Get the chain ID
    /// </summary>
    Task<int> GetChainIdAsync();

    /// <summary>
    /// Check if the client is connected
    /// </summary>
    Task<bool> IsConnectedAsync();
}
