//Copyright 2012-2013 Rare Crowds, Inc.
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
