//Copyright 2012-2013 Rare Crowds, Inc.
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ConfigManager;
using Diagnostics;

namespace DeliveryNetworkUtilities
{
    /// <summary>Static factory for delivery network clients</summary>
    public static class DeliveryNetworkClientFactory
    {
        /// <summary>
        /// Gets or sets the delivery network client factories.
        /// </summary>
        private static IDictionary<Type, IDeliveryNetworkClientFactory> Factories { get; set; }

        /// <summary>
        /// Initializes the static DeliveryNetworkClientFactory with the
        /// provided IDeliveryNetworkClientFactory instances.
        /// </summary>
        /// <param name="factories">The factories</param>
        public static void Initialize(IEnumerable<IDeliveryNetworkClientFactory> factories)
        {
            Factories = factories.ToDictionary(f => f.ClientType, f => f);
        }

        /// <summary>
        /// Creates a new instance of the <typeparamref name="TClient"/>
        /// implementation of IDeliveryNetworkClient initialized with
        /// the provided <see cref="ConfigManager.IConfig"/>.
        /// </summary>
        /// <typeparam name="TClient">Type of client to create</typeparam>
        /// <param name="config">Configuration to use</param>
        /// <returns>The delivery network client</returns>
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "TClient is typeparam")]
        public static TClient CreateClient<TClient>(IConfig config)
            where TClient : class, IDeliveryNetworkClient
        {
            if (Factories == null)
            {
                throw new InvalidOperationException(
                    "DeliveryNetworkClientFactory.Initialize must be called before attempting to create clients");
            }

            IDeliveryNetworkClientFactory factory;
            if (!Factories.TryGetValue(typeof(TClient), out factory))
            {
                var message =
                    "No factory found for IDeliveryNetworkClient implementation: {0}"
                    .FormatInvariant(typeof(TClient).FullName);
                LogManager.Log(LogLevels.Error, message);
                throw new ArgumentException(message, "TClient");
            }

            return factory.CreateClient(config) as TClient;
        }
    }
}
