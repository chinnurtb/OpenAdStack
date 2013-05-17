//-----------------------------------------------------------------------
// <copyright file="MeasureSourceFactory.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace DynamicAllocation
{
    /// <summary>
    /// Factory for creating measure sources from entities
    /// </summary>
    public static class MeasureSourceFactory
    {
        /// <summary>The measure source providers</summary>
        private static IMeasureSourceProvider[] providers;

        /// <summary>
        /// Initializes the factory with the specified measure source providers
        /// </summary>
        /// <param name="providers">The measure source providers</param>
        public static void Initialize(IEnumerable<IMeasureSourceProvider> providers)
        {
            MeasureSourceFactory.providers = providers.ToArray();
        }

        /// <summary>
        /// Creates measure sources for a delivery network using the provided context.
        /// </summary>
        /// <param name="deliveryNetwork">The delivery network to create measure for</param>
        /// <param name="version">The version to use</param>
        /// <param name="context">Context for creating the sources</param>
        /// <returns>Measure sources for the entities</returns>
        public static IEnumerable<IMeasureSource> CreateMeasureSources(
            DeliveryNetworkDesignation deliveryNetwork,
            int version,
            params object[] context)
        {
            return providers
                .Where(provider =>
                    provider.DeliveryNetwork == deliveryNetwork &&
                    provider.Version == version)
                .SelectMany(provider =>
                    provider.GetMeasureSources(context));
        }
    }
}
