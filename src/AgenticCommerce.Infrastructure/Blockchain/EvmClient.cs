using System.Numerics;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace AgenticCommerce.Infrastructure.Blockchain;

/// <summary>
/// Configuration options for EVM client (Base, Ethereum, etc.)
/// </summary>
public class EvmClientOptions
{
    /// <summary>
    /// Private key for the facilitator wallet that submits transactions
    /// </summary>
    public string FacilitatorPrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// RPC URLs by network
    /// </summary>
    public Dictionary<string, string> RpcUrls { get; set; } = new()
    {
        [X402Networks.BaseSepolia] = "https://sepolia.base.org",
        [X402Networks.BaseMainnet] = "https://mainnet.base.org",
        [X402Networks.EthereumSepolia] = "https://rpc.sepolia.org",
        [X402Networks.EthereumMainnet] = "https://eth.llamarpc.com"
    };
}

/// <summary>
/// Generic EVM client for executing x402 payments on any EVM chain.
/// Uses EIP-3009 transferWithAuthorization for gasless USDC transfers.
/// </summary>
public class EvmClient : IEvmClient
{
    private readonly EvmClientOptions _options;
    private readonly ILogger<EvmClient> _logger;
    private readonly string _network;
    private readonly Web3 _web3;
    private readonly Account _facilitatorAccount;

