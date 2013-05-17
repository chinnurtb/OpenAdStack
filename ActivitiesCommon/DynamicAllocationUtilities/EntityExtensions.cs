//-----------------------------------------------------------------------
// <copyright file="EntityExtensions.cs" company="Emerging Media Group">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;

namespace DynamicAllocationUtilities
{
    /// <summary>
    /// Extensions for getting/setting DynamicAllocation properties from Entities
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>Gets the EntityId of the blob containing the active allocations</summary>
        /// <param name="this">The CampaignEntity</param>
        /// <returns>The allocations blob EntityId</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static EntityId GetActiveBudgetAllocationsEntityId(this CampaignEntity @this)
        {
            var association = @this.TryGetAssociationByName(DynamicAllocationEntityProperties.AllocationSetActive);
            return (association != null) ? association.TargetEntityId : null;
        }

        /// <summary>Gets the EntityId of the blob containing the measure map</summary>
        /// <param name="this">The CampaignEntity</param>
        /// <returns>The measure map blob EntityId</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static EntityId GetMeasureMapEntityId(this CampaignEntity @this)
        {
            var association = @this.TryGetAssociationByName(DynamicAllocationEntityProperties.MeasureMap);
            return (association != null) ? association.TargetEntityId : null;
        }

        /// <summary>Sets whether the initial allocation has been done</summary>
        /// <param name="this">The campaign</param>
        /// <param name="value">If initial allocation has been done</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static void SetInitializationPhaseComplete(this CampaignEntity @this, bool value)
        {
            @this.SetPropertyValueByName(DynamicAllocationEntityProperties.InitializationPhaseComplete, value);
        }

        /// <summary>Gets whether the initial allocation has done</summary>
        /// <param name="this">The campaign</param>
        /// <returns>If initial allocation has been done</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static bool GetInitializationPhaseComplete(this CampaignEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DynamicAllocationEntityProperties.InitializationPhaseComplete);
            return value != null && value;
        }

        /// <summary>Gets the remaining budget</summary>
        /// <param name="this">The campaign</param>
        /// <returns>The remaining budget</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static decimal? GetRemainingBudget(this CampaignEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DynamicAllocationEntityProperties.RemainingBudget);
            return value != null ? (decimal?)(decimal)value.DynamicValue : null;
        }

        /// <summary>Sets the remaining budget</summary>
        /// <param name="this">The campaign</param>
        /// <param name="value">The remaining budget</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static void SetRemainingBudget(this CampaignEntity @this, decimal value)
        {
            @this.SetPropertyValueByName(DynamicAllocationEntityProperties.RemainingBudget, value);
        }

        /// <summary>Sets the AppNexus lifetime media budget cap for a CampaignEntity</summary>
        /// <param name="this">The campaign</param>
        /// <param name="lifetimeMediaBudgetCap">AppNexus lifetime media budget cap</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static void SetLifetimeMediaBudgetCap(this CampaignEntity @this, decimal lifetimeMediaBudgetCap)
        {
            @this.SetPropertyValueByName(DynamicAllocationEntityProperties.LifetimeMediaBudgetCap, (double)lifetimeMediaBudgetCap);
        }

        /// <summary>Gets the AppNexus lifetime media budget cap for a CampaignEntity</summary>
        /// <param name="this">The campaign</param>
        /// <returns>AppNexus lifetime media budget cap</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static decimal? GetLifetimeMediaBudgetCap(this CampaignEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DynamicAllocationEntityProperties.LifetimeMediaBudgetCap);
            return value != null ? (decimal?)value.DynamicValue : null;
        }

        /// <summary>Gets a measure source initialized from the MeasureMap property of the entity</summary>
        /// <param name="this">The entity</param>
        /// <returns>If MeasureMap is set, the IMeasureSource; otherwise, null</returns>
        public static IMeasureSource GetMeasureSource(this IEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(DynamicAllocationEntityProperties.MeasureMap);
            return value != null ? new EntityMeasureSource(@this) : null;
        }
    }
}
