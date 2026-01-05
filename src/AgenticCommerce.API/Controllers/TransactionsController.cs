using AgenticCommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<TransactionsController> _logger;
    private readonly ICircleGatewayClient _gatewayClient;

    public TransactionsController(
        IArcClient arcClient, 
        ICircleGatewayClient gatewayClient,
        ILogger<TransactionsController> logger)
    {
        _arcClient = arcClient;
        _gatewayClient = gatewayClient;
        _logger = logger;
    }

    /// <summary>
    /// Get current wallet balance
    /// </summary>
    [HttpGet("balance")]
    public async Task<ActionResult> GetBalance()
    {
        try
        {
            var balance = await _arcClient.GetBalanceAsync();
            var address = _arcClient.GetAddress();

            return Ok(new
            {
                walletAddress = address,
                balance = balance,
                currency = "USDC"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balance");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get total USDC balance across all chains via Circle Gateway
    /// </summary>
    [HttpGet("balance/total")]
    public async Task<ActionResult> GetTotalBalance()
    {
        try
        {
            _logger.LogInformation("Attempting to get total balance from Circle Gateway");

            var total = await _gatewayClient.GetTotalBalanceAsync();

            return Ok(new
            {
                totalBalance = total,
                currency = "USDC",
                source = "Circle Gateway (all chains)"
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not available"))
        {
            _logger.LogWarning("Circle Gateway not available, falling back to Arc balance");

            // Fallback to Arc balance only
            var arcBalance = await _arcClient.GetBalanceAsync();

            return Ok(new
            {
                totalBalance = arcBalance,
                currency = "USDC",
                source = "Arc only (Gateway not available on testnet)",
                note = "Circle Gateway may not be available on testnet yet. Showing Arc balance only."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get total balance");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get USDC balance breakdown by blockchain
    /// </summary>
    [HttpGet("balance/by-chain")]
    public async Task<ActionResult> GetBalancesByChain()
    {
        try
        {
            var balances = await _gatewayClient.GetBalancesByChainAsync();

            return Ok(new
            {
                balances = balances,
                total = balances.Values.Sum(),
                currency = "USDC",
                source = "Circle Gateway"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get balances by chain");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get supported blockchains from Circle Gateway
    /// </summary>
    [HttpGet("chains")]
    public async Task<ActionResult> GetSupportedChains()
    {
        try
        {
            var chains = await _gatewayClient.GetSupportedChainsAsync();

            return Ok(new
            {
                supportedChains = chains,
                count = chains.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get supported chains");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if Circle Gateway is available
    /// </summary>
    [HttpGet("gateway/status")]
    public async Task<ActionResult> GetGatewayStatus()
    {
        try
        {
            var isAvailable = await _gatewayClient.IsAvailableAsync();

            return Ok(new
            {
                available = isAvailable,
                service = "Circle Gateway"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Gateway status");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Send USDC to an address
    /// </summary>
    [HttpPost("send")]
    public async Task<ActionResult> SendUsdc([FromBody] SendUsdcRequest request)
    {
        try
        {
            var txHash = await _arcClient.SendUsdcAsync(
                request.ToAddress,
                request.Amount);

            return Ok(new
            {
                transactionId = txHash,
                amount = request.Amount,
                recipient = request.ToAddress,
                status = "pending"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send USDC");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get transaction details
    /// </summary>
    [HttpGet("{txHash}")]
    public async Task<ActionResult> GetTransaction(string txHash)
    {
        try
        {
            var tx = await _arcClient.GetTransactionAsync(txHash);

            if (tx == null)
            {
                return NotFound(new { error = $"Transaction {txHash} not found" });
            }

            return Ok(tx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get transaction receipt (confirmation)
    /// </summary>
    [HttpGet("{txHash}/receipt")]
    public async Task<ActionResult> GetTransactionReceipt(string txHash)
    {
        try
        {
            var receipt = await _arcClient.GetTransactionReceiptAsync(txHash);

            if (receipt == null)
            {
                return NotFound(new { error = $"Receipt for {txHash} not found" });
            }

            return Ok(receipt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction receipt");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Check if Arc blockchain is connected
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        try
        {
            var isConnected = await _arcClient.IsConnectedAsync();
            var address = _arcClient.GetAddress();

            return Ok(new
            {
                connected = isConnected,
                walletAddress = address,
                blockchain = "ARC-TESTNET"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check status");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request model for sending USDC
/// </summary>
public class SendUsdcRequest
{
    public string ToAddress { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}