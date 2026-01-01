using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public class X402VerificationResponse
    {
        [JsonPropertyName("data")]
        public X402VerificationData? Data { get; set; }
    }
}
