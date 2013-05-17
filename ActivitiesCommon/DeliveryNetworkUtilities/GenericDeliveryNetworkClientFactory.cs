//-----------------------------------------------------------------------
// <copyright file="GenericDeliveryNetworkClientFactory.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using ConfigManager;

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// Generic implementation of IDeliveryNetworkClientFactory
    /// </summary>
    /// <typeparam name="TClientInterface">
    /// Interface of the delivery network client to create
    /// </typeparam>
    /// <typeparam name="TClientImplementation">
    /// Implementation of the delivery network client to create
    /// </typeparam>
    public class GenericDeliveryNetworkClientFactory<TClientInterface, TClientImplementation>
        : IDeliveryNetworkClientFactory
        where TClientInterface : IDeliveryNetworkClient
        where TClientImplementation : class, TClientInterface, new()
    {
        /// <summary>
        /// Gets the type of the IDeliveryNetworkClient implementation
        /// created by the factory.
        /// </summary>
        public Type ClientType
        {
            get { return typeof(TClientInterface); }
        }

        /// <summary>
        /// Creates an instance of the delivery network client using
        /// the provided configuration.
        /// </summary>
        /// <param name="config">Configuration to use</param>
        /// <returns>The delivery network client instance</returns>
        public IDeliveryNetworkClient CreateClient(IConfig config)
        {
            return new TClientImplementation { Config = config };
        }
    }
}
