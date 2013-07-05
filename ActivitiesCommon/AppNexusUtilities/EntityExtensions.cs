//-----------------------------------------------------------------------
// <copyright file="EntityExtensions.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;

namespace AppNexusUtilities
{
    /// <summary>
    /// Extensions for getting/setting AppNexus specific properties from Entities
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>Sets the AppNexus member ID of a CompanyEntity</summary>
        /// <param name="this">The company</param>
        /// <param name="memberId">AppNexus member ID</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CompanyEntity")]
        public static void SetAppNexusMemberId(this CompanyEntity @this, int memberId)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.MemberId, (double)memberId);
        }

        /// <summary>Gets the AppNexus member ID of a CompanyEntity</summary>
        /// <param name="this">The company</param>
        /// <returns>AppNexus member ID if set; Otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CompanyEntity")]
        public static int? GetAppNexusMemberId(this CompanyEntity @this)
        {
            return @this.TryGetNumericPropertyValue(AppNexusEntityProperties.MemberId);
        }

        /// <summary>Sets the AppNexus advertiser ID of a CompanyEntity</summary>
        /// <param name="this">The company</param>
        /// <param name="advertiserId">AppNexus advertiser ID</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CompanyEntity")]
        public static void SetAppNexusAdvertiserId(this CompanyEntity @this, int advertiserId)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.AdvertiserId, (double)advertiserId);
        }

        /// <summary>Gets the AppNexus advertiser ID of a CompanyEntity</summary>
        /// <param name="this">The company</param>
        /// <returns>AppNexus advertiser ID if set; Otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CompanyEntity")]
        public static int? GetAppNexusAdvertiserId(this CompanyEntity @this)
        {
            return @this.TryGetNumericPropertyValue(AppNexusEntityProperties.AdvertiserId);
        }

        /// <summary>Sets the AppNexus line item ID of a CampaignEntity</summary>
        /// <param name="this">The campaign</param>
        /// <param name="lineItemId">AppNexus line item ID</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static void SetAppNexusLineItemId(this CampaignEntity @this, int lineItemId)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.LineItemId, (double)lineItemId);
        }

        /// <summary>Sets the AppNexus creative id</summary>
        /// <param name="this">The creative</param>
        /// <param name="creativeId">The creative id</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static void SetAppNexusCreativeId(this CreativeEntity @this, int creativeId)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.CreativeId, (double)creativeId);
        }

        /// <summary>Sets the AppNexus creative id</summary>
        /// <param name="this">The creative</param>
        /// <returns>The creative id</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static int? GetAppNexusCreativeId(this CreativeEntity @this)
        {
            return @this.TryGetNumericPropertyValue(AppNexusEntityProperties.CreativeId);
        }

        /// <summary>Gets the AppNexus line item ID of a CampaignEntity</summary>
        /// <param name="this">The campaign</param>
        /// <returns>AppNexus line item ID if set; Otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static int? GetAppNexusLineItemId(this CampaignEntity @this)
        {
            return @this.TryGetNumericPropertyValue(AppNexusEntityProperties.LineItemId);
        }

        /// <summary>Sets the AppNexus audit status for a creative</summary>
        /// <param name="this">The creative</param>
        /// <param name="auditStatus">The audit status</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static void SetAppNexusAuditStatus(this CreativeEntity @this, string auditStatus)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.CreativeAuditStatus, auditStatus);
        }

        /// <summary>Gets the AppNexus audit status for a creative</summary>
        /// <param name="this">The creative</param>
        /// <returns>The audit status</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CreativeEntity")]
        public static string GetAppNexusAuditStatus(this CreativeEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(AppNexusEntityProperties.CreativeAuditStatus);
            return value != null ? (string)value : null;
        }

        /// <summary>Sets the AppNexus include domains list for an entity (company or campaign)</summary>
        /// <param name="this">The entity</param>
        /// <param name="includeDomainList">The include domains list</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for IEntity")]
        public static void SetAppNexusIncludeDomainList(this IEntity @this, string[] includeDomainList)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.IncludeDomainList, string.Join("\n", includeDomainList));
        }

        /// <summary>Gets the AppNexus include domains list for an entity (company or campaign)</summary>
        /// <param name="this">The entity</param>
        /// <returns>The include domains list</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for IEntity")]
        public static string[] GetAppNexusIncludeDomainList(this IEntity @this)
        {
            var value = @this.TryGetPropertyValueByName(AppNexusEntityProperties.IncludeDomainList);
            return value != null ?
                ((string)value)
                    .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray() :
                null;
        }

        /// <summary>Sets the AppNexus include domains list for an entity (company or campaign)</summary>
        /// <param name="this">The campaign</param>
        /// <param name="includeDomainListId">The include domains list</param>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static void SetAppNexusIncludeDomainListId(this CampaignEntity @this, int includeDomainListId)
        {
            @this.SetPropertyValueByName(AppNexusEntityProperties.IncludeDomainListId, (double)includeDomainListId);
        }

        /// <summary>Gets the AppNexus include domains list for an entity (company or campaign)</summary>
        /// <param name="this">The campaign</param>
        /// <returns>The include domains list</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntity")]
        public static int? GetAppNexusIncludeDomainListId(this CampaignEntity @this)
        {
            return @this.TryGetNumericPropertyValue(AppNexusEntityProperties.IncludeDomainListId);
        }
    }
}
