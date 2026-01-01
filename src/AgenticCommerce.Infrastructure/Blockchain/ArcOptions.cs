using AgenticCommerce.Core.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace AgenticCommerce.Infrastructure.Blockchain
{
    public  class ArcOptions
    {
        public string RpcUrl { get; set; } = string.Empty;
        public int ChainId { get; set; }
        public string PrivateKey { get; set; } = string.Empty;
        public string UsdcContractAddress { get; set; } = string.Empty;
        public decimal GasLimit { get; set; } = 21000;
    }
}
