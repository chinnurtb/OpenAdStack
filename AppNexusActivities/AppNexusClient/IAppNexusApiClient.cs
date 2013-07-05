//-----------------------------------------------------------------------
// <copyright file="IAppNexusApiClient.cs" company="Rare Crowds Inc">
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
using DeliveryNetworkUtilities;
using Utilities.Net;

namespace AppNexusClient
{
    /// <summary>Defines the interface for the AppNexus Client</summary>
    public interface IAppNexusApiClient : IDeliveryNetworkClient
    {
        /// <summary>Gets a string identifying this AppNexus client</summary>
        string Id { get; }

        /// <summary>Get the advertiser for the code</summary>
        /// <param name="advertiserCode">The code of the advertiser</param>
        /// <returns>The values of the campaign if it exists; otherwise, null.</returns>
        IDictionary<string, object> GetAdvertiserByCode(string advertiserCode);

        /// <summary>Get the advertisers of the member</summary>
        /// <returns>Array of advertisers</returns>
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Needs to be a method for consistency")]
        IDictionary<string, object>[] GetMemberAdvertisers();

        /// <summary>Get the line item for the code</summary>
        /// <param name="advertiserCode">The code of the advertiser</param>
        /// <param name="lineItemCode">The code of the line item</param>
        /// <returns>The values of the line item if it exists; otherwise, null.</returns>
        IDictionary<string, object> GetLineItemByCode(string advertiserCode, string lineItemCode);

        /// <summary>Get the line item for the id</summary>
        /// <param name="advertiserId">The AppNexus id of the advertiser</param>
        /// <param name="lineItemId">The AppNexus id of the line item</param>
        /// <returns>The values of the line item if it exists; otherwise, null.</returns>
        IDictionary<string, object> GetLineItemById(int advertiserId, int lineItemId);

        /// <summary>Get the campaign for the code</summary>
        /// <param name="advertiserId">The AppNexus id of the advertiser</param>
        /// <param name="campaignCode">The code of the campaign</param>
        /// <returns>The values of the campaign if it exists; otherwise, null.</returns>
        IDictionary<string, object> GetCampaignByCode(int advertiserId, string campaignCode);

        /// <summary>Get the targeting profile for the code</summary>
        /// <param name="advertiserId">The AppNexus id of the advertiser</param>
        /// <param name="profileCode">The code of the profile</param>
        /// <returns>The values of the profile if it exists; otherwise, null.</returns>
        IDictionary<string, object> GetProfileByCode(int advertiserId, string profileCode);

        /// <summary>Get the targeting profile for the id</summary>
        /// <param name="advertiserId">The id of the advertiser</param>
        /// <param name="profileId">The id of the profile</param>
        /// <returns>The values of the profile if it exists; otherwise, null.</returns>
        IDictionary<string, object> GetProfileById(int advertiserId, int profileId);

        /// <summary>Get the member information</summary>
        /// <returns>The values of the member if it exists; otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Needs to be a method for consistency")]
        IDictionary<string, object> GetMember();

        /// <summary>Get all domain lists for the member</summary>
        /// <returns>Array of domain lists</returns>
        IDictionary<string, object>[] GetMemberDomainLists();

        /// <summary>Get all segments for the member</summary>
        /// <returns>The segment values for the member if they exist; otherwise, null.</returns>
        IDictionary<string, object>[] GetMemberSegments();

        /// <summary>Get subset of segments for the member</summary>
        /// <param name="startSegment">The index of the first segment to get</param>
        /// <param name="maxSegments">The maximum number of segments to get</param>
        /// <returns>The segment values for the member if they exist; otherwise, null.</returns>
        IDictionary<string, object>[] GetMemberSegments(int startSegment, int maxSegments);

        /// <summary>Creates a member segment in AppNexus</summary>
        /// <param name="memberId">Member AppNexus id</param>
        /// <param name="code">Segment code</param>
        /// <param name="shortName">Segment short name</param>
        /// <returns>The AppNexus segment id</returns>
        int CreateSegment(int memberId, string code, string shortName);

        /// <summary>Creates an advertiser in AppNexus</summary>
        /// <param name="name">Advertiser name</param>
        /// <param name="code">Company Entity External EntityId</param>
        /// <returns>The AppNexus advertiser id</returns>
        int CreateAdvertiser(string name, string code);

        /// <summary>Creates a line item in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="profileId">Profile id</param>
        /// <param name="name">Line item name</param>
        /// <param name="code">Line item code</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="totalBudget">Total budget</param>
        /// <returns>The AppNexus line item id</returns>
        int CreateLineItem(
            int advertiserId,
            int profileId,
            string name,
            string code,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal totalBudget);

        /// <summary>Updates a line item in AppNexus</summary>
        /// <param name="lineItemId">Id of the line item to update</param>
        /// <param name="advertiserId">Advertiser code</param>
        /// <param name="name">Line item name</param>
        /// <param name="code">Line item code</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="totalBudget">Total budget</param>
        void UpdateLineItem(
            int lineItemId,
            int advertiserId,
            string name,
            string code,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal totalBudget);

        /// <summary>Creates a campaign in AppNexus</summary>
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
        int CreateCampaign(
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
            decimal baseBid);

