//-----------------------------------------------------------------------
// <copyright file="AppNexusApiClient.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ConfigManager;
using Diagnostics;
using Utilities;
using Utilities.Net;
using Utilities.Storage;

namespace AppNexusClient
{
    /// <summary>AppNexus API Client</summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly",
        Justification = "False positive. IDisposable is inherited view IAppNexusApiClient")]
    internal class AppNexusApiClient : AppNexusApiClientBase, IAppNexusApiClient
    {
        /// <summary>Page size for member segment TryGetCollection calls</summary>
        private const int MemberSegmentGetCollectionPageSize = 1000;

        /// <summary>Mappings from AppNexusFrequencyType to profile property values</summary>
        private static readonly IDictionary<AppNexusFrequencyType, string> ProfileFrequencyMappings =
            new Dictionary<AppNexusFrequencyType, string>
            {
                { AppNexusFrequencyType.Lifetime, "max_lifetime_imps" },
                { AppNexusFrequencyType.Session, "max_session_imps" },
                { AppNexusFrequencyType.Day, "max_day_imps" },
                { AppNexusFrequencyType.Minutes, "min_minutes_per_imp" },
            };

        /// <summary>Filters for member segments</summary>
        /// <remarks>Only include segments that are active</remarks>
        private static readonly IDictionary<string, string> MemberSegmentFilters =
            new Dictionary<string, string>
            {
                { "state", "active" }
            };

        /// <summary>Backing field for AppNexusMembers</summary>
        private IPersistentDictionary<Tuple<string, DateTime>> appNexusMembers;

        /// <summary>Backing field for Id. DO NOT USE DIRECTLY.</summary>
        private string id;

        /// <summary>Gets a string identifying this AppNexus client</summary>
        /// <remarks>
        /// First checks if backing field has already been initialized. If not,
        /// then checks persistent storage for a cached value. If no value is
        /// cached (or it is expired) then gets the member and caches the value.
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Getting Id must not throw. Exception is logged.")]
        public string Id
        {
            get
            {
                if (this.id == null)
                {
                    Tuple<string, DateTime> cached = null;
                    this.AppNexusMembers.TryGetValue(this.RestClient.Id, out cached);

                    if (cached == null || DateTime.UtcNow > cached.Item2)
                    {
                        try
                        {
                            var member = this.GetMember();
                            cached = new Tuple<string, DateTime>(
                                    "{0}[{1}]".FormatInvariant(member["id"], member["name"]),
                                    DateTime.UtcNow.AddHours(8));

                            try
                            {
                                this.AppNexusMembers[this.RestClient.Id] = cached;
                            }
                            catch (InvalidETagException)
                            {
                                // Just use the value that was written
                            }
                        }
                        catch (Exception e)
                        {
                            string user = "UNKNOWN";
                            try
                            {
                                user = this.IsAppNexusApp ?
                                    this.Config.GetValue("AppNexus.App.UserId") :
                                    this.Config.GetValue("AppNexus.Username");
                            }
                            catch
                            {
                            }

                            LogManager.Log(
                                LogLevels.Warning,
                                "Unable to get AppNexus member for user {0}: {1}",
                                user,
                                e);

                            return "UNKNOWN";
                        }
                    }

                    this.id = this.AppNexusMembers[this.RestClient.Id].Item1;
                }

                return this.id;
            }
        }

        /// <summary>Gets the cache of AppNexus members</summary>
        private IPersistentDictionary<Tuple<string, DateTime>> AppNexusMembers
        {
            get
            {
                return this.appNexusMembers =
                    this.appNexusMembers ??
                    PersistentDictionaryFactory.CreateDictionary<Tuple<string, DateTime>>("AppNexusMembers");
            }
        }

        /// <summary>Get the member information</summary>
        /// <returns>The values of the member if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetMember()
        {
            return this.TryGetObject(AppNexusValues.Member, Uris.GetMember);
        }

        /// <summary>Get all domain lists for the member</summary>
        /// <returns>Array of domain lists</returns>
        public IDictionary<string, object>[] GetMemberDomainLists()
        {
            return this.TryGetCollection(AppNexusValues.DomainLists, Uris.GetMemberDomainLists);
        }

        /// <summary>Get all segments for the member</summary>
        /// <returns>The segment values for the member if they exist; otherwise, null.</returns>
        public IDictionary<string, object>[] GetMemberSegments()
        {
            var member = this.GetMember();
            return this.TryGetCollection(
                AppNexusValues.Segments,
                MemberSegmentFilters,
                MemberSegmentGetCollectionPageSize,
                Uris.GetSegmentsForMember,
                member["id"]);
        }

        /// <summary>Get subset of segments for the member</summary>
        /// <param name="startSegment">The index of the first segment to get</param>
        /// <param name="maxSegments">The maximum number of segments to get</param>
        /// <returns>The segment values for the member if they exist; otherwise, null.</returns>
        public IDictionary<string, object>[] GetMemberSegments(int startSegment, int maxSegments)
        {
            var member = this.GetMember();
            int segmentCount;
            return this.TryGetCollection(
                startSegment,
                maxSegments,
                out segmentCount,
                AppNexusValues.Segments,
                Uris.GetSegmentsForMember,
                member["id"]);
        }

        /// <summary>Creates a member segment in AppNexus</summary>
        /// <param name="memberId">Member AppNexus id</param>
        /// <param name="code">Segment code</param>
        /// <param name="shortName">Segment short name</param>
        /// <returns>The AppNexus segment id</returns>
        public int CreateSegment(int memberId, string code, string shortName)
        {
            var segmentJson = AppNexusJson.ProfileSegmentEntryFormat
                .FormatInvariant(memberId, code, shortName);
            return this.CreateObject(segmentJson, Uris.CreateSegmentForMember, memberId);
        }

        /// <summary>Creates an advertiser in AppNexus</summary>
        /// <param name="name">Advertiser name</param>
        /// <param name="code">Company Entity External EntityId</param>
        /// <returns>The AppNexus advertiser id</returns>
        /// <exception cref="AppNexusClientException">
        /// An error occured interracting with the AppNexus service
        /// </exception>
        public int CreateAdvertiser(string name, string code)
        {
            var advertiserJson = AppNexusJson.CreateAdvertiserFormat
                .FormatInvariant(name, code);
            return this.CreateObject(advertiserJson, Uris.CreateAdvertiser);
        }

        /// <summary>Creates a line item in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="profileId">Profile id</param>
        /// <param name="name">Line item name</param>
        /// <param name="code">Line item code</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date (UTC)</param>
        /// <param name="endDate">End date (UTC)</param>
        /// <param name="totalBudget">Total budget</param>
        /// <returns>The AppNexus line item id</returns>
        public int CreateLineItem(
            int advertiserId,
            int profileId,
            string name,
            string code,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal totalBudget)
        {
            var lineItemJson = AppNexusJson.CreateLineItemFormat.FormatInvariant(
                name,
                code,
                active ? AppNexusValues.StateActive : AppNexusValues.StateInactive,
                GetApiTimeStamp(startDate),
                GetApiTimeStamp(endDate),
                totalBudget,
                profileId);
            return this.CreateObject(lineItemJson, Uris.CreateLineItem, advertiserId);
        }

        /// <summary>Updates a line item in AppNexus</summary>
        /// <param name="lineItemId">Id of the line item to update</param>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="name">Line item name</param>
        /// <param name="code">Line item code</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="totalBudget">Total budget</param>
        public void UpdateLineItem(
            int lineItemId,
            int advertiserId,
            string name,
            string code,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal totalBudget)
        {
            var lineItemJson = AppNexusJson.UpdateLineItemFormat.FormatInvariant(
                name,
                code,
                active ? AppNexusValues.StateActive : AppNexusValues.StateInactive,
                GetApiTimeStamp(startDate),
                GetApiTimeStamp(endDate),
                totalBudget);
            this.UpdateObject(lineItemJson, Uris.UpdateLineItem, lineItemId, advertiserId);
        }

        /// <summary>Creates a campaign in AppNexus</summary>
        /// <remarks>Uses lifetime budget/impression caps</remarks>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="name">Campaign name</param>
        /// <param name="code">Campaign code</param>
        /// <param name="lineItemId">Line item id</param>
        /// <param name="profileId">Profile id</param>
        /// <param name="creativeIds">Creative ids</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="lifetimeBudgetCap">Lifetime budget cap</param>
        /// <param name="lifetimeImpressionCap">Lifetime impression cap</param>
        /// <param name="baseBid">Base bid</param>
        /// <returns>The AppNexus campaign id</returns>
        public int CreateCampaign(
            int advertiserId,
            string name,
            string code,
            int lineItemId,
            int profileId,
            int[] creativeIds,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal lifetimeBudgetCap,
            long lifetimeImpressionCap,
            decimal baseBid)
        {
            var creativesJson = creativeIds.Select(creative =>
                AppNexusJson.CampaignCreativeEntryFormat.FormatInvariant(creative));
            var campaignJson = AppNexusJson.CreateCampaignFormat.FormatInvariant(
                name,
                code,
                active ? AppNexusValues.StateActive : AppNexusValues.StateInactive,
                GetApiTimeStamp(startDate),
                GetApiTimeStamp(endDate),
                lifetimeBudgetCap,
                lifetimeImpressionCap,
                baseBid,
                profileId,
                lineItemId,
                advertiserId,
                string.Join(",", creativesJson));
            return this.CreateObject(campaignJson, Uris.CreateCampaign, advertiserId);
        }

        /// <summary>Updates a campaign in AppNexus</summary>
        /// <param name="campaignCode">Code of the campaign to update</param>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="name">Campaign name</param>
        /// <param name="creativeIds">Creative ids</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="lifetimeBudgetCap">Daily budget cap</param>
        /// <param name="lifetimeImpressionCap">Daily impression cap</param>
        /// <param name="baseBid">Base bid</param>
        public void UpdateCampaign(
            string campaignCode,
            int advertiserId,
            string name,
            int[] creativeIds,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal lifetimeBudgetCap,
            long lifetimeImpressionCap,
            decimal baseBid)
        {
            var creativesJson = creativeIds.Select(creative =>
                AppNexusJson.CampaignCreativeEntryFormat.FormatInvariant(creative));
            var campaignJson = AppNexusJson.UpdateCampaignFormat.FormatInvariant(
                name,
                campaignCode,
                active ? AppNexusValues.StateActive : AppNexusValues.StateInactive,
                GetApiTimeStamp(startDate),
                GetApiTimeStamp(endDate),
                lifetimeBudgetCap,
                lifetimeImpressionCap,
                baseBid,
                string.Join(",", creativesJson));
            this.UpdateObject(campaignJson, Uris.UpdateCampaign, campaignCode, advertiserId);
        }

        /// <summary>Updates a campaign in AppNexus</summary>
        /// <param name="campaignCode">Code of the campaign to update</param>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="active">If the campaign's state should be active or inactive</param>
        public void UpdateCampaignState(string campaignCode, int advertiserId, bool active)
        {
            var campaignJson = AppNexusJson.UpdateCampaignStateFormat.FormatInvariant(
                active ? AppNexusValues.StateActive : AppNexusValues.StateInactive);
            this.UpdateObject(campaignJson, Uris.UpdateCampaign, campaignCode, advertiserId);
        }

        /// <summary>Creates a targeting profile in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="code">Profile code</param>
        /// <param name="allowUnknownAge">Allow unknown age</param>
        /// <param name="ageRange">Age range</param>
        /// <param name="gender">Gender demographic</param>
        /// <param name="segments">Targeting segment id/description pairs</param>
        /// <param name="dmaTargets">DMA targeting metro code/name pairs</param>
        /// <param name="regionTargets">State targeting codes (ISO 3166-2 for US and Canada)</param>
        /// <param name="location">Location on the page to target</param>
        /// <param name="inventoryAttributeTargets">Inventory attributes</param>
        /// <param name="contentCategoryTargets">Content categories</param>
        /// <param name="domainListTargets">Domains lists</param>
        /// <returns>The AppNexus profile id</returns>
        public int CreateCampaignProfile(
            int advertiserId,
            string code,
            bool allowUnknownAge,
            Tuple<int, int> ageRange,
            string gender,
            IDictionary<int, string> segments,
            IDictionary<int, string> dmaTargets,
            IEnumerable<string> regionTargets,
            PageLocation location,
            IEnumerable<int> inventoryAttributeTargets,
            IDictionary<int, bool> contentCategoryTargets,
            IDictionary<int, bool> domainListTargets)
        {
            var ageTargets =
                ageRange != null ?
                AppNexusJson.ProfileAgeTargetFormat
                    .FormatInvariant(
                        allowUnknownAge.ToString().ToLowerInvariant(),
                        ageRange.Item1,
                        ageRange.Item2) :
                string.Empty;

            var genderTarget =
                !string.IsNullOrWhiteSpace(gender) ?
                AppNexusJson.ProfileGenderTargetFormat
                    .FormatInvariant(gender) :
                    string.Empty;

            var segmentsJson = segments.Keys.Select(segment =>
                AppNexusJson.ProfileSegmentEntryFormat
                .FormatInvariant(segment))
                .ToArray();

            var dmaTargetsJson = dmaTargets.Select(dma =>
                AppNexusJson.ProfileDmaTargetsEntryFormat
                .FormatInvariant(dma.Key, dma.Value))
                .ToArray();
            var dmasJson =
                dmaTargets.Count == 0 ?
                    string.Empty :
                    @"""dma_action"":""include"",""dma_targets"":[{0}],"
                    .FormatInvariant(string.Join(",", dmaTargetsJson));

            var regionTargetsJson = regionTargets.Select(region =>
                AppNexusJson.ProfileRegionTargetsEntryFormat
                .FormatInvariant(region))
                .ToArray();
            var regionsJson =
                regionTargets.Count() == 0 ?
                    string.Empty :
                    @"""region_action"":""include"",""region_targets"":[{0}],"
                    .FormatInvariant(string.Join(",", regionTargetsJson));

            var positions =
                location != PageLocation.Any &&
                location != PageLocation.Unknown ?
                AppNexusJson.ProfilePositionFormat
                .FormatInvariant(location.ToString().ToLowerInvariant()) :
                string.Empty;

            var useInventoryAttributes = inventoryAttributeTargets.Count() > 0;
            var inventoryAttributesJson = inventoryAttributeTargets
                .Select(attribute =>
                    AppNexusJson.ProfileInventoryAttributeTargetsEntryFormat
                    .FormatInvariant(attribute));

            // Content category id 0 is actually the Allow_Unknown flag
            var allowUnknownContentCategories =
                contentCategoryTargets.ContainsKey(0) ? contentCategoryTargets[0] :
                false;

            var contentCategoriesJson = contentCategoryTargets
                .Where(kvp => kvp.Key != 0)
                .Select(category =>
                    AppNexusJson.ContentCategoryTargetsEntryFormat
                    .FormatInvariant(category.Key, category.Value ? "include" : "exclude"));

            // Domain list targets
            var domainListAction = domainListTargets != null && domainListTargets.Count > 0 ? "include" : "exclude";
            var domainListTargetsJson = string.Empty;
            if (domainListTargets != null && domainListTargets.Count > 0)
            {
                var domainListInclude = domainListTargets.First().Value;

                // Domain list targets should all be the same
                if (!domainListTargets.Values.All(include => domainListInclude == include))
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "Invalid domain-list targets for campaign profile '{0}'. All entries must have the same action.\nDomainListTargets: {1}",
                        code,
                        domainListTargets.ToString<int, bool>());
                }

                domainListTargetsJson = string.Join(",", domainListTargets.Keys.Select(id => @"{{""id"":{0}}}".FormatInvariant(id)));
            }

            var profileDescription = string.Join(" + ", segments.Values.Concat(dmaTargets.Values));

            var profileJson =
                AppNexusJson.CampaignProfileFormat
                .FormatInvariant(
                code,
                profileDescription,
                advertiserId,
                string.Join(",", segmentsJson),
                dmasJson,
                regionsJson,
                (location == PageLocation.Any).ToString().ToLowerInvariant(),
                positions,
                useInventoryAttributes.ToString().ToLowerInvariant(),
                string.Join(",", inventoryAttributesJson),
                ageTargets,
                allowUnknownContentCategories.ToString().ToLowerInvariant(),
                string.Join(",", contentCategoriesJson),
                genderTarget,
                domainListAction,
                domainListTargetsJson);

            return this.CreateObject(profileJson, Uris.CreateProfile, advertiserId);
        }

        /// <summary>Creates a targeting profile for a line-item in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="description">Profile description</param>
        /// <param name="frequencyCaps">Frequency caps</param>
        /// <param name="includeDomainListId">List of domains to include</param>
        /// <param name="domainTargets">Additional domains to target (if any)</param>
        /// <returns>The AppNexus profile id</returns>
        public int CreateLineItemProfile(
            int advertiserId,
            string description,
            IDictionary<AppNexusFrequencyType, int> frequencyCaps,
            int? includeDomainListId,
            string[] domainTargets)
        {
            // Dictionary of additional profile properties
            var profileProperties = new Dictionary<string, object>();

            // Add properties for the frequency mappings
            profileProperties.Add(
                frequencyCaps.ToDictionary(
                    kvp => ProfileFrequencyMappings[kvp.Key],
                    kvp => (object)kvp.Value));

            // Add include domain list (if any)
            if (includeDomainListId != null)
            {
                profileProperties["domain_list_action"] = "include";
                profileProperties["domain_list_targets"] = new[]
                {
                    new Dictionary<string, object> { { "id", includeDomainListId.Value } }
                };
            }

            // Add domain targets (if any)
            if (domainTargets != null && domainTargets.Length > 0)
            {
                profileProperties["domain_action"] = "include";
                profileProperties["domain_targets"] = domainTargets
                    .Distinct()
                    .Select(domain => new Dictionary<string, object> { { "domain", domain } })
                    .ToArray();
            }

            // Serialize to JSON and strip enclosing braces
            var profilePropertiesJson =
                JsonSerializer.Serialize(profileProperties)
                .Trim('{', '}');
            if (!string.IsNullOrWhiteSpace(profilePropertiesJson))
            {
                profilePropertiesJson += ",";
            }

            // Create the object from formatted line-item profile JSON
            var profileJson = AppNexusJson.LineItemProfileFormat
                .FormatInvariant(
                description,
                advertiserId,
                profilePropertiesJson);
            return this.CreateObject(profileJson, Uris.CreateProfile, advertiserId);
        }

        /// <summary>Creates a creative in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="name">Creative name</param>
        /// <param name="code">The code</param>
        /// <param name="templateId">Template id</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <param name="content">Tag HTML/JS content or base64 binary</param>
        /// <param name="fileName">Binary content file name (optional)</param>
        /// <param name="clickUrl">Click URL (optional)</param>
        /// <param name="flashBackupContent">Flash backup image content base64 (optional)</param>
        /// <param name="flashBackupFileName">Flash backup image file name (optional)</param>
        /// <param name="flashClickVariable">Flash click variable (optional)</param>
        /// <returns>The AppNexus creative id</returns>
        public int CreateCreative(
            int advertiserId,
            string name,
            string code,
            int templateId,
            int width,
            int height,
            string content,
            string fileName = null,
            string clickUrl = null,
            string flashBackupContent = null,
            string flashBackupFileName = null,
            string flashClickVariable = null)
    {
            var creative = new Dictionary<string, object>
            {
                {
                    "creative",
                    new Dictionary<string, object>
                    {
                        { "code", code },
                        { "name", name },
                        {
                            "template",
                            new Dictionary<string, object> { { "id", templateId } }
                        },
                        { "width", width },
                        { "height", height },
                        { "content", content },
                        { "click_url", clickUrl },
                        { "file_name", fileName },
                        { "flash_click_variable", flashClickVariable },
                        { "flash_backup_content", flashBackupContent },
                        { "flash_backup_filename", flashBackupFileName },
                    }
                    .Where(kvp => kvp.Value != null)
                    .ToDictionary()
                }
            };
            /*
            var creativeJson = AppNexusJson.CreativeFormat.FormatInvariant(
                name,
                code,
                active ? AppNexusValues.StateActive : AppNexusValues.StateInactive,
                JsonEscape(content),
                width,
                height,
                advertiserId,
                7);
             */
            var creativeJson = JsonSerializer.Serialize(creative);
            return this.CreateObject(creativeJson, Uris.CreateCreative, advertiserId);
        }

        /// <summary>Creates a domain list</summary>
        /// <param name="name">Domain list name</param>
        /// <param name="description">Domain list description</param>
        /// <param name="domains">List of domains</param>
        /// <returns>The AppNexus domain-list id</returns>
        public int CreateDomainList(
            string name,
            string description,
            string[] domains)
        {
            var quotedDomains = domains
                .Select(domain =>
                    @"""{0}""".FormatInvariant(domain));
            var domainListJson = AppNexusJson.DomainListFormat.FormatInvariant(
                name,
                description,
                string.Join(",", quotedDomains));
            return this.CreateObject(domainListJson, Uris.CreateDomainList);
        }

        /// <summary>Get the advertiser for the code</summary>
        /// <param name="advertiserCode">The code of the advertiser</param>
        /// <returns>The values of the campaign if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetAdvertiserByCode(string advertiserCode)
        {
            return this.TryGetObject(AppNexusValues.Advertiser, Uris.GetAdvertiserByCode, advertiserCode);
        }

        /// <summary>Get the advertisers of the member</summary>
        /// <returns>Array of advertisers</returns>
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Needs to be a method for consistency")]
        public IDictionary<string, object>[] GetMemberAdvertisers()
        {
            return this.TryGetCollection(AppNexusValues.Advertisers, Uris.GetMemberAdvertisers);
        }

        /// <summary>Get the campaign for the code</summary>
        /// <param name="advertiserId">The AppNexus id of the advertiser</param>
        /// <param name="campaignCode">The code of the campaign</param>
        /// <returns>The values of the campaign if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetCampaignByCode(int advertiserId, string campaignCode)
        {
            return this.TryGetObject(AppNexusValues.Campaign, Uris.GetCampaignByCode, campaignCode, advertiserId);
        }

        /// <summary>Get the targeting profile for the code</summary>
        /// <param name="advertiserId">The AppNexus id of the advertiser</param>
        /// <param name="profileCode">The code of the profile</param>
        /// <returns>The values of the profile if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetProfileByCode(int advertiserId, string profileCode)
        {
            return this.TryGetObject(AppNexusValues.Profile, Uris.GetProfileByCode, profileCode, advertiserId);
        }

        /// <summary>Get the targeting profile for the id</summary>
        /// <param name="advertiserId">The id of the advertiser</param>
        /// <param name="profileId">The id of the profile</param>
        /// <returns>The values of the profile if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetProfileById(int advertiserId, int profileId)
        {
            return this.TryGetObject(AppNexusValues.Profile, Uris.GetProfileById, profileId, advertiserId);
        }

        /// <summary>Get the line item for the code</summary>
        /// <param name="advertiserCode">The code of the advertiser</param>
        /// <param name="lineItemCode">The code of the line item</param>
        /// <returns>The values of the line item if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetLineItemByCode(string advertiserCode, string lineItemCode)
        {
            return this.TryGetObject(AppNexusValues.LineItem, Uris.GetLineItemByCode, lineItemCode, advertiserCode);
        }

        /// <summary>Get the line item for the id</summary>
        /// <param name="advertiserId">The AppNexus id of the advertiser</param>
        /// <param name="lineItemId">The AppNexus id of the line item</param>
        /// <returns>The values of the line item if it exists; otherwise, null.</returns>
        public IDictionary<string, object> GetLineItemById(int advertiserId, int lineItemId)
        {
            return this.TryGetObject(AppNexusValues.LineItem, Uris.GetLineItemById, lineItemId, advertiserId);
        }

        /// <summary>Gets all creatives for a member from AppNexus</summary>
        /// <returns>Array of creatives</returns>
        public IDictionary<string, object>[] GetMemberCreatives()
        {
            return this.TryGetCollection(AppNexusValues.Creatives, Uris.GetMemberCreatives);
        }

        /// <summary>Gets all creative formats</summary>
        /// <returns>Array of creative formats</returns>
        public IDictionary<string, object>[] GetCreativeFormats()
        {
            return this.TryGetCollection(AppNexusValues.CreativeFormats, Uris.GetCreativeFormats);
        }

        /// <summary>Gets all creative templates</summary>
        /// <returns>Array of creative templates</returns>
        public IDictionary<string, object>[] GetCreativeTemplates()
        {
            return this.TryGetCollection(AppNexusValues.CreativeTemplates, Uris.GetCreativeTemplates);
        }

        /// <summary>Gets all creatives for an advertiser from AppNexus</summary>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <returns>Array of creatives</returns>
        public IDictionary<string, object>[] GetAdvertiserCreatives(int advertiserId)
        {
            return this.TryGetCollection(AppNexusValues.Creatives, Uris.GetAdvertiserCreatives, advertiserId);
        }

        /// <summary>Gets a specific creative from AppNexus</summary>
        /// <param name="creativeId">The AppNexus creative id</param>
        /// <returns>The creative values</returns>
        public IDictionary<string, object> GetCreative(int creativeId)
        {
            var response = this.RestClient.Get(Uris.GetCreative, creativeId);
            var responseValues = this.RestClient.TryGetResponseValues(response);
            if (responseValues == null)
            {
                throw new AppNexusClientException(
                    "Failed to get creative '{0}".FormatInvariant(creativeId),
                    response);
            }

            return responseValues[AppNexusValues.Creative] as IDictionary<string, object>;
        }

        /// <summary>Deletes an advertiser in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        public void DeleteAdvertiser(int advertiserId)
        {
            this.DeleteObject("advertiser?id={0}", advertiserId);
        }

        /// <summary>Deletes a line-item in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="lineItemId">The AppNexus line-item id</param>
        public void DeleteLineItem(int advertiserId, int lineItemId)
        {
            this.DeleteObject("line-item?id={0}&advertiser_id={1}", lineItemId, advertiserId);
        }

        /// <summary>Deletes a campaign in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="campaignId">The AppNexus campaign id</param>
        public void DeleteCampaign(int advertiserId, int campaignId)
        {
            this.DeleteObject("campaign?id={0}&advertiser_id={1}", campaignId, advertiserId);
        }

        /// <summary>Deletes a targeting profile in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="profileId">The AppNexus profile id</param>
        public void DeleteProfile(int advertiserId, int profileId)
        {
            this.DeleteObject("profile?id={0}&advertiser_id={1}", profileId, advertiserId);
        }

        /// <summary>Deletes a creative in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="creativeId">The AppNexus creative id</param>
        public void DeleteCreative(int advertiserId, int creativeId)
        {
            this.DeleteObject("creative?id={0}&advertiser_id={1}", creativeId, advertiserId);
        }

        /// <summary>Deletes a domain list</summary>
        /// <param name="domainListId">The AppNexus domain list id</param>
        public void DeleteDomainList(int domainListId)
        {
            this.DeleteObject("domain-list?id={0}", domainListId);
        }

        /// <summary>Requests a report for the specified line item from AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="lineItemId">AppNexus line-item id</param>
        /// <returns>The AppNexus report id</returns>
        public string RequestDeliveryReport(int advertiserId, int lineItemId)
        {
            var reportRequestJson = AppNexusJson.DeliveryReportRequestFormat
                .FormatInvariant(lineItemId);
            return this.RequestReport(advertiserId, reportRequestJson);
        }

        /// <summary>Gets the cities that can be targeted</summary>
        /// <param name="filter">City filter. Examples: "US", "US/NY", "US/ALL"</param>
        /// <returns>The cities as an array of dictionaries with id, name, region and country values.</returns>
        /// <seealso href="https://wiki.appnexus.com/display/api/City+Service"/>
        public IDictionary<string, object>[] GetCities(string filter)
        {
            return this.TryGetUnpagedCollection("cities", Uris.GetCities, filter);
        }

        /// <summary>Gets the categories that can be targeted</summary>
        /// <returns>The categories as an array of dictionaries including id and name values.</returns>
        public IDictionary<string, object>[] GetContentCategories()
        {
            var memberCategories =
                this.TryGetCollection("content-categories", Uris.GetMemberContentCategories);
            
            // TODO: Do we need/want universal or just the member's content categories?
            var universalCategories =
                this.TryGetCollection("content-categories", Uris.GetUniversalContentCategories);
            
            return memberCategories
                .Concat(universalCategories)
                .ToArray();
        }

        /// <summary>Gets the inventory source targets</summary>
        /// <returns>The inventory source targets as an array of dictionaries including id and name values.</returns>
        public IDictionary<string, object>[] GetInventoryAttributes()
        {
            return this.TryGetCollection("inventory-attributes", Uris.GetInventorySources);
        }

        /// <summary>URIs for interracting with the AppNexus service</summary>
        private static class Uris
        {
            /// <summary>Get member URI</summary>
            public const string GetMember = "member";

            /// <summary>Get segments for member URI</summary>
            ////public const string GetSegmentsForMember = "segment?member_id={0}";
            public const string GetSegmentsForMember = "segment?advertiser_id=null";

            /// <summary>Create member segment URI</summary>
            public const string CreateSegmentForMember = "segment?member_id={0}";

            /// <summary>Create advertiser URI</summary>
            public const string CreateAdvertiser = "advertiser";

            /// <summary>Create line item URI</summary>
            public const string CreateLineItem = "line-item?advertiser_id={0}";

            /// <summary>Create campaign URI format</summary>
            public const string CreateCampaign = "campaign?advertiser_id={0}";

            /// <summary>Create profile URI format</summary>
            public const string CreateProfile = "profile?advertiser_id={0}";

            /// <summary>Create creative URI format</summary>
            public const string CreateCreative = "creative?advertiser_id={0}";

            /// <summary>Create domain list URI</summary>
            public const string CreateDomainList = "domain-list";

            /// <summary>Update line item URI format</summary>
            public const string UpdateLineItem = "line-item?id={0}&advertiser_id={1}";

            /// <summary>Update campaign URI format</summary>
            public const string UpdateCampaign = "campaign?code={0}&advertiser_id={1}";

            /// <summary>Get member creatives URI</summary>
            public const string GetMemberCreatives = "creative";

            /// <summary>Get advertiser creatives URI format</summary>
            public const string GetAdvertiserCreatives = "creative?advertiser_id={0}";

            /// <summary>Get creative URI format</summary>
            public const string GetCreative = "creative?id={0}";

            /// <summary>Get creative formats URI</summary>
            public const string GetCreativeFormats = "creative-format";

            /// <summary>Get creative templates URI</summary>
            public const string GetCreativeTemplates = "template";

            /// <summary>Get advertiser URI format</summary>
            public const string GetAdvertiserByCode = "advertiser?code={0}";

            /// <summary>Get member advertisers URI</summary>
            public const string GetMemberAdvertisers = "advertiser";

            /// <summary>Get member domain lists URI</summary>
            public const string GetMemberDomainLists = "domain-list";

            /// <summary>Get line-item by code URI format</summary>
            public const string GetLineItemByCode = "line-item?code={0}&advertiser_code={1}";

            /// <summary>Get line-item by id URI format</summary>
            public const string GetLineItemById = "line-item?id={0}&advertiser_id={1}";

            /// <summary>Get campaign by code URI format</summary>
            public const string GetCampaignByCode = "campaign?code={0}&advertiser_id={1}";

            /// <summary>Get profile by code URI format</summary>
            public const string GetProfileByCode = "profile?code={0}&advertiser_id={1}";

            /// <summary>Get profile by id URI format</summary>
            public const string GetProfileById = "profile?id={0}&advertiser_id={1}";

            /// <summary>Get cities format</summary>
            public const string GetCities = "city/{0}";

            /// <summary>Get member content categories</summary>
            public const string GetMemberContentCategories = "content-category";

            /// <summary>Get universal content categories</summary>
            public const string GetUniversalContentCategories = "content-category?category_type=universal";

            /// <summary>Get the inventory sources</summary>
            public const string GetInventorySources = "inventory-attribute";
        }
    }
}
