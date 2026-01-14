using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;

namespace AgenticCommerce.Infrastructure.Payments;

/// <summary>
/// x402 client for AI agents to make paid API requests
/// Handles 402 responses automatically by creating and submitting payments
/// </summary>
public class X402Client
{
    private readonly HttpClient _httpClient;
    private readonly IArcClient _arcClient;
    private readonly ILogger<X402Client> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public X402Client(
        HttpClient httpClient,
        IArcClient arcClient,
        ILogger<X402Client> logger)
    {
        _httpClient = httpClient;
        _arcClient = arcClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Make a GET request that may require x402 payment
    /// Automatically handles 402 responses
    /// </summary>
    public async Task<X402Response<T>> GetWithPaymentAsync<T>(
        string url,
        decimal maxBudgetUsdc = 1.0m,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Making x402 GET request to {Url}", url);

        // First request - may get 402
        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode != HttpStatusCode.PaymentRequired)
        {
            // No payment needed - return directly
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return X402Response<T>.Success(result!, null, 0);
            }

            return X402Response<T>.Failure($"Request failed: {response.StatusCode}");
        }

        // Got 402 - parse payment requirements
        var paymentRequired = await ParsePaymentRequiredAsync(response);
        if (paymentRequired == null || paymentRequired.Accepts.Count == 0)
        {
            return X402Response<T>.Failure("Invalid 402 response - no payment requirements");
        }

        // Select first acceptable payment option
        var requirement = paymentRequired.Accepts[0];
        var amountUsdc = decimal.Parse(requirement.MaxAmountRequired) / 1_000_000m;

        _logger.LogInformation(
            "Payment required: {Amount} USDC to {PayTo} on {Network}",
            amountUsdc, requirement.PayTo, requirement.Network);

        // Check budget
        if (amountUsdc > maxBudgetUsdc)
        {
            return X402Response<T>.Failure(
                $"Payment {amountUsdc} USDC exceeds budget {maxBudgetUsdc} USDC");
        }

        // Create payment payload
        var payload = await CreatePaymentPayloadAsync(requirement);
        if (payload == null)
        {
            return X402Response<T>.Failure("Failed to create payment payload");
        }

        // Retry request with payment
        var paymentJson = JsonSerializer.Serialize(payload, _jsonOptions);
        var paymentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(paymentJson));

        var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
        retryRequest.Headers.Add(X402Headers.Payment, paymentBase64);

        var retryResponse = await _httpClient.SendAsync(retryRequest, cancellationToken);

        if (!retryResponse.IsSuccessStatusCode)
        {
            var errorContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
            return X402Response<T>.Failure($"Payment failed: {errorContent}");
        }

        // Parse successful response
        var successContent = await retryResponse.Content.ReadAsStringAsync(cancellationToken);
        var successResult = JsonSerializer.Deserialize<T>(successContent, _jsonOptions);

        // Get transaction hash from response header if present
        string? txHash = null;
        if (retryResponse.Headers.TryGetValues(X402Headers.PaymentResponse, out var paymentResponses))
        {
            var prJson = paymentResponses.FirstOrDefault();
            if (!string.IsNullOrEmpty(prJson))
            {
                var pr = JsonSerializer.Deserialize<JsonElement>(prJson);
                if (pr.TryGetProperty("transactionHash", out var txElement))
                {
                    txHash = txElement.GetString();
                }
            }
        }

        _logger.LogInformation(
            "x402 payment successful: {Amount} USDC, tx: {TxHash}",
            amountUsdc, txHash);

        return X402Response<T>.Success(successResult!, txHash, amountUsdc);
    }

    /// <summary>
    /// Parse 402 response to get payment requirements
    /// </summary>
    private async Task<X402PaymentRequired?> ParsePaymentRequiredAsync(HttpResponseMessage response)
    {
        // Try header first
        if (response.Headers.TryGetValues(X402Headers.PaymentRequired, out var headerValues))
        {
            var base64 = headerValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(base64))
            {
                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                    return JsonSerializer.Deserialize<X402PaymentRequired>(json, _jsonOptions);
                }
                catch
                {
                    _logger.LogWarning("Failed to decode payment required header");
                }
            }
        }

        // Fall back to body
        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrEmpty(body))
        {
            try
            {
                return JsonSerializer.Deserialize<X402PaymentRequired>(body, _jsonOptions);
            }
            catch
            {
                _logger.LogWarning("Failed to parse payment required body");
            }
        }

        return null;
    }

    /// <summary>
    /// Create a payment payload for the given requirement
    /// In production, this would sign an EIP-3009 authorization
    /// </summary>
    private async Task<X402PaymentPayload?> CreatePaymentPayloadAsync(X402PaymentRequirement requirement)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N");

            // For Arc network, we'll use Circle's transfer API
            // In full x402, this would be an EIP-3009 signed authorization
            var payload = new X402PaymentPayload
            {
                X402Version = 2,
                Scheme = requirement.Scheme,
                Network = requirement.Network,
                Payload = new X402EvmPayload
                {
                    // In production: actual EIP-3009 signature
                    Signature = $"0x{nonce}",
                    Authorization = new X402Eip3009Authorization
                    {
                        From = _arcClient.GetAddress(),
                        To = requirement.PayTo,
                        Value = requirement.MaxAmountRequired,
                        ValidAfter = 0,
                        ValidBefore = now + 300, // 5 minutes
                        Nonce = $"0x{nonce}"
                    }
                }
            };

            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create payment payload");
            return null;
        }
    }
}

/// <summary>
/// Response from an x402 request
/// </summary>
public class X402Response<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? TransactionHash { get; set; }
    public decimal AmountPaidUsdc { get; set; }

    public static X402Response<T> Success(T data, string? txHash, decimal amount)
    {
        return new X402Response<T>
        {
            IsSuccess = true,
            Data = data,
            TransactionHash = txHash,
            AmountPaidUsdc = amount
        };
    }

    public static X402Response<T> Failure(string error)
    {
        return new X402Response<T>
        {
            IsSuccess = false,
            Error = error
        };
    }
}
