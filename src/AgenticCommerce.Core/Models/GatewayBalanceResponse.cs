using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public class GatewayBalanceResponse
    {
        [JsonPropertyName("data")]
        public GatewayBalanceData? Data { get; set; } 
    }
}
