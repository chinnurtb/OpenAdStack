//-----------------------------------------------------------------------
// <copyright file="IDeliveryNetworkClient.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using ConfigManager;

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// Describes the interface for delivery network clients created by
    /// implementers of IDeliveryNetworkClientFactory.
    /// </summary>
    /// <remarks>
    /// Implemenations should favor lazy initialization wherever possible,
    /// especially for members dependent upon the Config.
    /// </remarks>
    public interface IDeliveryNetworkClient : IDisposable
    {
        /// <summary>Gets or sets the client's configuration</summary>
        IConfig Config { get; set; }
    }
}
