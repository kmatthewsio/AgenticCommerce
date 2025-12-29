using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public class TransactionInfo
    {
        public string TxHash { get; set; } = string.Empty;
        public long? BlockNumber { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public decimal AmountUsdc { get; set; }
        public int GasUsed { get; set; }
        public decimal GasPriceUsdc { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSuccess { get; set; }
    }
}
