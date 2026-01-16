using System.Numerics;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nethereum.Signer;
using Nethereum.Util;
using System.Text.Json;

namespace AgenticCommerce.API.Controllers;

/// <summary>
/// x402 V2 spec-compliant payment endpoints.
/// Implements the x402 HTTP payment protocol for AI agent micropayments.
/// See: https://www.x402.org/ and https://github.com/coinbase/x402
/// </summary>
[ApiController]
[Route("api/x402")]
[Produces("application/json")]
[Tags("x402 Payments")]
public class X402Controller : ControllerBase
{
    private readonly IX402Service _x402Service;
    private readonly AgenticCommerceDbContext _dbContext;
    private readonly ILogger<X402Controller> _logger;

    public X402Controller(
        IX402Service x402Service,
        AgenticCommerceDbContext dbContext,
        ILogger<X402Controller> logger)
    {
        _x402Service = x402Service;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Example paid API endpoint - returns 402 with proper x402 headers
    /// Cost: $0.01 USDC
    /// </summary>
    [HttpGet("protected/analysis")]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(AnalysisResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalysis()
    {
        // Check for payment header
        var paymentHeader = Request.Headers[X402Headers.Payment].FirstOrDefault();

        if (string.IsNullOrEmpty(paymentHeader))
        {
            // No payment - return 402 with payment requirements
            return Return402PaymentRequired(
                resource: "/api/x402/protected/analysis",
                amountUsdc: 0.01m,
                description: "AI Analysis API - $0.01 per request"
            );
        }

        // Decode and verify payment
        var payload = _x402Service.DecodePaymentPayload(paymentHeader);
        if (payload == null)
        {
            return BadRequest(new { error = "Invalid payment payload encoding" });
        }

        // Get the requirement that was used
        var requirement = new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = payload.Network,
            MaxAmountRequired = "10000", // 0.01 USDC in smallest unit
            PayTo = _x402Service.GetPayToAddress(),
            Resource = "/api/x402/protected/analysis"
        };

        // Verify payment
        var verifyResult = await _x402Service.VerifyPaymentAsync(payload, requirement);
        if (!verifyResult.IsValid)
        {
            _logger.LogWarning("Payment verification failed: {Reason}", verifyResult.InvalidReason);
            return BadRequest(new { error = verifyResult.InvalidReason });
        }

        // Settle payment
        var settleResult = await _x402Service.SettlePaymentAsync(payload, requirement);
        if (!settleResult.Success)
        {
            _logger.LogError("Payment settlement failed: {Error}", settleResult.ErrorMessage);
            return StatusCode(500, new { error = "Payment settlement failed", details = settleResult.ErrorMessage });
        }

        // Add payment response header
        Response.Headers[X402Headers.PaymentResponse] = JsonSerializer.Serialize(new
        {
            success = true,
            transactionHash = settleResult.TransactionHash,
            network = settleResult.NetworkId
        });

        // Return the paid content
        _logger.LogInformation(
            "Paid request completed: {TxHash} from {Payer}",
            settleResult.TransactionHash, verifyResult.Payer);

        return Ok(new AnalysisResult
        {
            Result = "AI analysis complete!",
            Analysis = "This is premium AI-powered analysis data that required x402 payment.",
            Timestamp = DateTime.UtcNow,
            CostUsdc = 0.01m,
            TransactionHash = settleResult.TransactionHash,
            Payer = verifyResult.Payer
        });
    }

    /// <summary>
    /// Example paid data endpoint - $0.001 per request (micropayment)
    /// </summary>
    [HttpGet("protected/data")]
    [ProducesResponseType(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetData()
    {
        var paymentHeader = Request.Headers[X402Headers.Payment].FirstOrDefault();

        if (string.IsNullOrEmpty(paymentHeader))
        {
            return Return402PaymentRequired(
                resource: "/api/x402/protected/data",
                amountUsdc: 0.001m,
                description: "Data API - $0.001 per request"
            );
        }

        var payload = _x402Service.DecodePaymentPayload(paymentHeader);
        if (payload == null)
        {
            return BadRequest(new { error = "Invalid payment payload" });
        }

        var requirement = new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = payload.Network,
            MaxAmountRequired = "1000", // 0.001 USDC
            PayTo = _x402Service.GetPayToAddress(),
            Resource = "/api/x402/protected/data"
        };

        var verifyResult = await _x402Service.VerifyPaymentAsync(payload, requirement);
        if (!verifyResult.IsValid)
        {
            return BadRequest(new { error = verifyResult.InvalidReason });
        }

        var settleResult = await _x402Service.SettlePaymentAsync(payload, requirement);
        if (!settleResult.Success)
        {
            return StatusCode(500, new { error = settleResult.ErrorMessage });
        }

        Response.Headers[X402Headers.PaymentResponse] = JsonSerializer.Serialize(new
        {
            success = true,
            transactionHash = settleResult.TransactionHash
        });

        return Ok(new
        {
            data = new[] { "premium", "data", "item1", "item2" },
            timestamp = DateTime.UtcNow,
            paidBy = verifyResult.Payer
        });
    }

    /// <summary>
    /// Facilitator verify endpoint - verify a payment payload
    /// POST /api/x402/facilitator/verify
    /// </summary>
    [HttpPost("facilitator/verify")]
    [ProducesResponseType(typeof(X402VerifyResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<X402VerifyResponse>> Verify([FromBody] X402VerifyRequest request)
    {
        _logger.LogInformation("Facilitator verify request received");

        var result = await _x402Service.VerifyPaymentAsync(
            request.PaymentPayload,
            request.PaymentRequirements);

        return Ok(result);
    }

    /// <summary>
    /// Facilitator settle endpoint - execute payment on blockchain
    /// POST /api/x402/facilitator/settle
    /// </summary>
    [HttpPost("facilitator/settle")]
    [ProducesResponseType(typeof(X402SettleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<X402SettleResponse>> Settle([FromBody] X402SettleRequest request)
    {
        _logger.LogInformation("Facilitator settle request received");

        // First verify
        var verifyResult = await _x402Service.VerifyPaymentAsync(
            request.PaymentPayload,
            request.PaymentRequirements);

        if (!verifyResult.IsValid)
        {
            return Ok(new X402SettleResponse
            {
                Success = false,
                ErrorMessage = $"Verification failed: {verifyResult.InvalidReason}"
            });
        }

        // Then settle
        var settleResult = await _x402Service.SettlePaymentAsync(
            request.PaymentPayload,
            request.PaymentRequirements);

        return Ok(settleResult);
    }

    /// <summary>
    /// Get payment requirements without triggering 402
    /// Useful for clients to know pricing before making a request
    /// </summary>
    [HttpGet("pricing")]
    [ProducesResponseType(typeof(X402PricingInfo), StatusCodes.Status200OK)]
    public IActionResult GetPricing()
    {
        return Ok(new X402PricingInfo
        {
            Endpoints = new List<X402EndpointPrice>
            {
                new X402EndpointPrice
                {
                    Resource = "/api/x402/protected/analysis",
                    AmountUsdc = 0.01m,
                    Description = "AI Analysis API"
                },
                new X402EndpointPrice
                {
                    Resource = "/api/x402/protected/data",
                    AmountUsdc = 0.001m,
                    Description = "Data API"
                }
            },
            SupportedNetworks = new[] { X402Networks.ArcTestnet, X402Networks.BaseSepolia },
            PayTo = _x402Service.GetPayToAddress()
        });
    }

    /// <summary>
    /// Get x402 payment history with optional filtering.
    /// Returns all payments processed through this facilitator.
    /// </summary>
    /// <param name="network">Filter by blockchain network (e.g., arc-testnet, base-sepolia)</param>
    /// <param name="status">Filter by status (Pending, Verified, Settled, Failed)</param>
    /// <param name="limit">Maximum number of records to return (default: 50, max: 500)</param>
    /// <response code="200">Returns list of payment records</response>
    [HttpGet("payments")]
    [ProducesResponseType(typeof(X402PaymentHistoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<X402PaymentHistoryResponse>> GetPaymentHistory(
        [FromQuery] string? network = null,
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50)
    {
        limit = Math.Min(limit, 500);

        var query = _dbContext.X402Payments.AsQueryable();

        if (!string.IsNullOrEmpty(network))
            query = query.Where(p => p.Network == network);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(p => p.Status == status);

        var payments = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new X402PaymentRecord
            {
                PaymentId = p.PaymentId,
                Resource = p.Resource,
                Network = p.Network,
                AmountUsdc = p.AmountUsdc,
                PayerAddress = p.PayerAddress,
                RecipientAddress = p.RecipientAddress,
                TransactionHash = p.TransactionHash,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                SettledAt = p.SettledAt
            })
            .ToListAsync();

        var totalCount = await query.CountAsync();

        return Ok(new X402PaymentHistoryResponse
        {
            Payments = payments,
            TotalCount = totalCount,
            ReturnedCount = payments.Count
        });
    }

    /// <summary>
    /// Get x402 payment statistics summary.
    /// Returns aggregated stats for the facilitator.
    /// </summary>
    /// <response code="200">Returns payment statistics</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(X402StatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<X402StatsResponse>> GetStats()
    {
        var stats = await _dbContext.X402Payments
            .GroupBy(_ => 1)
            .Select(g => new X402StatsResponse
            {
                TotalPayments = g.Count(),
                TotalVolumeUsdc = g.Sum(p => p.AmountUsdc),
                SettledPayments = g.Count(p => p.Status == X402PaymentStatus.Settled),
                FailedPayments = g.Count(p => p.Status == X402PaymentStatus.Failed)
            })
            .FirstOrDefaultAsync() ?? new X402StatsResponse();

        stats.NetworkBreakdown = await _dbContext.X402Payments
            .GroupBy(p => p.Network)
            .Select(g => new NetworkStats
            {
                Network = g.Key,
                PaymentCount = g.Count(),
                VolumeUsdc = g.Sum(p => p.AmountUsdc)
            })
            .ToListAsync();

        return Ok(stats);
    }

    #region Test Endpoints (Development Only)

    /// <summary>
    /// Generate a test payment payload for facilitator testing.
    /// Returns a payload that can be used with /facilitator/verify and /facilitator/settle.
    /// </summary>
    /// <param name="amountUsdc">Amount in USDC (default: 0.01)</param>
    /// <param name="network">Network (default: arc-testnet)</param>
    [HttpGet("test/generate-payload")]
    [ProducesResponseType(typeof(TestPayloadResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateTestPayload(
        [FromQuery] decimal amountUsdc = 0.01m,
        [FromQuery] string network = "arc-testnet")
    {
        var payToAddress = _x402Service.GetPayToAddress();
        var amountSmallestUnit = ((long)(amountUsdc * 1_000_000)).ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Create test payment payload
        var payload = new X402PaymentPayload
        {
            X402Version = 2,
            Scheme = "exact",
            Network = network,
            Payload = new X402EvmPayload
            {
                Signature = $"0xtest_signature_{nonce}", // Mock signature for testing
                Authorization = new X402Eip3009Authorization
                {
                    From = "0xTestPayer_" + nonce[..8],
                    To = payToAddress,
                    Value = amountSmallestUnit,
                    ValidAfter = now - 60,
                    ValidBefore = now + 300, // Valid for 5 minutes
                    Nonce = nonce
                }
            }
        };

        // Create matching requirement
        var requirement = new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = network,
            MaxAmountRequired = amountSmallestUnit,
            Resource = "/api/x402/test",
            Description = $"Test payment of {amountUsdc} USDC",
            PayTo = payToAddress,
            Asset = X402Assets.UsdcContracts.GetValueOrDefault(network, "0x...")
        };

        // Create the full request objects for Swagger
        var verifyRequest = new X402VerifyRequest
        {
            PaymentPayload = payload,
            PaymentRequirements = requirement
        };

        var settleRequest = new X402SettleRequest
        {
            PaymentPayload = payload,
            PaymentRequirements = requirement
        };

        return Ok(new TestPayloadResponse
        {
            Message = "Use these payloads with /facilitator/verify and /facilitator/settle",
            PaymentPayload = payload,
            PaymentRequirements = requirement,
            VerifyRequestBody = verifyRequest,
            SettleRequestBody = settleRequest,
            Base64Payload = _x402Service.EncodePaymentRequired(new X402PaymentRequired
            {
                Accepts = new List<X402PaymentRequirement> { requirement }
            })
        });
    }

    /// <summary>
    /// Generate a REAL verifiable payment payload using a test private key.
    /// This creates a cryptographically valid signature that will pass verification.
    /// Uses Hardhat test account #0 (NEVER use in production - key is publicly known).
    /// </summary>
    /// <param name="amountUsdc">Amount in USDC (default: 0.01)</param>
    [HttpGet("test/generate-signed-payload")]
    [ProducesResponseType(typeof(TestPayloadResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateSignedTestPayload([FromQuery] decimal amountUsdc = 0.01m)
    {
        // Well-known Hardhat test private key (account #0) - NEVER use in production!
        const string testPrivateKey = "ac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80";
        const string testAddress = "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266";

        // Use Base Sepolia since it has a real USDC contract we can reference
        const string network = X402Networks.BaseSepolia;
        var tokenContract = X402Assets.UsdcContracts[network];

        var payToAddress = _x402Service.GetPayToAddress();
        var amountSmallestUnit = ((long)(amountUsdc * 1_000_000)).ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Generate proper 32-byte nonce
        var nonceBytes = Guid.NewGuid().ToByteArray().Concat(Guid.NewGuid().ToByteArray()).ToArray();
        var nonce = "0x" + BitConverter.ToString(nonceBytes).Replace("-", "").ToLowerInvariant();

        var authorization = new X402Eip3009Authorization
        {
            From = testAddress,
            To = payToAddress,
            Value = amountSmallestUnit,
            ValidAfter = 0,
            ValidBefore = now + 3600, // Valid for 1 hour
            Nonce = nonce
        };

        // Sign using EIP-712
        var signature = SignEip3009Authorization(authorization, testPrivateKey, network, tokenContract);

        var payload = new X402PaymentPayload
        {
            X402Version = 2,
            Scheme = "exact",
            Network = network,
            Payload = new X402EvmPayload
            {
                Signature = signature,
                Authorization = authorization
            }
        };

        var requirement = new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = network,
            MaxAmountRequired = amountSmallestUnit,
            Resource = "/api/x402/test",
            Description = $"Test payment of {amountUsdc} USDC",
            PayTo = payToAddress,
            Asset = tokenContract
        };

        var verifyRequest = new X402VerifyRequest
        {
            PaymentPayload = payload,
            PaymentRequirements = requirement
        };

        return Ok(new
        {
            message = "This payload has a REAL cryptographic signature and will pass verification!",
            instructions = "Copy the 'verifyRequestBody' and POST it to /api/x402/facilitator/verify",
            testAccount = testAddress,
            warning = "Test key only - never use in production",
            verifyRequestBody = verifyRequest
        });
    }

    #region EIP-712 Signing Helpers (for test endpoint only)

    private static string SignEip3009Authorization(
        X402Eip3009Authorization auth,
        string privateKey,
        string network,
        string tokenContract)
    {
        var sha3 = new Sha3Keccack();
        var chainId = X402Networks.ChainIds.TryGetValue(network, out var id) ? id : 1;

        var domainSeparator = BuildDomainSeparator(sha3, "USD Coin", "2", chainId, tokenContract);
        var structHash = BuildStructHash(sha3, auth);
        var digest = BuildEip712Digest(sha3, domainSeparator, structHash);

        var key = new EthECKey(privateKey);
        var signature = key.SignAndCalculateV(digest);

        var sigBytes = new byte[65];
        Array.Copy(signature.R, 0, sigBytes, 32 - signature.R.Length, signature.R.Length);
        Array.Copy(signature.S, 0, sigBytes, 64 - signature.S.Length, signature.S.Length);
        sigBytes[64] = signature.V[0];

        return "0x" + BitConverter.ToString(sigBytes).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] BuildDomainSeparator(Sha3Keccack sha3, string name, string version, int chainId, string verifyingContract)
    {
        var domainTypeHash = sha3.CalculateHash(
            System.Text.Encoding.UTF8.GetBytes("EIP712Domain(string name,string version,uint256 chainId,address verifyingContract)"));
        var nameHash = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(name));
        var versionHash = sha3.CalculateHash(System.Text.Encoding.UTF8.GetBytes(version));

        var encoded = new byte[32 * 5];
        Array.Copy(domainTypeHash, 0, encoded, 0, 32);
        Array.Copy(nameHash, 0, encoded, 32, 32);
        Array.Copy(versionHash, 0, encoded, 64, 32);

        var chainIdBytes = new BigInteger(chainId).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(chainIdBytes, 0, encoded, 96 + (32 - chainIdBytes.Length), chainIdBytes.Length);

        var contractBytes = HexToBytes(verifyingContract);
        Array.Copy(contractBytes, 0, encoded, 128 + (32 - contractBytes.Length), contractBytes.Length);

        return sha3.CalculateHash(encoded);
    }

    private static byte[] BuildStructHash(Sha3Keccack sha3, X402Eip3009Authorization auth)
    {
        var typeHash = sha3.CalculateHash(
            System.Text.Encoding.UTF8.GetBytes("TransferWithAuthorization(address from,address to,uint256 value,uint256 validAfter,uint256 validBefore,bytes32 nonce)"));

        var encoded = new byte[32 * 7];
        Array.Copy(typeHash, 0, encoded, 0, 32);

        var fromBytes = HexToBytes(auth.From);
        Array.Copy(fromBytes, 0, encoded, 32 + (32 - fromBytes.Length), fromBytes.Length);

        var toBytes = HexToBytes(auth.To);
        Array.Copy(toBytes, 0, encoded, 64 + (32 - toBytes.Length), toBytes.Length);

        var valueBytes = BigInteger.Parse(auth.Value).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(valueBytes, 0, encoded, 96 + (32 - valueBytes.Length), valueBytes.Length);

        var validAfterBytes = new BigInteger(auth.ValidAfter).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(validAfterBytes, 0, encoded, 128 + (32 - validAfterBytes.Length), validAfterBytes.Length);

        var validBeforeBytes = new BigInteger(auth.ValidBefore).ToByteArray(isUnsigned: true, isBigEndian: true);
        Array.Copy(validBeforeBytes, 0, encoded, 160 + (32 - validBeforeBytes.Length), validBeforeBytes.Length);

        var nonceBytes = HexToBytes(auth.Nonce);
        if (nonceBytes.Length < 32)
        {
            var paddedNonce = new byte[32];
            Array.Copy(nonceBytes, 0, paddedNonce, 32 - nonceBytes.Length, nonceBytes.Length);
            nonceBytes = paddedNonce;
        }
        Array.Copy(nonceBytes, 0, encoded, 192, 32);

        return sha3.CalculateHash(encoded);
    }

    private static byte[] BuildEip712Digest(Sha3Keccack sha3, byte[] domainSeparator, byte[] structHash)
    {
        var message = new byte[66];
        message[0] = 0x19;
        message[1] = 0x01;
        Array.Copy(domainSeparator, 0, message, 2, 32);
        Array.Copy(structHash, 0, message, 34, 32);
        return sha3.CalculateHash(message);
    }

    private static byte[] HexToBytes(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.Length % 2 != 0) hex = "0" + hex;

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    #endregion

    /// <summary>
    /// Execute a full test payment flow using Arc testnet.
    /// This will actually send USDC from the configured wallet.
    /// WARNING: Uses real testnet USDC!
    /// </summary>
    /// <param name="amountUsdc">Amount in USDC (default: 0.001 = $0.001)</param>
    /// <param name="toAddress">Recipient address (default: sends to self)</param>
    [HttpPost("test/execute-payment")]
    [ProducesResponseType(typeof(TestExecuteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TestExecuteResponse>> ExecuteTestPayment(
        [FromQuery] decimal amountUsdc = 0.001m,
        [FromQuery] string? toAddress = null)
    {
        var walletAddress = _x402Service.GetPayToAddress();
        var recipient = toAddress ?? walletAddress; // Default: send to self

        _logger.LogInformation(
            "Executing test payment: {Amount} USDC to {Recipient}",
            amountUsdc, recipient);

        // Create payment payload
        var nonce = Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var amountSmallestUnit = ((long)(amountUsdc * 1_000_000)).ToString();

        var payload = new X402PaymentPayload
        {
            X402Version = 2,
            Scheme = "exact",
            Network = X402Networks.ArcTestnet,
            Payload = new X402EvmPayload
            {
                Signature = $"0xtest_{nonce}",
                Authorization = new X402Eip3009Authorization
                {
                    From = walletAddress,
                    To = recipient,
                    Value = amountSmallestUnit,
                    ValidAfter = now - 60,
                    ValidBefore = now + 300,
                    Nonce = nonce
                }
            }
        };

        var requirement = new X402PaymentRequirement
        {
            Scheme = "exact",
            Network = X402Networks.ArcTestnet,
            MaxAmountRequired = amountSmallestUnit,
            Resource = "/api/x402/test/execute-payment",
            Description = "Test payment execution",
            PayTo = recipient
        };

        // Execute settlement (this will actually send USDC on Arc)
        var settleResult = await _x402Service.SettlePaymentAsync(payload, requirement);

        return Ok(new TestExecuteResponse
        {
            Success = settleResult.Success,
            Message = settleResult.Success
                ? $"Successfully sent {amountUsdc} USDC to {recipient}"
                : $"Payment failed: {settleResult.ErrorMessage}",
            TransactionHash = settleResult.TransactionHash,
            Network = settleResult.NetworkId,
            AmountUsdc = amountUsdc,
            FromAddress = walletAddress,
            ToAddress = recipient,
            ErrorMessage = settleResult.ErrorMessage
        });
    }

    /// <summary>
    /// Quick test to verify Circle API connectivity and wallet balance.
    /// </summary>
    [HttpGet("test/wallet-status")]
    [ProducesResponseType(typeof(WalletStatusResponse), StatusCodes.Status200OK)]
    public IActionResult GetWalletStatus()
    {
        var walletAddress = _x402Service.GetPayToAddress();

        return Ok(new WalletStatusResponse
        {
            WalletAddress = walletAddress,
            Network = X402Networks.ArcTestnet,
            IsConfigured = !string.IsNullOrEmpty(walletAddress),
            Message = string.IsNullOrEmpty(walletAddress)
                ? "Circle wallet not configured. Set Circle__WalletAddress environment variable."
                : $"Wallet configured: {walletAddress}"
        });
    }

    #endregion

    /// <summary>
    /// Returns a 402 Payment Required response with proper headers
    /// </summary>
    private IActionResult Return402PaymentRequired(string resource, decimal amountUsdc, string description)
    {
        var paymentRequired = _x402Service.CreatePaymentRequired(
            resource,
            amountUsdc,
            description);

        // Set the x402 header (Base64 encoded)
        var encodedRequirement = _x402Service.EncodePaymentRequired(paymentRequired);
        Response.Headers[X402Headers.PaymentRequired] = encodedRequirement;

        _logger.LogInformation(
            "Returning 402 for {Resource}: {Amount} USDC",
            resource, amountUsdc);

        // Also return JSON body for convenience
        return StatusCode(StatusCodes.Status402PaymentRequired, paymentRequired);
    }
}

#region Response DTOs

public class AnalysisResult
{
    public string Result { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public decimal CostUsdc { get; set; }
    public string? TransactionHash { get; set; }
    public string? Payer { get; set; }
}

public class X402PricingInfo
{
    public List<X402EndpointPrice> Endpoints { get; set; } = new();
    public string[] SupportedNetworks { get; set; } = Array.Empty<string>();
    public string PayTo { get; set; } = string.Empty;
}

public class X402EndpointPrice
{
    public string Resource { get; set; } = string.Empty;
    public decimal AmountUsdc { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class X402PaymentHistoryResponse
{
    public List<X402PaymentRecord> Payments { get; set; } = new();
    public int TotalCount { get; set; }
    public int ReturnedCount { get; set; }
}

public class X402PaymentRecord
{
    public string PaymentId { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public decimal AmountUsdc { get; set; }
    public string PayerAddress { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string? TransactionHash { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SettledAt { get; set; }
}

public class X402StatsResponse
{
    public int TotalPayments { get; set; }
    public decimal TotalVolumeUsdc { get; set; }
    public int SettledPayments { get; set; }
    public int FailedPayments { get; set; }
    public List<NetworkStats> NetworkBreakdown { get; set; } = new();
}

public class NetworkStats
{
    public string Network { get; set; } = string.Empty;
    public int PaymentCount { get; set; }
    public decimal VolumeUsdc { get; set; }
}

public class TestPayloadResponse
{
    public string Message { get; set; } = string.Empty;
    public X402PaymentPayload PaymentPayload { get; set; } = new();
    public X402PaymentRequirement PaymentRequirements { get; set; } = new();
    public X402VerifyRequest VerifyRequestBody { get; set; } = new();
    public X402SettleRequest SettleRequestBody { get; set; } = new();
    public string Base64Payload { get; set; } = string.Empty;
}

public class TestExecuteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TransactionHash { get; set; }
    public string? Network { get; set; }
    public decimal AmountUsdc { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class WalletStatusResponse
{
    public string WalletAddress { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion
