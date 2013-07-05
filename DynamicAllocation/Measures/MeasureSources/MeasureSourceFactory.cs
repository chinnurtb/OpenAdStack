//-----------------------------------------------------------------------
// <copyright file="MeasureSourceFactory.cs" company="Rare Crowds Inc">
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
