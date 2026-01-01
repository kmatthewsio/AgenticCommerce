using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;


namespace AgenticCommerce.Core.Models
{
    public class X402PaymentResponse
    {
        [JsonPropertyName("data")]
        public X402PaymentData? Data { get; set; }
    }
}
