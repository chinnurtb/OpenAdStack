//-----------------------------------------------------------------------
// <copyright file="IDeliveryNetworkClientFactory.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using ConfigManager;

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// Describes the interface for delivery network client factories
    /// </summary>
    public interface IDeliveryNetworkClientFactory
    {
        /// <summary>
        /// Gets the type of the IDeliveryNetworkClient implementation
        /// created by the factory.
        /// </summary>
        Type ClientType { get; }

        /// <summary>
        /// Creates an IDeliveryNetworkClient instance using the provided
        /// configuration.
        /// </summary>
        /// <param name="config">Configuration to use</param>
        /// <returns>The delivery network client instance</returns>
        IDeliveryNetworkClient CreateClient(IConfig config);
    }
}
