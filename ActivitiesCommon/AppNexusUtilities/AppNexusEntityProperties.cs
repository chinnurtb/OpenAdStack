//-----------------------------------------------------------------------
// <copyright file="AppNexusEntityProperties.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace AppNexusUtilities
{
    /// <summary>
    /// Names of the entity properties in which AppNexus related values are kept
    /// </summary>
    public static class AppNexusEntityProperties
    {
        /// <summary>AppNexus member_id</summary>
        public const string MemberId = "APNXMemberId";

        /// <summary>AppNexus advertiser_id</summary>
        public const string AdvertiserId = "APNXAdvertiserId";

        /// <summary>AppNexus campaign_id</summary>
        public const string CampaignId = "APNXCampaignId";

        /// <summary>AppNexus creative_id</summary>
        public const string CreativeId = "APNXCreativeId";

        /// <summary>AppNexus audit_status</summary>
        public const string CreativeAuditStatus = "APNXAuditStatus";

        /// <summary>AppNexus line_item_id</summary>
        public const string LineItemId = "APNXLineItemId";

        /// <summary>AppNexus domain list id</summary>
        public const string IncludeDomainListId = "APNXIncludeDomainListId";
        
        /// <summary>Association name for the raw delivery data from AppNexus</summary>
        public const string AppNexusRawDeliveryDataIndex = "APNXRawDeliveryDataIndex";

        /// <summary>AppNexus include domains list</summary>
        public const string IncludeDomainList = "APNXIncludeDomainList";
    }
}
