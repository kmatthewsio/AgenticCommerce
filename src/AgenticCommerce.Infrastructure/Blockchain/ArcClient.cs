using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Infrastructure.Blockchain
{
    /// <summary>
    /// Configuration options for Circle API
    /// </summary>
    public class CircleOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public string EntitySecret { get; set; } = string.Empty;
        public string WalletAddress { get; set; } = string.Empty;
        public string WalletId { get; set; } = string.Empty;
        public string X402ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API endpoints
        /// </summary>
        public string WalletsApiUrl { get; set; } = "https://api.circle.com/v1/w3s";
        public string GatewayApiUrl { get; set; } = "https://gateway-api-testnet.circle.com/v1";
        public string X402FacilitatorApiUrl { get; set; } = "https://x402.org/facilitator";
    }

    /// <summary>
    /// Client for Arc blockchain via Circle's Developer Controlled Wallets API
    /// </summary>
    public class ArcClient : IArcClient
    {
        private readonly HttpClient _httpClient;
        private readonly CircleOptions _circleOptions;
        private readonly ILogger<ArcClient> _logger;
        private string? _circlePublicKey;

        public ArcClient(IOptions<CircleOptions> options, ILogger<ArcClient> logger, IHttpClientFactory httpClientFactory)
        {
            _circleOptions = options.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("CircleWallets");

            _logger.LogInformation("ArcClient initialized with wallet: {WalletAddress}", _circleOptions.WalletAddress);
        }

        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                var response = await MakeCircleRequestAsync(
                    HttpMethod.Get,
                    $"/wallets/{_circleOptions.WalletId}");

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Circle API connection test failed with status code: {StatusCode} {Content}",
                        response.StatusCode, content);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Circle API");
                return false;
            }
        }

        public Task<int> GetChainIdAsync()
        {
            return Task.FromResult(1);
        }

        public async Task<decimal> GetBalanceAsync(string? address = null)
        {
            try
            {
                var response = await MakeCircleRequestAsync(
                    HttpMethod.Get,
                    $"/wallets/{_circleOptions.WalletId}/balances");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var balanceResponse = JsonSerializer.Deserialize<WalletBalanceResponse>(content);

                var usdcBalance = balanceResponse?.Data?.TokenBalances?
                    .FirstOrDefault(b => b.Token?.Symbol?.Equals("USDC", StringComparison.OrdinalIgnoreCase) ?? false);

                if (usdcBalance != null && decimal.TryParse(usdcBalance.Amount, out var balance))
                {
                    _logger.LogInformation("Balance: {Balance} USDC", balance);
                    return balance;
                }

                return 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get balance");
                throw;
            }
        }

        public Task<decimal> GetGasPriceAsync()
        {
            return Task.FromResult(0.001m);
        }

        public async Task<string> SendUsdcAsync(string toAddress, decimal amountUsdc, int? gasLimit = null)
        {
            try
            {
                _logger.LogInformation("Sending {Amount} USDC to {Recipient}", amountUsdc, toAddress);

                // Fetch Circle's public key if not cached
                if (_circlePublicKey == null)
                {
                    _logger.LogInformation("Fetching Circle public key for entity secret encryption...");
                    _circlePublicKey = await GetCirclePublicKeyAsync();
                }

                // Encrypt entity secret for this request
                var entitySecretCiphertext = EncryptEntitySecret(_circlePublicKey, _circleOptions.EntitySecret);

                var transferRequest = new
                {
                    idempotencyKey = Guid.NewGuid().ToString(),
                    walletId = _circleOptions.WalletId,
                    blockchain = "ARC-TESTNET",
                    destinationAddress = toAddress,
                    amounts = new[] { amountUsdc.ToString("F6") },
                    fee = new
                    {
                        type = "level",
                        config = new { feeLevel = "MEDIUM" }
                    },
                    entitySecretCiphertext = entitySecretCiphertext
                };

                var response = await MakeCircleRequestAsync(
                    HttpMethod.Post,
                    "/developer/transactions/transfer",
                    transferRequest);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var transferResponse = JsonSerializer.Deserialize<CircleTransferResponse>(content);

                var transactionId = transferResponse?.Data?.Id
                    ?? throw new Exception("No transaction ID returned");

                _logger.LogInformation("Transaction initiated: {TxId}", transactionId);

                return transactionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send USDC");
                throw;
            }
        }

        public async Task<TransactionInfo?> GetTransactionAsync(string txHash)
        {
            try
            {
                var response = await MakeCircleRequestAsync(
                    HttpMethod.Get,
                    $"/transactions/{txHash}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Transaction not found: {TxHash}", txHash);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var txResponse = JsonSerializer.Deserialize<CircleTransactionResponse>(content);

                if (txResponse?.Data == null)
                    return null;

                var tx = txResponse.Data;

                return new TransactionInfo
                {
                    TxHash = tx.TxHash ?? txHash,
                    FromAddress = _circleOptions.WalletAddress,
                    ToAddress = tx.DestinationAddress ?? string.Empty,
                    AmountUsdc = decimal.TryParse(tx.Amounts?.FirstOrDefault(), out var amount) ? amount : 0m,
                    GasPriceUsdc = 0.001m,
                    GasUsed = 21000,
                    BlockNumber = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get transaction: {TxHash}", txHash);
                return null;
            }
        }

        public async Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash)
        {
            try
            {
                var response = await MakeCircleRequestAsync(
                    HttpMethod.Get,
                    $"/transactions/{txHash}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Transaction receipt not found: {TxHash}", txHash);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var txResponse = JsonSerializer.Deserialize<CircleTransactionResponse>(content);

                if (txResponse?.Data == null)
                    return null;

                var tx = txResponse.Data;
                var isConfirmed = tx.State == "COMPLETE" || tx.State == "CONFIRMED";
                var isFailed = tx.State == "FAILED";
                var status = isConfirmed ? 1 : (isFailed ? 0 : -1);

                return new TransactionReceipt
                {
                    TxHash = tx.TxHash ?? txHash,
                    BlockNumber = 0,
                    FromAddress = _circleOptions.WalletAddress,
                    ToAddress = tx.DestinationAddress ?? string.Empty,
                    Status = status,
                    GasUsed = 21000
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get transaction receipt: {TxHash}", txHash);
                return null;
            }
        }

        public async Task<TransactionReceipt> WaitForTransactionReceiptAsync(string txHash, int timeoutSeconds = 120)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
                {
                    var receipt = await GetTransactionReceiptAsync(txHash);

                    if (receipt != null)
                    {
                        if (receipt.Status == 1)
                        {
                            _logger.LogInformation("Transaction confirmed: {TxHash}", txHash);
                            return receipt;
                        }
                        else if (receipt.Status == 0)
                        {
                            throw new Exception($"Transaction failed: {txHash}");
                        }
                    }

                    await Task.Delay(2000);
                }

                throw new TimeoutException($"Transaction not confirmed within {timeoutSeconds} seconds: {txHash}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to wait for transaction receipt: {TxHash}", txHash);
                throw;
            }
        }

        public string GetAddress()
        {
            return _circleOptions.WalletAddress;
        }

        private async Task<HttpResponseMessage> MakeCircleRequestAsync(
            HttpMethod method,
            string endpoint,
            object? body = null)
        {
            var fullUrl = $"{_circleOptions.WalletsApiUrl}{endpoint}";

            _logger.LogInformation("Making Circle API request: {Method} {Url}", method, fullUrl);

            var request = new HttpRequestMessage(method, fullUrl);
            request.Headers.Add("Authorization", $"Bearer {_circleOptions.ApiKey}");

            // GET requests don't need entity secret

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// Fetch Circle's public key for encrypting entity secret
        /// </summary>
        private async Task<string> GetCirclePublicKeyAsync()
        {
            try
            {
                var fullUrl = $"{_circleOptions.WalletsApiUrl}/config/entity/publicKey";
                var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                request.Headers.Add("Authorization", $"Bearer {_circleOptions.ApiKey}");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(content);

                return json
                    .GetProperty("data")
                    .GetProperty("publicKey")
                    .GetString()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch Circle public key");
                throw;
            }
        }

        /// <summary>
        /// Convert hex string to bytes
        /// </summary>
        private static byte[] HexToBytes(string hex)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex[2..];

            if (hex.Length % 2 != 0)
                throw new Exception("Hex string must have even length");

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);

            return bytes;
        }

        /// <summary>
        /// Encrypt entity secret using Circle's public key (RSA-OAEP-SHA256)
        /// </summary>
        private string EncryptEntitySecret(string publicKeyPem, string entitySecretHex)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var entitySecretBytes = HexToBytes(entitySecretHex);
            var ciphertext = rsa.Encrypt(
                entitySecretBytes,
                RSAEncryptionPadding.OaepSHA256
            );

            return Convert.ToBase64String(ciphertext);
        }
    }

    // Circle API Response Models
    internal class WalletBalanceResponse
    {
        [JsonPropertyName("data")]
        public WalletBalanceData? Data { get; set; }
    }

    internal class WalletBalanceData
    {
        [JsonPropertyName("tokenBalances")]
        public List<WalletTokenBalance>? TokenBalances { get; set; }
    }

    internal class WalletTokenBalance
    {
        [JsonPropertyName("token")]
        public WalletToken? Token { get; set; }

        [JsonPropertyName("amount")]
        public string? Amount { get; set; }
    }

    internal class WalletToken
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }
    }

    internal class CircleTransferResponse
    {
        [JsonPropertyName("data")]
        public CircleTransferData? Data { get; set; }
    }

    internal class CircleTransferData
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    internal class CircleTransactionResponse
    {
        [JsonPropertyName("data")]
        public CircleTransaction? Data { get; set; }
    }

    internal class CircleTransaction
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("txHash")]
        public string? TxHash { get; set; }

        [JsonPropertyName("destinationAddress")]
        public string? DestinationAddress { get; set; }

        [JsonPropertyName("amounts")]
        public List<string>? Amounts { get; set; }
    }
}