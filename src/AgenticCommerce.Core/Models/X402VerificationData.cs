using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public  class X402VerificationData
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("recipient")]
        public string Recipient { get; set; } = string.Empty;

        [JsonPropertyName("apiEndPoint")]
        public string ApiEndPoint { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

    }
}
