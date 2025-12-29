using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public  class AgentStatus
    {
        public string AgentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal BalanceUsdc { get; set; }
        public decimal? BudgetUsdc { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalSpent { get; set; }
        public string Address { get; set; } = string.Empty;
    }
}
