//-----------------------------------------------------------------------
// <copyright file="GenericDeliveryNetworkClientFactory.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
