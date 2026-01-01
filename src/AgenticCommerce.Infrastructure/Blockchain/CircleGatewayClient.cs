using AgenticCommerce.Core.Interfaces;
using AgenticCommerce.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AgenticCommerce.Infrastructure.Blockchain
{
    public class CircleGatewayClient : ICircleGatewayClient
    {
        private readonly HttpClient _httpClient;
        private readonly CircleOptions _circleOptions;
        private readonly ILogger<CircleGatewayClient> _logger;

        public CircleGatewayClient(IOptions<CircleOptions> options, ILogger<CircleGatewayClient> logger, IHttpClientFactory httpClientFactory)
        {
            _circleOptions = options.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_circleOptions.GatewayApiUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_circleOptions.ApiKey}");
            _logger.LogInformation("CircleGatewayClient initialized with wallet: {WalletAddress}", _circleOptions.WalletAddress);
        }

        public async Task<decimal> GetUnifiedBalanceAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/accounts/{_circleOptions.WalletAddress}/balance");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var balanceResponse = JsonSerializer.Deserialize<GatewayBalanceResponse>(content);

                return balanceResponse?.Data?.TotalBalance ?? 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get unified balance from Circle Gateway");
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetBalancesByChainAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/accounts/{_circleOptions.WalletAddress}/balances");

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var balancesResponse = JsonSerializer.Deserialize<GatewayBalancesResponse>(content);
                var balances = new Dictionary<string, decimal>();

                if (balancesResponse?.Data?.Chains != null)
                {
                    foreach (var chain in balancesResponse.Data.Chains)
                    {
                        if (chain.Name != null && decimal.TryParse(chain.Balance, out var balance))
                        {
                            balances[chain.Name] = balance;
                        }
                    }
                }

                return balances;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get balances by chain from Circle Gateway");
                throw;
            }

        }
    }
}
