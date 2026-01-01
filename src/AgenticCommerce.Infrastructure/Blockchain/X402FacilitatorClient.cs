using System;
using System.Collections.Generic;
using System.Text;
using AgenticCommerce.Core.Models;
using AgenticCommerce.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AgenticCommerce.Infrastructure.Blockchain
{
    public class X402FacilitatorClient : IX402FacilitatorClient
    {
        private readonly HttpClient _httpClient;
        private readonly CircleOptions _circleOptions;
        private readonly ILogger<X402FacilitatorClient> _logger;

        public X402FacilitatorClient(IHttpClientFactory httpClientFactory, IOptions<CircleOptions> circleOptions, ILogger<X402FacilitatorClient> logger)
        {
            _circleOptions = circleOptions.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("X402Facilitator");
            _httpClient.BaseAddress = new Uri(_circleOptions.X402FacilitatorApiUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_circleOptions.X402ApiKey}");
        }
        public async Task<X402VerificationResult> VerifyPaymentRequestAsync(string paymentRequest)
        {
            try
            {
                _logger.LogInformation("Verifying payment request: {PaymentRequest}", paymentRequest);

                var request = new
                {
                    paymentRequest,
                    walletaddress = _circleOptions.WalletAddress
                };
                
                var response = await MakeRequestAsync(HttpMethod.Post, "/verify", request);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var verificationResponse = JsonSerializer
                    .Deserialize<X402VerificationResponse>(content);

                if(verificationResponse?.Data == null)
                {
                    throw new Exception("Invalid response from X402 Facilitator");
                }
                return new X402VerificationResult
                {
                    IsValid = verificationResponse.Data.IsValid,
                    Amount = verificationResponse.Data.Amount,
                    Recipient = verificationResponse.Data.Recipient,
                    ApiEndPoint = verificationResponse.Data.ApiEndPoint,
                    Description = verificationResponse.Data.Description
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying payment request: {PaymentRequest}", paymentRequest);
                throw;
            }
        }

        public async Task<X402PaymentResult> SubmitPaymentAsync(X402Payment payment)
        {
            try
            {
                _logger.LogInformation("Submitting x402 payment for {Amount} USDC", payment.Amount);

                var request = new
                {
                    walletId = _circleOptions.WalletId,
                    amount = payment.Amount.ToString("F6"),
                    recipient = payment.Recipient,
                    apiEndpoint = payment.ApiEndPoint,
                    metadata = payment.Metadata
                };

                var response = await MakeRequestAsync(
                    HttpMethod.Post,
                    "/payments",
                    request);

                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var paymentResponse = JsonSerializer.Deserialize<X402PaymentResponse>(content);

                if (paymentResponse?.Data == null)
                    throw new Exception("Invalid payment response");

                return new X402PaymentResult
                {
                    Success = true,
                    PaymentId = paymentResponse.Data.PaymentId,
                    TransactionHash = paymentResponse.Data.TransactionHash,
                    ApiAccessToken = paymentResponse.Data.ApiAccessToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit x402 payment");
                return new X402PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<HttpResponseMessage> MakeRequestAsync(HttpMethod method, string endpoint, 
            object? body = null)
        {
            var request = new HttpRequestMessage(method, endpoint);

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase       
                });

                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            return await _httpClient.SendAsync(request);
        }


    }
}
