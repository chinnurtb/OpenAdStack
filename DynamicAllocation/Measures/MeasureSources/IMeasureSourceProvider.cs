//-----------------------------------------------------------------------
// <copyright file="IMeasureSourceProvider.cs" company="Rare Crowds Inc">
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
