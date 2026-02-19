using AgenticCommerce.Infrastructure.Payments;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

/// <summary>
/// Example controller demonstrating x402 attribute-based payments.
/// Just add [X402Payment(amount)] to any endpoint!
/// </summary>
[ApiController]
[Route("api/x402-example")]
public class X402ExampleController : ControllerBase
{
    private readonly ILogger<X402ExampleController> _logger;

    public X402ExampleController(ILogger<X402ExampleController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simple paid endpoint - $0.01 per request
    /// The [X402Payment] attribute handles everything automatically
    /// </summary>
    [HttpGet("simple")]
    [X402Payment(0.01)]
    public IActionResult GetSimple()
    {
        return Ok(new
        {
            message = "You paid $0.01 for this response!",
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash()
        });
    }

    /// <summary>
    /// Micropayment endpoint - $0.001 per request
    /// Perfect for high-volume, low-cost API calls
    /// </summary>
    [HttpGet("micro")]
    [X402Payment(0.001, Description = "Micropayment API")]
    public IActionResult GetMicro()
    {
        return Ok(new
        {
            data = new[] { "item1", "item2", "item3" },
            cost = 0.001m,
            payer = HttpContext.GetX402Payer()
        });
    }

    /// <summary>
    /// Premium endpoint - $0.10 per request
    /// For expensive operations like AI inference
    /// </summary>
    [HttpPost("premium")]
    [X402Payment(0.10, Description = "Premium AI Analysis")]
    public IActionResult PostPremium([FromBody] PremiumRequest request)
    {
        // Simulate expensive AI operation
        return Ok(new
        {
            analysis = $"Deep analysis of: {request.Query}",
            confidence = 0.95,
            cost = 0.10m,
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash()
        });
    }

    /// <summary>
    /// Multi-network endpoint - accepts payment on multiple chains
    /// </summary>
    [HttpGet("multichain")]
    [X402Payment(0.05,
        Network = "eip155:5042002",
        AllowMultipleNetworks = true,
        AlternativeNetworks = "eip155:84532,eip155:11155111")]
    public IActionResult GetMultichain()
    {
        return Ok(new
        {
            message = "This endpoint accepts payment on Arc, Base, or Ethereum!",
            payer = HttpContext.GetX402Payer(),
            transactionHash = HttpContext.GetX402TransactionHash(),
            amountPaid = HttpContext.GetX402AmountPaid()
        });
    }

    /// <summary>
    /// Free endpoint for comparison - no payment required
    /// </summary>
    [HttpGet("free")]
    public IActionResult GetFree()
    {
        return Ok(new
        {
            message = "This endpoint is free!",
            isPaid = HttpContext.IsX402Paid()
        });
    }
}

public class PremiumRequest
{
    public string Query { get; set; } = string.Empty;
}
