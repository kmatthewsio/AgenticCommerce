using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Models
{
    public class AgentRunResult
    {
        public string AgentId { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public double ExecutionTimeMs { get; set; }
    }
}
