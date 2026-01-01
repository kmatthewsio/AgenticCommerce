using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Interfaces
{
    public  interface ICircleGatewayClient
    {
        /// <summary>
        /// Asynchronously retrieves the total unified balance across all linked accounts.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the total unified balance as a
        /// decimal value.</returns>
        Task<decimal> GetUnifiedBalanceAsync();

        /// <summary>
        /// Asynchronously retrieves the balances for all supported blockchain networks.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary mapping each
        /// blockchain network identifier to its corresponding balance. The dictionary is empty if no balances are
        /// available.</returns>
        Task<Dictionary<string, decimal>> GetBalancesByChainAsync();

    }
}
