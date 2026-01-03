using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Infrastructure.Agents
{
    public class AIOptions
    {
        public string? OpenAIApiKey { get; set; }
        public string OpenAIModel { get; set; } = "gpt-4o";

        public string? AnthropicApiKey { get; set; } 
        public string AnthropicModel { get; set; } = "claude-opus-4-20250514";        

    }
}
