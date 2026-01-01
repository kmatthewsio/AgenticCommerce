using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public class X402Payment
    {
        public decimal Amount { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string ApiEndPoint { get; set; } = string.Empty;
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
