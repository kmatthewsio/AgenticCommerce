using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public class GatewayBalancesData
    {
        [JsonPropertyName("chains")]
        public List<ChainBalance>? Chains { get; set; }
    }
}
