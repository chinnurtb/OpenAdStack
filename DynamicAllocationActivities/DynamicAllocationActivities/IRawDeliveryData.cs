// -----------------------------------------------------------------------
// <copyright file="IRawDeliveryData.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
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