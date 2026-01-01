using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public class X402PaymentResult
    {
        public bool Success { get; set; }
        public string? PaymentId { get; set; }
        public string? TransactionHash { get; set; } = string.Empty;
        public string? ApiAccessToken { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
