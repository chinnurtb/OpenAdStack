//-----------------------------------------------------------------------
// <copyright file="CampaignFixture.cs" company="Rare Crowds Inc">
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
