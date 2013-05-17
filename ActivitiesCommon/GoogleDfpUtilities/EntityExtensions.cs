//-----------------------------------------------------------------------
// <copyright file="EntityExtensions.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;
using GoogleDfpUtilities;
using Newtonsoft.Json;

namespace GoogleDfpUtilities
{
    /// <summary>Entity extensions for Google DFP related properties</summary>
    public static class EntityExtensions
    {
        /// <summary>Sets the CompanyEntity's Google DFP order id</summary>
        /// <param name="this">The campaign</param>
        /// <param name="advertiserId">The order id</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CompanyEntity")]
        public static void SetDfpAdvertiserId(this CompanyEntity @this, long advertiserId)
        {
            @this.SetPropertyValueByName(GoogleDfpEntityProperties.AdvertiserId, (double)advertiserId);
        }

        /// <summary>Sets the CompanyEntity's Google DFP order id</summary>
        /// <param name="this">The campaign</param>
        /// <returns>The order id</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CompanyEntity")]
        public static long? GetDfpAdvertiserId(this CompanyEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(GoogleDfpEntityProperties.AdvertiserId);
            return value != null ? (long?)(double)value : null;
        }

        /// <summary>Sets the CampaignEntity's Google DFP order id</summary>
        /// <param name="this">The campaign</param>
        /// <param name="orderId">The order id</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static void SetDfpOrderId(this CampaignEntity @this, long orderId)
        {
            @this.SetPropertyValueByName(GoogleDfpEntityProperties.OrderId, (double)orderId);
        }

        /// <summary>Sets the CampaignEntity's Google DFP order id</summary>
        /// <param name="this">The campaign</param>
        /// <returns>The order id</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static long? GetDfpOrderId(this CampaignEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(GoogleDfpEntityProperties.OrderId);
            return value != null ? (long?)(double)value : null;
        }

        /// <summary>Sets the CreativeEntity's Google DFP creative id</summary>
        /// <param name="this">The creative</param>
        /// <param name="creativeId">The creative id</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static void SetDfpCreativeId(this CreativeEntity @this, long creativeId)
        {
            @this.SetPropertyValueByName(GoogleDfpEntityProperties.CreativeId, (double)creativeId);
        }

        /// <summary>Sets the CreativeEntity's Google DFP creative id</summary>
        /// <param name="this">The creative</param>
        /// <returns>The creative id</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static long? GetDfpCreativeId(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(GoogleDfpEntityProperties.CreativeId);
            return value != null ? (long?)(double)value : null;
        }

        /// <summary>Gets the active budget allocation set</summary>
        /// <param name="this">The campaign</param>
        /// <returns>The active budget allocations</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static BudgetAllocation GetActiveAllocations(this CampaignEntity @this)
        {
            var activeAllocationsJson = @this.TryGetPropertyValueByName(DynamicAllocationEntityProperties.AllocationSetActive);
            if (activeAllocationsJson != null)
            {
                return JsonConvert.DeserializeObject<BudgetAllocation>(activeAllocationsJson);
            }

            return null;
        }
    }
}
