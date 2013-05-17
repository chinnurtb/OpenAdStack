// -----------------------------------------------------------------------
// <copyright file="IDynamicAllocationCampaign.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
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