    // EIP-3009 transferWithAuthorization function ABI
    private const string TransferWithAuthorizationAbi = @"[{
        ""name"": ""transferWithAuthorization"",
        ""type"": ""function"",
        ""inputs"": [
            { ""name"": ""from"", ""type"": ""address"" },
            { ""name"": ""to"", ""type"": ""address"" },
            { ""name"": ""value"", ""type"": ""uint256"" },
            { ""name"": ""validAfter"", ""type"": ""uint256"" },
            { ""name"": ""validBefore"", ""type"": ""uint256"" },
            { ""name"": ""nonce"", ""type"": ""bytes32"" },
            { ""name"": ""v"", ""type"": ""uint8"" },
            { ""name"": ""r"", ""type"": ""bytes32"" },
            { ""name"": ""s"", ""type"": ""bytes32"" }
        ],
        ""outputs"": []
    }]";

    // ERC-20 balanceOf function ABI
    private const string BalanceOfAbi = @"[{
        ""name"": ""balanceOf"",
        ""type"": ""function"",
        ""inputs"": [{ ""name"": ""account"", ""type"": ""address"" }],
        ""outputs"": [{ ""name"": """", ""type"": ""uint256"" }]
    }]";

    public EvmClient(
        IOptions<EvmClientOptions> options,
        ILogger<EvmClient> logger,
        string network)
    {
        _options = options.Value;
        _logger = logger;
        _network = network;

        if (!_options.RpcUrls.TryGetValue(network, out var rpcUrl))
        {
            throw new ArgumentException($"No RPC URL configured for network: {network}");
        }

        if (string.IsNullOrEmpty(_options.FacilitatorPrivateKey))
        {
            throw new ArgumentException("Facilitator private key is required for EVM client");
        }

        // Get chain ID for the network
        var chainId = X402Networks.ChainIds.TryGetValue(network, out var id) ? id : 1;

        // Create account from private key
        _facilitatorAccount = new Account(_options.FacilitatorPrivateKey, chainId);
        _web3 = new Web3(_facilitatorAccount, rpcUrl);

        _logger.LogInformation(
            "EvmClient initialized for {Network} (chain {ChainId}) with facilitator {Address}",
            network, chainId, _facilitatorAccount.Address);
    }

    public string GetNetwork() => _network;

    public async Task<int> GetChainIdAsync()
    {
        var chainId = await _web3.Eth.ChainId.SendRequestAsync();
        return (int)chainId.Value;
    }

    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            return blockNumber.Value > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to {Network}", _network);
            return false;
        }
    }

    public async Task<decimal> GetUsdcBalanceAsync(string address, string tokenContract)
    {
        try
        {
            var contract = _web3.Eth.GetContract(BalanceOfAbi, tokenContract);
            var balanceFunction = contract.GetFunction("balanceOf");
            var balance = await balanceFunction.CallAsync<BigInteger>(address);

            // USDC has 6 decimals
            return (decimal)balance / 1_000_000m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get USDC balance for {Address}", address);
            throw;
        }
    }

    public async Task<bool> IsTransactionConfirmedAsync(string txHash)
    {
        try
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            return receipt != null && receipt.Status.Value == 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check transaction status: {TxHash}", txHash);
            return false;
        }
    }

    public async Task<string> ExecuteTransferWithAuthorizationAsync(
        string tokenContract,
        string from,
        string to,
        string value,
        long validAfter,
        long validBefore,
        string nonce,
        string signature)
    {
        _logger.LogInformation(
            "Executing transferWithAuthorization on {Network}: {From} -> {To}, value {Value}",
            _network, from, to, value);

        try
        {
            // Parse the signature into v, r, s components
            var (v, r, s) = ParseSignature(signature);

            // Parse value as BigInteger
            var valueInt = BigInteger.Parse(value);

            // Parse nonce as bytes32
            var nonceBytes = HexToBytes32(nonce);

            // Get contract instance
            var contract = _web3.Eth.GetContract(TransferWithAuthorizationAbi, tokenContract);
            var transferFunction = contract.GetFunction("transferWithAuthorization");

            // Estimate gas
            var gas = await transferFunction.EstimateGasAsync(
                _facilitatorAccount.Address,
                null,
                null,
                from, to, valueInt,
                new BigInteger(validAfter),
                new BigInteger(validBefore),
                nonceBytes,
                v, r, s);

            _logger.LogDebug("Estimated gas: {Gas}", gas.Value);

            // Send the transaction
            var txHash = await transferFunction.SendTransactionAsync(
                _facilitatorAccount.Address,
                new HexBigInteger(gas.Value * 120 / 100), // Add 20% buffer
                null,
                from, to, valueInt,
                new BigInteger(validAfter),
                new BigInteger(validBefore),
                nonceBytes,
                v, r, s);

            _logger.LogInformation(
                "TransferWithAuthorization submitted: {TxHash} on {Network}",
                txHash, _network);

            return txHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute transferWithAuthorization on {Network}", _network);
            throw;
        }
    }

    /// <summary>
    /// Parse a 65-byte signature into v, r, s components
    /// </summary>
    private (byte v, byte[] r, byte[] s) ParseSignature(string signature)
    {
        var sigBytes = HexToBytes(signature);

        if (sigBytes.Length != 65)
        {
            throw new ArgumentException($"Invalid signature length: {sigBytes.Length}, expected 65");
        }

        var r = new byte[32];
        var s = new byte[32];
        Array.Copy(sigBytes, 0, r, 0, 32);
        Array.Copy(sigBytes, 32, s, 0, 32);
        var v = sigBytes[64];

        // Normalize v value (could be 0/1 or 27/28)
        if (v < 27)
        {
            v += 27;
        }

        return (v, r, s);
    }

    /// <summary>
    /// Convert hex string to bytes
    /// </summary>
    private static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Array.Empty<byte>();

        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex[2..];

        if (hex.Length % 2 != 0)
            hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    /// <summary>
    /// Convert hex string to 32-byte array (left-padded)
    /// </summary>
    private static byte[] HexToBytes32(string hex)
    {
        var bytes = HexToBytes(hex);
        if (bytes.Length == 32)
            return bytes;

        var result = new byte[32];
        var offset = 32 - bytes.Length;
        Array.Copy(bytes, 0, result, offset, bytes.Length);
        return result;
    }
}

/// <summary>
/// Factory for creating network-specific EVM clients
/// </summary>
public interface IEvmClientFactory
{
    /// <summary>
    /// Get an EVM client for the specified network
    /// </summary>
    IEvmClient GetClient(string network);

    /// <summary>
    /// Check if a network is supported
    /// </summary>
    bool IsNetworkSupported(string network);
}

/// <summary>
/// Factory implementation that creates and caches EVM clients per network
/// </summary>
public class EvmClientFactory : IEvmClientFactory
{
    private readonly IOptions<EvmClientOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, IEvmClient> _clients = new();
    private readonly object _lock = new();

    public EvmClientFactory(
        IOptions<EvmClientOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public IEvmClient GetClient(string network)
    {
        lock (_lock)
        {
            if (_clients.TryGetValue(network, out var existing))
            {
                return existing;
            }

            var logger = _loggerFactory.CreateLogger<EvmClient>();
            var client = new EvmClient(_options, logger, network);
            _clients[network] = client;
            return client;
        }
    }

    public bool IsNetworkSupported(string network)
    {
        return _options.Value.RpcUrls.ContainsKey(network);
    }
}
