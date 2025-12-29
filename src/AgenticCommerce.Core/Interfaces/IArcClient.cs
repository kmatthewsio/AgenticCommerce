using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
