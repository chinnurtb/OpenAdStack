//-----------------------------------------------------------------------
// <copyright file="IMeasureSourceProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace DynamicAllocation
{
    /// <summary>
    /// Describes an interface for providers of measure sources
    /// </summary>
    public interface IMeasureSourceProvider
    {
        /// <summary>
        /// Gets the delivery network the measure sources are for
        /// </summary>
        DeliveryNetworkDesignation DeliveryNetwork { get; }

        /// <summary>
        /// Gets the version of the measure source provider
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Gets the measure sources from the provider.
        /// </summary>
        /// <param name="context">
        /// Context objects needed for creating the sources.
        /// </param>
        /// <returns>The measure sources</returns>
        IEnumerable<IMeasureSource> GetMeasureSources(params object[] context);
    }
}
