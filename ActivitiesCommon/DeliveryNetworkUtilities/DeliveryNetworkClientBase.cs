//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkClientBase.cs" company="Rare Crowds Inc">
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
using Diagnostics;

namespace DeliveryNetworkUtilities
{
    /// <summary>Base class for delivery network clients</summary>
    public abstract class DeliveryNetworkClientBase : IDeliveryNetworkClient
    {
        /// <summary>Backing field for Config</summary>
        private IConfig config;

        /// <summary>Gets or sets the configuration</summary>
        public IConfig Config
        {
            get
            {
                if (this.config == null)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Config not initialized. Creating default config.");
                    this.config = new CustomConfig();
                }

                return this.config;
            }

            set
            {
                this.config = value;
            }
        }
    
        /// <summary>Free unmanaged resources</summary>
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
        }
    }
}
