// -----------------------------------------------------------------------
// <copyright file="RawDeliveryDataIndexItem.cs" company="Rare Crowds Inc">
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