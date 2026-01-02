using AgenticCommerce.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly IArcClient _arcClient;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(IArcClient arcClient, ILogger<TransactionsController> logger)
    {
        _arcClient = arcClient;
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