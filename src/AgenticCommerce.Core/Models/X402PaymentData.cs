using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public class X402PaymentData
    {
        [JsonPropertyName("paymentId")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonPropertyName("transactionHash")]
        public string TransactionHash { get; set; } = string.Empty;

        [JsonPropertyName("apiAccessToken")]
        public string? ApiAccessToken { get; set; }

    }
}
