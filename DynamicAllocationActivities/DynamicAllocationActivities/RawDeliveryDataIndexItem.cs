// -----------------------------------------------------------------------
// <copyright file="RawDeliveryDataIndexItem.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Class to contain raw delivery data entity references for a single network
    /// </summary>
    public class RawDeliveryDataIndexItem
    {
        /// <summary>Initializes a new instance of the <see cref="RawDeliveryDataIndexItem"/> class.</summary>
        /// <param name="deliveryNetwork">The delivery network.</param>
        /// <param name="entityIds">An collection of raw delivery data entity id's.</param>
        public RawDeliveryDataIndexItem(DeliveryNetworkDesignation deliveryNetwork, IEnumerable<EntityId> entityIds)
        {
            this.DeliveryNetwork = deliveryNetwork;
            this.RawDeliveryDataEntityIds = entityIds;
        }

        /// <summary>Gets the array of raw delivery data entity id's.</summary>
        public IEnumerable<EntityId> RawDeliveryDataEntityIds { get; private set; }

        /// <summary>Gets the delivery network.</summary>
        public DeliveryNetworkDesignation DeliveryNetwork { get; private set; }
    }
}