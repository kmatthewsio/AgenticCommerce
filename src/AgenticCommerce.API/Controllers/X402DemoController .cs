using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Payments;
using Microsoft.AspNetCore.Mvc;

namespace AgenticCommerce.API.Controllers;

[ApiController]
[Route("api/x402-demo")]
public class X402DemoController : ControllerBase
{
    private readonly IX402PaymentService _paymentService;
    private readonly ILogger<X402DemoController> _logger;

    public X402DemoController(
        IX402PaymentService paymentService,
        ILogger<X402DemoController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Example paid API endpoint - requires $0.01 payment
    /// </summary>
    [HttpGet("ai-analysis")]
    [ProducesResponseType(typeof(PaymentRequirement), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(AiAnalysisResponse), StatusCodes.Status200OK)]
    public IActionResult GetAiAnalysis([FromQuery] string? paymentProof)
    {
        // If no payment proof provided, return 402 Payment Required
        if (string.IsNullOrEmpty(paymentProof))
        {
            var requirement = _paymentService.CreatePaymentRequirement(
                endpoint: "/api/x402-demo/ai-analysis",
                amount: 0.01m,
                description: "AI Analysis API Call - $0.01 per request"
            );

            return StatusCode(StatusCodes.Status402PaymentRequired, requirement);
        }

        // Payment proof provided - verify it
        // (In real implementation, deserialize and verify the proof)
        // For demo, we'll assume it's valid

        return Ok(new AiAnalysisResponse
        {
            Result = "AI analysis complete! This response cost $0.01 USDC.",
            Analysis = "Sample AI-powered analysis data...",
            Timestamp = DateTime.UtcNow,
            Cost = 0.01m
        });
    }

    /// <summary>
    /// Submit payment proof for verification
    /// </summary>
    [HttpPost("verify-payment")]
    [ProducesResponseType(typeof(PaymentVerificationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaymentVerificationResult>> VerifyPayment(
        [FromBody] PaymentProof proof)
    {
        var result = await _paymentService.VerifyPaymentAsync(proof);

        if (!result.IsValid)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

public class AiAnalysisResponse
{
    public string Result { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal Cost { get; set; }
}