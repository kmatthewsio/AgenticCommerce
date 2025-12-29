using AgenticCommerce.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgenticCommerce.Core.Interfaces
{
    public interface IAgentService
    {
        /// <summary>
        /// Creates a new agent with the specified name and optional budget in USD Coin (USDC).
        /// </summary>
        /// <param name="name">The name to assign to the new agent. Cannot be null or empty.</param>
        /// <param name="budgetUsdc">The maximum budget for the agent, denominated in USDC. Specify null to create an agent without a budget
        /// limit.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the unique identifier of the
        /// created agent.</returns>
        Task<string> CreateAgentAsync(string name, decimal? budgetUsdc = null);

        /// <summary>
        /// Runs the specified agent asynchronously with the provided message and returns the result of the agent's
        /// execution.  
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent to run. Cannot be null or empty.</param>
        /// <param name="message">The input message to send to the agent. Cannot be null.</param>
        /// <param name="stream">true to enable streaming of the agent's response; otherwise, false.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an AgentRunResult object with
        /// the outcome of the agent's execution.</returns>
        Task<AgentRunResult> RunAgentAsync(string agentId, string message, bool stream = false);

        /// <summary>
        /// Initiates an asynchronous purchase transaction on behalf of the specified agent, transferring the given USDC
        /// amount to the recipient address.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent performing the purchase. Cannot be null or empty.</param>
        /// <param name="recipientAddress">The address of the recipient who will receive the USDC funds. Must be a valid address format and cannot be
        /// null or empty.</param>
        /// <param name="amountUsdc">The amount of USDC to transfer. Must be a positive decimal value.</param>
        /// <param name="description">A description of the purchase transaction. Used for record-keeping or audit purposes. Cannot be null.</param>
        /// <param name="generateConfirmation">true to generate and return a purchase confirmation; otherwise, false.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a PurchaseResult object with
        /// details of the completed transaction.</returns>
        Task<PurchaseResult> ExecutePurchaseAsync(
            string agentId,
            string recipientAddress,
            decimal amountUsdc,
            string description,
            bool generateConfirmation = true);

         /// <summary>
         /// Asynchronously retrieves the current status of the specified agent.
         /// </summary>
         /// <param name="agentId">The unique identifier of the agent whose status is to be retrieved. Cannot be null or empty.</param>
         /// <returns>A task that represents the asynchronous operation. The task result contains the current status of the
         /// specified agent.</returns>
        Task<AgentStatus> GetAgentStatusAsync(string agentId);

        /// <summary>
        /// Asynchronously deletes the agent with the specified identifier.
        /// </summary>
        /// <param name="agentId">The unique identifier of the agent to delete. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the agent
        /// was successfully deleted; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteAgentAsync(string agentId);

        /// <summary>
        /// Asynchronously retrieves a collection of all registered agents.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable collection of <see
        /// cref="AgentInfo"/> objects representing the registered agents. The collection is empty if no agents are
        /// registered.</returns>
        Task<IEnumerable<AgentInfo>> ListAgentsAsync();
    }
}
