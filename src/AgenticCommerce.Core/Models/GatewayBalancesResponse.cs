using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public  class GatewayBalancesResponse
    {
        [JsonPropertyName("data")]
        public GatewayBalancesData? Data { get; set; }
    }
}
