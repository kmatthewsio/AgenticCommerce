using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public class TransactionReceipt
    {
        public string TxHash { get; set; } = string.Empty;
        public long BlockNumber { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public int Status { get; set; }
        public int GasUsed { get; set; }
    }
}
