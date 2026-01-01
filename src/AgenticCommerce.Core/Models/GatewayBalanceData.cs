using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public class GatewayBalanceData
    {
        [JsonPropertyName("totalBalance")]
        public decimal TotalBalance { get; set; }
    }
}
