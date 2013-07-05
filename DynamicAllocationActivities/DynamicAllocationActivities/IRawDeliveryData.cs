// -----------------------------------------------------------------------
// <copyright file="IRawDeliveryData.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// An interface definition for a class that manages retrieval of raw delivery data from
    /// a campaign entity.
    /// </summary>
    public interface IRawDeliveryData
    {
        /// <summary>Retrieve zero or more raw delivery data indexes for a campaign from storage.</summary>
        /// <returns>The indexes items. No partial success. Collection may be empty if no indexes were found but there were no failures.</returns>
        IEnumerable<RawDeliveryDataIndexItem> RetrieveRawDeliveryDataIndexItems();

        /// <summary>Get a single raw delivery data item.</summary>
        /// <param name="rawDeliveryDataEntityId">The raw delivery data entity id.</param>
        /// <returns>The raw delivery data item, or null if not found.</returns>
        RawDeliveryDataItem RetrieveRawDeliveryDataItem(EntityId rawDeliveryDataEntityId);
    }
}