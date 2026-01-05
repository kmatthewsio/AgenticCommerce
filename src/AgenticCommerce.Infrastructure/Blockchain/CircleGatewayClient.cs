using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
//using AgenticCommerce.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace AgenticCommerce.Infrastructure.Blockchain
{
    /// <summary>
    /// Circle Gateway client for unified cross-chain balance aggregation
    /// </summary>
    public class CircleGatewayClient : ICircleGatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly CircleOptions _circleOptions;
        private readonly ILogger<CircleGatewayClient> _logger;

        public CircleGatewayClient(
            HttpClient httpClient,
            IOptions<CircleOptions> circleOptions,
            ILogger<CircleGatewayClient> logger)
        {
            _httpClient = httpClient;
            _circleOptions = circleOptions.Value;
            _logger = logger;
        }

        public async Task<decimal> GetTotalBalanceAsync()
        {
            try
            {
                var balances = await GetBalancesByChainAsync();
                var total = balances.Values.Sum();

                _logger.LogInformation("Total USDC balance across all chains: {Total}", total);
                return total;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total balance from Gateway");
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetBalancesByChainAsync()
        {
            try
            {
                var fullUrl = $"{_circleOptions.GatewayApiUrl}/balances";

                _logger.LogInformation("🔍 Gateway: Wallet address being queried: {Address}", _circleOptions.WalletAddress);
                _logger.LogInformation("🔍 Gateway: Querying URL: {Url}", fullUrl);

                var sources = CircleDomains.GetTestnetDomains()
                    .Select(domain => new GatewaySource
                    {
                        Depositor = _circleOptions.WalletAddress,
                        Domain = domain
                    })
                    .ToList();

                var requestBody = new GatewayBalanceRequest
                {
                    Token = "USDC",
                    Sources = sources
                };

                // 🔍 LOG THE REQUEST BODY
                _logger.LogInformation("🔍 Gateway: Request body: {Body}",
                    JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { WriteIndented = true }));

                var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
                request.Headers.Add("Authorization", $"Bearer {_circleOptions.ApiKey}");
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gateway API error: {StatusCode} - {Error}", response.StatusCode, error);
                    response.EnsureSuccessStatusCode();
                }

                var content = await response.Content.ReadAsStringAsync();

                // 🔍 LOG THE RESPONSE
                _logger.LogInformation("🔍 Gateway: Full response: {Content}", content);

                var balanceResponse = JsonSerializer.Deserialize<GatewayBalanceResponse>(content);

                // 🔍 LOG WHAT WE PARSED
                _logger.LogInformation("🔍 Gateway: Parsed {Count} balances from response",
                    balanceResponse?.Balances?.Count ?? 0);

                var balancesByChain = new Dictionary<string, decimal>();

                if (balanceResponse?.Balances != null)
                {
                    foreach (var balance in balanceResponse.Balances)
                    {
                        // 🔍 LOG EACH BALANCE
                        _logger.LogInformation("🔍 Gateway: Domain {Domain} ({Name}): Balance string = '{Balance}'",
                            balance.Domain, CircleDomains.GetChainName(balance.Domain), balance.Balance);

                        if (balance.Balance != null && decimal.TryParse(balance.Balance, out var amount))
                        {
                            var chainName = CircleDomains.GetChainName(balance.Domain);
                            balancesByChain[chainName] = amount;

                            _logger.LogInformation(
                                "✅ Chain {Chain} (domain {Domain}): {Amount} USDC",
                                chainName, balance.Domain, amount);
                        }
                    }
                }

                return balancesByChain;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get balances by chain from Gateway");
                throw;
            }
        }

        public async Task<List<string>> GetSupportedChainsAsync()
        {
            // Return hardcoded list based on Circle's documentation
            var chains = new List<string>
        {
            "Ethereum",
            "Avalanche",
            "Optimism",
            "Arbitrum",
            "Base",
            "Polygon",
            "Unichain",
            "Sonic",
            "World Chain",
            "Sei",
            "HyperEVM",
            "Arc"
        };

            _logger.LogInformation("Supported chains: {Chains}", string.Join(", ", chains));
            return chains;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Try a simple balance query
                var requestBody = new GatewayBalanceRequest
                {
                    Token = "USDC",
                    Sources = new List<GatewaySource>
                {
                    new GatewaySource
                    {
                        Depositor = _circleOptions.WalletAddress,
                        Domain = CircleDomains.Arc // Just check Arc
                    }
                }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_circleOptions.GatewayApiUrl}/balances");
                request.Headers.Add("Authorization", $"Bearer {_circleOptions.ApiKey}");
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request);

                var isAvailable = response.IsSuccessStatusCode;
                _logger.LogInformation("Circle Gateway available: {Available}", isAvailable);

                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Circle Gateway is not available");
                return false;
            }
        }
    }
}