        /// <summary>Updates a campaign in AppNexus</summary>
        /// <param name="campaignCode">Code of the campaign to update</param>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="name">Campaign name</param>
        /// <param name="creativeIds">Creative ids</param>
        /// <param name="active">If active</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <param name="lifetimeBudgetCap">Lifetime budget cap</param>
        /// <param name="lifetimeImpressionCap">Lifetime impression cap</param>
        /// <param name="baseBid">Base bid</param>
        void UpdateCampaign(
            string campaignCode,
            int advertiserId,
            string name,
            int[] creativeIds,
            bool active,
            DateTime startDate,
            DateTime endDate,
            decimal lifetimeBudgetCap,
            long lifetimeImpressionCap,
            decimal baseBid);

        /// <summary>Updates a campaign in AppNexus</summary>
        /// <param name="campaignCode">Code of the campaign to update</param>
        /// <param name="advertiserId">Advertiser id</param>
        /// <param name="active">If the campaign's state should be active or inactive</param>
        void UpdateCampaignState(string campaignCode, int advertiserId, bool active);

        /// <summary>Creates a targeting profile for a campaign in AppNexus</summary>
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
        int CreateCampaignProfile(
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
            IDictionary<int, bool> domainListTargets);
        
        /// <summary>Creates a targeting profile for a line-item in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="code">Profile code</param>
        /// <param name="frequencyCaps">Frequency caps</param>
        /// <param name="includeDomainListId">List of domains to include (if any)</param>
        /// <param name="domainTargets">Additional domains to target (if any)</param>
        /// <returns>The AppNexus profile id</returns>
        int CreateLineItemProfile(
            int advertiserId,
            string code,
            IDictionary<AppNexusFrequencyType, int> frequencyCaps,
            int? includeDomainListId,
            string[] domainTargets);

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
        [SuppressMessage("Microsoft.Design", "CA1026", Justification = "Defaults make more sense than 3 overloads here")]
        [SuppressMessage("Microsoft.Design", "CA1054", Justification = "Use of 'URL' string is consistent with AppNexus API")]
        int CreateCreative(
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
            string flashClickVariable = null);

        /// <summary>Gets all creatives for a member from AppNexus</summary>
        /// <returns>Array of creatives</returns>
        IDictionary<string, object>[] GetMemberCreatives();

        /// <summary>Gets all creatives for an advertiser from AppNexus</summary>
        /// <param name="advertiserId">The AppNexus advertiser id</param>
        /// <returns>Array of creatives</returns>
        IDictionary<string, object>[] GetAdvertiserCreatives(int advertiserId);

        /// <summary>Gets a specific creative from AppNexus</summary>
        /// <param name="creativeId">The AppNexus creative id</param>
        /// <returns>The creative values</returns>
        IDictionary<string, object> GetCreative(int creativeId);

        /// <summary>Gets all creative formats</summary>
        /// <returns>Array of creative formats</returns>
        IDictionary<string, object>[] GetCreativeFormats();

        /// <summary>Gets all creative templates</summary>
        /// <returns>Array of creative templates</returns>
        IDictionary<string, object>[] GetCreativeTemplates();

        /// <summary>Creates a domain list</summary>
        /// <param name="name">Domain list name</param>
        /// <param name="description">Domain list description</param>
        /// <param name="domains">List of domains</param>
        /// <returns>The AppNexus domain-list id</returns>
        int CreateDomainList(
            string name,
            string description,
            string[] domains);

        /// <summary>Deletes an advertiser in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        void DeleteAdvertiser(int advertiserId);

        /// <summary>Deletes a line-item in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="lineItemId">The AppNexus line-item id</param>
        void DeleteLineItem(int advertiserId, int lineItemId);

        /// <summary>Deletes a campaign in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="campaignId">The AppNexus campaign id</param>
        void DeleteCampaign(int advertiserId, int campaignId);

        /// <summary>Deletes a targeting profile in AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="profileId">The AppNexus profile id</param>
        void DeleteProfile(int advertiserId, int profileId);

        /// <summary>Deletes a creative</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="creativeId">The AppNexus creative id</param>
        void DeleteCreative(int advertiserId, int creativeId);

        /// <summary>Deletes a domain list</summary>
        /// <param name="domainListId">The AppNexus domain list id</param>
        void DeleteDomainList(int domainListId);

        /// <summary>Requests a report for the specified line item from AppNexus</summary>
        /// <param name="advertiserId">Advertiser AppNexus id</param>
        /// <param name="lineItemId">AppNexus line-item id</param>
        /// <returns>The AppNexus report id</returns>
        string RequestDeliveryReport(int advertiserId, int lineItemId);

        /// <summary>Retrieve the specified report from AppNexus</summary>
        /// <param name="reportId">Report AppNexus id</param>
        /// <returns>If available, the report CSV data; Otherwise, null.</returns>
        string RetrieveReport(string reportId);

        /// <summary>Gets the cities that can be targeted</summary>
        /// <param name="filter">City filter. Examples: "US", "US/NY", "US/ALL"</param>
        /// <returns>The cities as an array of dictionaries with id, name, region and country values.</returns>
        IDictionary<string, object>[] GetCities(string filter);

        /// <summary>Gets the content categories that can be targeted</summary>
        /// <returns>The categories as an array of dictionaries including id and name values.</returns>
        IDictionary<string, object>[] GetContentCategories();

        /// <summary>Gets the inventory source targets</summary>
        /// <returns>The inventory source targets as an array of dictionaries including id and name values.</returns>
        IDictionary<string, object>[] GetInventoryAttributes();
    }
}
