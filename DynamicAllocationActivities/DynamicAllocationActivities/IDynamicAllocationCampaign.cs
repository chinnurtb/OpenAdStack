// -----------------------------------------------------------------------
// <copyright file="IDynamicAllocationCampaign.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using ConfigManager;
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Interface definition for a class that encapsulates Dynamic Allocation Campaign data
    /// </summary>
    public interface IDynamicAllocationCampaign
    {
        /// <summary>Gets the company entity associated with the DA Campaign.</summary>
        CompanyEntity CompanyEntity { get; }

        /// <summary>Gets the campaign entity associated with the DA Campaign.</summary>
        CampaignEntity CampaignEntity { get; }

        /// <summary>Gets the user entity owner of the campaign</summary>
        UserEntity CampaignOwner { get; }

        /// <summary>Gets the custom config parameters for the DA Campaign.</summary>
        IConfig CampaignConfig { get; }

        /// <summary>Gets the allocation parameters for the DA Campaign.</summary>
        AllocationParameters AllocationParameters { get; }

        /// <summary>Gets the default delivery network</summary>
        DeliveryNetworkDesignation DeliveryNetwork { get; }

        /// <summary>Gets an object to manage BudgetAllocationHistory.</summary>
        IBudgetAllocationHistory BudgetAllocationHistory { get; }

        /// <summary>Gets an object to manager RawDeliveryData.</summary>
        IRawDeliveryData RawDeliveryData { get; }

        /// <summary>Gets the AllocationNodeMap or initializes a new one if it doesn't exist</summary>
        /// <returns>The allocation node map (empty if not yet initialized).</returns>
        /// <exception cref="DataAccessEntityNotFoundException">Thrown if associated blob entity not found.</exception>
        /// <exception cref="DataAccessTypeMismatchException">Thrown if associated entity is not BlobEntity.</exception>
        Dictionary<string, MeasureSet> RetrieveAllocationNodeMap();

        /// <summary>Creates a measure map for the company and campaign</summary>
        /// <returns>The measure map</returns>
        /// <exception cref="ArgumentNullException">
        /// Throws if static measure providers have not been initialized in the runtime context.
        /// </exception>
        MeasureMap RetrieveMeasureMap();

        /// <summary>
        /// Create a DynamicAllocationEngine instance using measure sources, etc from the entities
        /// </summary>
        /// <returns>The created DynamicAllocationEngine instance</returns>
        IDynamicAllocationEngine CreateDynamicAllocationEngine();

        /// <summary>
        /// Build a new blob containing the the allocation and associate it to the campaign as the active allocation
        /// </summary>
        /// <param name="newActiveAllocation">The new active allocation</param>
        /// <returns>The created budget allocation blob</returns>
        BlobEntity CreateAndAssociateActiveAllocationBlob(BudgetAllocation newActiveAllocation);
    }
}