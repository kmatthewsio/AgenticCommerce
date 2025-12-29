using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AgenticCommerce.Core.Models
{
    public  class AgentInfo
    {
        public string AgentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } 
    }
}
