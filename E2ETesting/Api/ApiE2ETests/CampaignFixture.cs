//-----------------------------------------------------------------------
// <copyright file="CampaignFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using E2ETestUtilities;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestUtilities;

namespace ApiIntegrationTests
{
    /// <summary>Tests for campaign API</summary>
    [TestClass]
    public class CampaignFixture : ApiFixtureBase
    {
        /// <summary>Round-trip a campaign</summary>
        [TestMethod]
        public void RoundtripCampaign()
        {
            // Create an advertiser
            var advertiserName = Guid.NewGuid().ToString("n");
            var advertiserEntityId = this.CreateCompany(advertiserName, "Advertiser");
            Assert.IsNotNull(advertiserEntityId);

            // Create the campaign
            var campaignName = Guid.NewGuid().ToString("n");
            var newCampaign = new Dictionary<string, object>
                {
                    { "EntityCategory", "Campaign" },
                    { "ExternalName", campaignName },
                    { "ExternalType", "DynamicAllocationCampaign" },
                    {
                        "Properties",
                        new Dictionary<string, object>
                        {
                            { "Status", "Draft" },
                            { "Budget", "12345" },
                            { "StartDate", "2012-07-23T16:25:00.0000000Z" },
                            { "EndDate", "2012-08-07T16:25:00.0000000Z" }
                        }
                    }
                };
            var campaignJson = JsonConvert.SerializeObject(newCampaign);
            var createCampaignUrl =
                "entity/company/{0}/campaign"
                .FormatInvariant(advertiserEntityId);
            var createCampaignResponse =
                this.RestClient.SendRequest(HttpMethod.POST, createCampaignUrl, campaignJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Campaign")
                .AssertContainsJsonValue("EntityCategory", "Campaign")
                .AssertContainsJsonValue("ExternalName", campaignName)
                .AssertContainsJsonValue("ExternalType", "DynamicAllocationCampaign")
                .AssertContainsJsonValue("Properties")
                .AssertContainsJsonValue("Status", "Draft")
                .AssertContainsJsonValue("Budget", 12345m)
                .AssertContainsJsonValue("StartDate", "2012-07-23T16:25:00.0000000Z")
                .AssertContainsJsonValue("EndDate", "2012-08-07T16:25:00.0000000Z");
            var campaign = createCampaignResponse
                .TryDeserializeContentJson()["Campaign"]
                as IDictionary<string, object>;
            var campaignEntityId = campaign["ExternalEntityId"];

            // Get the created campaign
            var getCampaignUrl =
                "entity/company/{0}/campaign/{1}"
                .FormatInvariant(
                    advertiserEntityId,
                    campaignEntityId);
            this.RestClient.SendRequest(HttpMethod.GET, getCampaignUrl)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Campaign")
                .AssertContainsJsonValue("EntityCategory", "Campaign")
                .AssertContainsJsonValue("ExternalName", campaignName)
                .AssertContainsJsonValue("ExternalType", "DynamicAllocationCampaign")
                .AssertContainsJsonValue("Properties")
                .AssertContainsJsonValue("Status", "Draft")
                .AssertContainsJsonValue("Budget", 12345m)
                .AssertContainsJsonValue("StartDate", "2012-07-23T16:25:00.0000000Z")
                .AssertContainsJsonValue("EndDate", "2012-08-07T16:25:00.0000000Z");
        }
    }
}
