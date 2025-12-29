using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;
using AgenticCommerce.Core.Models;

namespace AgenticCommerce.Core.Interfaces

{
    /// <summary>
    /// Defines the contract for an ARC client that provides access to ARC-related operations and services.
    /// </summary>
    /// <remarks>Implement this interface to create a client capable of interacting with ARC resources. The
    /// specific operations and usage details are defined by the implementing class.</remarks>
    public interface  IArcClient
    {
        /// <summary>
        /// Check if connected arc
        /// </summary>
        /// <returns></returns>
        Task<bool> IsConnectedAsync();
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<int> GetChainIdAsync();

        /// <summary>
        /// Asynchronously retrieves the current balance for the specified address.
        /// </summary>
        /// <param name="address">The address for which to retrieve the balance. If null, retrieves the balance for the default account.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the balance of the specified
        /// address as a decimal value.</returns>
        Task<decimal> GetBalanceAsync(string? address = null);

        /// <summary>
        /// Get current gas price in USDC
        /// </summary>
        /// <returns></returns>
        Task<decimal> GetGasPriceAysnc();

        /// <summary>
        /// Sends a specified amount of USDC to the given blockchain address asynchronously.
        /// </summary>
        /// <param name="toAddress">The destination blockchain address to which the USDC will be sent. Cannot be null or empty.</param>
        /// <param name="amountUsdc">The amount of USDC to send. Must be a positive decimal value representing the number of USDC tokens.</param>
        /// <param name="gasLimit">An optional gas limit to use for the transaction. If null, a default gas limit will be applied.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the transaction hash as a string
        /// if the transfer is successful.</returns>
        Task<string> SendUsdcAysnc(string toAddress, decimal amountUsdc, int? gasLimit = null);

        /// <summary>
        /// Get tansaction by hash
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        Task<TransactionInfo?> GetTransactionAsync(string txHash);

        /// <summary>
        /// Get transaction receipt by hash
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>

        Task<TransactionReceipt?> GetTransactionReceiptAsync(string txHash);

        /// <summary>
        /// Wait for transaction receipt by hash
        /// </summary>
        /// <param name="txHash"></param>
        /// <param name="timeoutMs"></param>
        /// <returns></returns>

        Task<TransactionReceipt> WaitForTransactionReceiptAsync(string txHash, int timeoutMs = 120);

        /// <summary>
        /// Get the agents wallet address
        /// </summary>
        /// <returns></returns>
        string GetAddress();
    }
}
