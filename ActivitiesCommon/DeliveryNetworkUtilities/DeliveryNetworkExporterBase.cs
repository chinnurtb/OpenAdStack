//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkExporterBase.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Linq;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;

namespace DeliveryNetworkUtilities
{
    /// <summary>Base class for delivery network exporters encapsulating lazy-initialized properties</summary>
    /// <typeparam name="TDeliveryNetworkClient">Type of the delivery network client</typeparam>
    public abstract class DeliveryNetworkExporterBase<TDeliveryNetworkClient> : IDisposable
        where TDeliveryNetworkClient : class, IDeliveryNetworkClient
    {
        /// <summary>Backing field for Config</summary>
        private IConfig config;

        /// <summary>Backing field for Client</summary>
        private TDeliveryNetworkClient client;

        /// <summary>Initializes a new instance of the DeliveryNetworkExporterBase class.</summary>
        /// <param name="deliveryNetwork">The delivery network being exported to</param>
        /// <param name="version">Version of the exporter</param>
        protected DeliveryNetworkExporterBase(
            DeliveryNetworkDesignation deliveryNetwork,
            int version)
        {
            this.DeliveryNetwork = deliveryNetwork;
            this.Version = version;
        }

        /// <summary>Gets the delivery network designation</summary>
        protected DeliveryNetworkDesignation DeliveryNetwork { get; private set; }

        /// <summary>Gets the delivery network exporter version</summary>
        protected int Version { get; private set; }

        /// <summary>Gets the exporter configuration</summary>
        protected IConfig Config
        {
            get
            {
                return this.config = this.config ?? this.BuildConfig();
            }
        }

        /// <summary>Gets the delivery network client</summary>
        protected TDeliveryNetworkClient Client
        {
            get
            {
                return this.client =
                    this.client ??
                    DeliveryNetworkClientFactory.CreateClient<TDeliveryNetworkClient>(this.Config);
            }
        }

        /// <summary>Gets the entities used in the exporter's config</summary>
        protected abstract IEnumerable<IEntity> ConfigEntities { get;  }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        /// <param name="disposing">
        /// Whether to clean up managed resources as well as unmanaged
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.client != null)
                {
                    this.client.Dispose();
                    this.client = null;
                }
            }
        }

        /// <summary>Builds the IConfig used by the exporter</summary>
        /// <returns>The config</returns>
        protected virtual IConfig BuildConfig()
        {
            return EntityActivityUtilities.BuildCustomConfigFromEntities(
                true, this.ConfigEntities.ToArray());
        }
    }
}
