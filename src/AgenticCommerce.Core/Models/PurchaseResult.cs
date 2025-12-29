using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public  class PurchaseResult
    {
        public bool Success { get; set; }
        public string? TxHash { get; set; }
        public decimal? AmountUsdc { get; set; }
        public string? Recipient { get; set; }
        public string? ConfirmationMessage { get; set; }
        public decimal? RemainingBudget { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
