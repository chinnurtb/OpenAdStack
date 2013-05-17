//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkClientBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
