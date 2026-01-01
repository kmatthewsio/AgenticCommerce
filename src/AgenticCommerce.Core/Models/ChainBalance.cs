using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AgenticCommerce.Core.Models
{
    public class ChainBalance
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }   
        [JsonPropertyName("balance")]
        public string? Balance { get; set; }

    }
}
