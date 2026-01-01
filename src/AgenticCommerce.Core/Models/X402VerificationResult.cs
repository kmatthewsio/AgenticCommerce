using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public class X402VerificationResult
    {
        public bool IsValid { get; set; }
        public decimal Amount { get; set; }
        public string Recipient { get; set; } = string.Empty;
        public string ApiEndPoint { get; set; } = string.Empty;
        public string? Description { get; set; }        
    }
}
