//-----------------------------------------------------------------------
// <copyright file="AppNexusJson.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AppNexusClient
{
    /// <summary>Container for AppNexus JSON format strings</summary>
    public static class AppNexusJson
    {
        /// <summary>Format for AppNexus time stamps</summary>
        public const string TimeStampFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>JSON format for the auth request</summary>
        public const string AuthRequestFormat = @"
{{
  ""auth"":
  {{
    ""username"" : ""{0}"",
    ""password"" : ""{1}""
  }}
}}";
        
        /// <summary>
        /// POST content JSON for creating a segment.
        /// Format arguments:
        /// 0 - Member id
        /// 1 - Code
        /// 2 - Short name
        /// </summary>
        public const string CreateMemberSegmentFormat = @"
{{
    ""segment"":
    {{
        ""member_id"":{0},
        ""code"":""{1}"",
        ""short_name"":""{2}""
    }}
}}";

        /// <summary>
        /// POST content JSON for creating an advertiser.
        /// Format arguments:
        /// 0 - CompanyEntity.ExternalName
        /// 1 - CompanyEntity.ExternalEntityId
        /// </summary>
        public const string CreateAdvertiserFormat = @"
{{
    ""advertiser"":
    {{
        ""name"":""RC_{0}"",
        ""state"":""active"",
        ""code"":""{1}"",
        ""timezone"":""UTC""
    }}
}}";

        /// <summary>
        /// Default PUT content JSON for updating a line item.
        /// Format arguments:
        /// 0 - campaign.ExternalName
        /// 1 - campaign.ExternalEntityId
        /// 2 - ["active"|"inactive"]
        /// 3 - campaign.GetAppNexusStartDate()
        /// 4 - campaign.GetAppNexusEndDate()
        /// 5 - Total budget
        /// 6 - Profile id
        /// </summary>
        /// <remarks>
        /// To modify values beyond the ones above, deserialize using JavaScriptSerializer,
        /// replace the values in the resulting IDictionary&lt;string, object%gt;, and
        /// reserialize it back to JSON.
        /// </remarks>
        public const string CreateLineItemFormat = @"
{{
    ""line-item"":
    {{
        ""name"":""RC_{0}"",
        ""code"":""{1}"",
        ""state"":""{2}"",
        ""timezone"":""UTC"",
	    ""currency"":""USD"",
        ""inventory_type"":""real_time"",
        ""start_date"":""{3}"",
        ""end_date"":""{4}"",
        ""track_revenue"":""track"",
        ""revenue_type"":""cost_plus_margin"",
        ""revenue_value"":0,
        ""cost_plus_value"":""0.0"",
        ""cost_plus_type"":""margin"",
        ""lifetime_budget"":""{5}"",
        ""lifetime_budget_imps"":null,
        ""daily_budget"":null,
        ""daily_budget_imps"":null,
        ""enable_pacing"":true,
        ""insertion_orders"":[],
        ""broker_fees"":[],
        ""goal_type"":""none"",
        ""goal_value"":null,
        ""profile_id"":{6},
        ""pixels"":null
    }}
}}";

        /// <summary>
        /// Default PUT content JSON for updating a line item.
        /// Format arguments:
        /// 0 - campaign.ExternalName
        /// 1 - campaign.ExternalEntityId
        /// 2 - ["active"|"inactive"]
        /// 3 - campaign.GetAppNexusStartDate()
        /// 4 - campaign.GetAppNexusEndDate()
        /// 5 - Total budget
        /// </summary>
        /// <remarks>
        /// To modify values beyond the ones above, deserialize using JavaScriptSerializer,
        /// replace the values in the resulting IDictionary&lt;string, object%gt;, and
        /// reserialize it back to JSON.
        /// </remarks>
        public const string UpdateLineItemFormat = @"
{{
    ""line-item"":
    {{
        ""name"":""RC_{0}"",
        ""code"":""{1}"",
        ""state"":""{2}"",
        ""timezone"":""UTC"",
	    ""currency"":""USD"",
        ""inventory_type"":""real_time"",
        ""start_date"":""{3}"",
        ""end_date"":""{4}"",
        ""track_revenue"":""track"",
        ""revenue_type"":""cost_plus_margin"",
        ""revenue_value"":0,
        ""cost_plus_value"":""0.0"",
        ""cost_plus_type"":""margin"",
        ""lifetime_budget"":""{5}"",
        ""lifetime_budget_imps"":null,
        ""daily_budget"":null,
        ""daily_budget_imps"":null,
        ""enable_pacing"":true,
        ""insertion_orders"":[],
        ""broker_fees"":[],
        ""goal_type"":""none"",
        ""goal_value"":null,
        ""pixels"":null
    }}
}}";

        /// <summary>
        /// POST content JSON for creating a campaign (with lifetime budget/impression caps)
        /// Format args:
        /// 0: Name (campaign.ExternalName + measureSet?)
        /// 1: Code (allocation.AllocationId)
        /// 2: State [active|inactive]
        /// 3: Start Date
        /// 4: End Date
        /// 5: Lifetime Budget Cap
        /// 6: Lifetime Impression Cap
        /// 7: Base Bid (MaxBid)
        /// 8: Profile id
        /// 9: Line item id
        /// 10: Advertiser id
        /// 11: Creatives
        /// </summary>
        /// <remarks>TODO: Review/reduce?</remarks>
        public const string CreateCampaignFormat = @"
{{
    ""campaign"":
    {{
        ""name"":""RC_{0}"",
        ""code"":""{1}"",
        ""timezone"":""UTC"",
        ""profile_id"":""{8}"",
        ""line_item_id"":""{9}"",
        ""advertiser_id"":""{10}"",
        ""state"":""{2}"",
        ""start_date"":""{3}"",
        ""end_date"":""{4}"",
        ""enable_pacing"":false,
        ""daily_budget"":null,
        ""daily_budget_imps"":null,
        ""lifetime_budget"":""{5}"",
        ""lifetime_budget_imps"":""{6}"",
        ""base_bid"":""{7}"",
        ""creatives"":[{11}],
        ""set_cadence_modifier_by_default"":""0"",
        ""click_url"":null,
        ""priority"":5,
        ""pay_by_cpm"":""pay-by-cpm"",
        ""margin_value"":""0"",
        ""bid_margin"":0,
        ""max_bid"":null,
        ""min_bid"":null,
        ""predicted_type"":""cpa-bid"",
        ""cpc_goal"":null,
        ""cpm_bid_type"":""base"",
        ""cpm_bid_type_cpm"":""clearing"",
        ""defer_to_line_item_revenue_value"":"""",
        ""inventory_type"":""real_time"",
        ""payment_type"":""pay-by-cpm"",
        ""roadblock_creatives"":false,
        ""require_cookie_for_tracking"":true,
        ""labels"":[],
        ""learn_budget"":null,
        ""learn_budget_imps"":null,
        ""learn_budget_daily_cap"":null,
        ""learn_budget_daily_imps"":null,
        ""cadence_modifier_enabled"":false,
        ""pixels"":null,
        ""defer_to_li_prediction"":false,
        ""cpc_payout"":null,
        ""broker_fees"":[]
    }}
}}";

        /// <summary>
        /// PUT content JSON for updating a campaign
        /// Format args:
        /// 0: Name (campaign.ExternalName + measureSet?)
        /// 1: Code (allocation.AllocationId)
        /// 2: State [active|inactive]
        /// 3: Start Date
        /// 4: End Date
        /// 5: Lifetime Budget Cap
        /// 6: Lifetime Impression Cap
        /// 7: Base Bid (MaxBid)
        /// 8: Creatives
        /// </summary>
        public const string UpdateCampaignFormat = @"
{{
    ""campaign"":
    {{
        ""name"":""RC_{0}"",
        ""code"":""{1}"",
        ""state"":""{2}"",
        ""start_date"":""{3}"",
        ""end_date"":""{4}"",
        ""lifetime_budget"":""{5}"",
        ""lifetime_budget_imps"":""{6}"",
        ""base_bid"":""{7}"",
        ""pixels"":null,
        ""creatives"":[{8}]
    }}
}}";

        /// <summary>
        /// PUT content JSON for updating a campaign's state
        /// Format args:
        /// 0: State [active|inactive]
        /// </summary>
        public const string UpdateCampaignStateFormat = @"
{{
    ""campaign"": {{ ""state"":""{0}"" }}
}}";

        /// <summary>Creative entry format</summary>
        public const string CampaignCreativeEntryFormat = @"{{""id"":{0}}}";

        /// <summary>
        /// Format args:
        /// 0: Code
        /// 1: Description
        /// 2: Advertiser ID
        /// 3: Segments
        /// 4: DMA Targets
        /// 5: Region Targets
        /// 6: Allow Unknown Position
        /// 7: Positions (see ProfilePositionFormat)
        /// 8: Use inventory attribute targets
        /// 9: Inventory attribute targets
        /// 10: Age Targets
        /// 11: Allow Unknown Content Category
        /// 12: Content Categories
        /// 13: Additional properties
        /// </summary>
        public const string CampaignProfileFormat = @"
{{
    ""profile"":
    {{
        ""code"":""{0}"",
        ""description"":""{1}"",
        ""advertiser_id"":""{2}"",
        ""position_targets"":
        {{
            ""allow_unknown"":{6}{7}
        }},
        ""segment_boolean_operator"":""and"",
        ""segment_targets"":[{3}],
        {4}{5}
        ""use_inventory_attribute_targets"":{8},
        ""inventory_attribute_targets"":[{9}],{10}
        ""content_category_targets"":
        {{
            ""allow_unknown"":{11},
            ""content_categories"":[{12}]
        }},
        {13}
        ""domain_list_action"":""{14}"",
        ""domain_list_targets"":[{15}],
        ""country_action"":""include"",
        ""country_targets"":[{{""country"":""US"",""name"":""United States""}}]
    }}
}}";

        /// <summary>
        /// Format args:
        /// 0: Code
        /// 1: Description
        /// 2: Advertiser ID
        /// 3: Segments
        /// 4: DMA Targets
        /// 5: Region Targets
        /// 6: Allow Unknown Position
        /// 7: Positions (see ProfilePositionFormat)
        /// 8: Use inventory attribute targets
        /// 9: Inventory attribute targets
        /// 10: Age Targets
        /// 11: Allow Unknown Content Category
        /// 12: Content Categories
        /// 13: Additional properties
        /// </summary>
        /// <remarks>
        /// Includes hard-coded Rare Crowds member target
        /// </remarks>
        public const string ExclusivelyRareCrowdsInventoryCampaignProfileFormat = @"
{{
    ""profile"":
    {{
        ""code"":""{0}"",
        ""description"":""{1}"",
        ""advertiser_id"":""{2}"",
        ""position_targets"":
        {{
            ""allow_unknown"":{6}{7}
        }},
        ""segment_boolean_operator"":""and"",
        ""segment_targets"":[{3}],
        {4}{5}
        ""use_inventory_attribute_targets"":{8},
        ""inventory_attribute_targets"":[{9}],{10}
        ""content_category_targets"":
        {{
            ""allow_unknown"":{11},
            ""content_categories"":[{12}]
        }},
        ""member_targets"": [{{
            ""id"": ""1320"",
            ""action"": ""include"",
            ""third_party_auditor_id"": null,
            ""audit_requirement"": ""none"",
            ""billing_name"": ""Rare Crowds Inc.""
        }}],
        {13}
        ""domain_list_action"":""{14}"",
        ""domain_list_targets"":[{15}],
        ""country_action"":""include"",
        ""country_targets"":[{{""country"":""US"",""name"":""United States""}}]
    }}
}}";

        /// <summary>
        /// Format args:
        /// 0: Description
        /// 1: Advertiser ID
        /// 2: Additional properties (ex: freq caps, site targets)
        /// </summary>
        public const string LineItemProfileFormat = @"
{{
    ""profile"":
    {{
        ""description"":""{0}"",
        ""advertiser_id"":""{1}"",
        {2}
        ""country_action"":""include"",
        ""country_targets"":[{{""country"":""US"",""name"":""United States""}}]
    }}
}}";

        /// <summary>
        /// Format args:
        /// 0: Position ("above" or "below")
        /// </summary>
        public const string ProfilePositionFormat = @",
            ""positions"":[{{""position"":""{0}""}}]
";

        /// <summary>
        /// Format args:
        /// 0: Attribute ID
        /// </summary>
        public const string ProfileInventoryAttributeTargetsEntryFormat = @"{{""id"":{0}}}";

        /// <summary>
        /// Format args:
        /// 0: Content Category ID
        /// 1: Action ("include" or "exclude")
        /// </summary>
        public const string ContentCategoryTargetsEntryFormat = @"{{""id"":{0}, ""action"":""{1}""}}";

        /// <summary>
        /// Format args:
        /// 0: Code
        /// 1: Description
        /// 2: Segments
        /// 3: DMA Targets
        /// 4: Advertiser ID
        /// </summary>
        public const string FullProfileFormat = @"
{{
    ""profile"":{{
        ""code"":""{0}"",
        ""description"":""{1}"",
        ""max_day_imps"":null,
        ""min_minutes_per_imp"":null,
        ""max_lifetime_imps"":null,
        ""min_session_imps"":null,
        ""max_session_imps"":null,
        ""session_freq_type"":""platform"",
        ""require_cookie_for_freq_cap"":true,
        ""is_template"":false,
        ""daypart_targets"":null,
        ""daypart_timezone"":null,
        ""age_targets"":{{
                ""allow_unknown"":false,
                ""ages"":[{{""low"":25,""high"":35}}]
        }},
        ""gender_targets"":null,
        ""language_action"":""exclude"",
        ""language_targets"":[],
        ""country_targets"":[],
        ""country_action"":""exclude"",
        ""region_targets"":[],
        ""region_action"":""exclude"",
        ""dma_targets"":[{3}],
        ""dma_action"":""include"",
        ""city_targets"":[],
        ""city_action"":""exclude"",
        ""zip_targets"":null,
        ""size_targets"":[],
        ""position_targets"":{{
            ""allow_unknown"":false,
            ""positions"":[{{""position"":""above""}}]
        }},
        ""querystring_action"":""exclude"",
        ""querystring_boolen_operator"":""and"",
        ""querystring_targets"":[],
        ""placement_targets"":[],
        ""site_targets"":[],
        ""segment_boolean_operator"":""and"",
        ""segment_group_targets"":null,
        ""segment_targets"":[{2}],
        ""exelate_targets"":null,
        ""operating_system_action"":""exclude"",
        ""operating_system_targets"":[],
        ""browser_action"":""exclude"",
        ""browser_targets"":[],
        ""carrier_action"":""exclude"",
        ""carrier_targets"":[],
        ""handset_make_action"":""exclude"",
        ""handset_model_action"":""exclude"",
        ""handset_model_targets"":[],
        ""handset_make_targets"":[],
        ""location_target_latitude"":null,
        ""location_target_longitude"":null,
        ""location_target_radius"":null,
        ""user_group_targets"":null,
        ""venue_targets"":[],
        ""venue_action"":""exclude"",
        ""inventory_source_targets"":[],
        ""inventory_group_targets"":[],
        ""inv_class_targets"":[],
        ""publisher_targets"":[],
        ""media_buy_targets"":[],
        ""member_targets"":[],
        ""content_category_targets"":{{
            ""content_categories"":[]
        }},
        ""inventory_action"":""exclude"",
        ""domain_targets"":[],
        ""domain_list_targets"":[],
        ""domain_action"":""exclude"",
        ""domain_list_action"":""exclude"",
        ""platform_content_category_targets"":[],
        ""platform_placement_targets"":[],
        ""platform_publisher_targets"":[],
        ""supply_type_action"":""include"",
        ""supply_type_targets"":[""web"",""mobile_web""],
        ""trust"":""appnexus"",
        ""allow_unaudited"":false,
        ""use_inventory_attribute_targets"":true,
        ""inventory_attribute_targets"":[],
        ""intended_audience_targets"":[""general"",""children"",""young_adult""],
        ""advertiser_id"":""{4}""
    }}
}}";

        /// <summary>
        /// Age target format
        /// Format args:
        /// 0: Whether to allow unknown
        /// 1: Minimum age
        /// 2: Maximum age
        /// </summary>
        public const string ProfileAgeTargetFormat = @"""age_targets"":{{""allow_unknown"":""{0}"", ""ages"":[{{""low"":""{1}"", ""high"":""{2}""}}]}},";

        /// <summary>
        /// Gender target format
        /// Format args:
        /// 0: Gender (male|female)
        /// </summary>
        public const string ProfileGenderTargetFormat = @"""gender"":""{0}"",";

        /// <summary>
        /// Targeting segment entry format
        /// Format args:
        /// 0: Segment ID (APNXId in the MeasureMap)
        /// </summary>
        public const string ProfileSegmentEntryFormat = @"{{""id"":""{0}""}}";

        /// <summary>
        /// DMA targeting entry format
        /// Format args:
        /// 0: Metro code (APNXId in the MeasureMap)
        /// 1: Name (leafName in MeasureMap)
        /// </summary>
        public const string ProfileDmaTargetsEntryFormat = @"{{""dma"":""{0}"", ""name"":""{1}""}}";

        /// <summary>
        /// State targeting entry format (ISO 3166-2)
        /// Format args:
        /// 0: State code (APNXId in the MeasureMap)
        /// 1: Name (leafName in MeasureMap)
        /// </summary>
        public const string ProfileRegionTargetsEntryFormat = @"{{""region"":""{0}""}}";

        /// <summary>
        /// Creative format
        /// Format args:
        /// 0: Name
        /// 1: Code (Creative EntityId)
        /// 2: State [active|inactive]
        /// 3: Content (Tag JS/HTML or base64 binary)
        /// 4: Width
        /// 5: Height
        /// 6: Advertiser ID
        /// 7: Template ID
        /// </summary>
        public const string CreativeFormat = @"
{{
    ""creative"":
    {{
        ""name"":""RC_{0}"",
        ""code"":""{1}"",
        ""advertiser_id"":""{6}"",
        ""content"":""{3}"",
        ""width"":""{4}"",
        ""height"":""{5}"",
        ""template"":{{""id"":""{7}""}}
    }}
}}";

        /// <summary>
        /// Domain list format
        /// Format args:
        /// 0: Name
        /// 1: Description
        /// 2: List of domains
        /// </summary>
        public const string DomainListFormat = @"
{{
    ""domain-list"":
    {{
        ""name"":""RC_{0}"",
        ""description"":""{1}"",
        ""domains"":[{2}]
    }}
}}";

        /// <summary>POST content JSON for the report request</summary>
        /// <remarks>
        /// Requests a report containing the campaign_id, campaign_code (MeasureSets Blob EntityId), impressions and eCPM
        /// for the entire lifetime with one row per campaign.
        /// </remarks>
        public const string DeliveryReportRequestFormat = @"
{{
    ""report"":
    {{
        ""report_type"":""advertiser_analytics"",
        ""report_interval"":""last_48_hours"",
        ""columns"":[""campaign_id"", ""hour"", ""campaign_code"", ""imps"", ""ecpm"", ""spend"", ""clicks""],
        ""row_per"":[""campaign_id"", ""hour""],
        ""filters"":[{{""line_item_id"":{0}}}],
        ""timezone"":""UTC""
    }}
}}";

        /// <summary>JSON values for AppNexus Age Ranges</summary>
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = ".Net does not yet have a ReadonlyDictionary type")]
        public static readonly IDictionary<AppNexusAgeRange, string> AgeRangesJson = new Dictionary<AppNexusAgeRange, string>
        {
            { AppNexusAgeRange.None, string.Empty },
            { AppNexusAgeRange.AllowUnknown, @"""age_targets"":{""allow_unknown"":true, ""ages"":[]}," },
            { AppNexusAgeRange.Age18To24, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""18"", ""high"":""24""}]}," },
            { AppNexusAgeRange.Age25To34, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""25"", ""high"":""34""}]}," },
            { AppNexusAgeRange.Age35To44, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""35"", ""high"":""44""}]}," },
            { AppNexusAgeRange.Age45To49, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""45"", ""high"":""49""}]}," },
            { AppNexusAgeRange.Age50To54, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""50"", ""high"":""54""}]}," },
            { AppNexusAgeRange.Age55To64, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""55"", ""high"":""64""}]}," },
            { AppNexusAgeRange.Age65To85, @"""age_targets"":{""allow_unknown"":false, ""ages"":[{""low"":""65"", ""high"":""85""}]}," }
        };
    }
}
