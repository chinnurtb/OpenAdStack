//-----------------------------------------------------------------------
// <copyright file="CreativeFixture.cs" company="Rare Crowds Inc">
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
using System.Threading;
using E2ETestUtilities;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestUtilities;

namespace ApiIntegrationTests
{
    /// <summary>Tests for creative API</summary>
    [TestClass]
    public class CreativeFixture : ApiFixtureBase
    {
        /// <summary>Test creative tag</summary>
        private const string TestTag = 
@"<a href=""${CLICK_URL}http://comicsdungeon.com/DCDigitalStore.aspx?${CACHEBUSTER}"" TARGET=""_blank""><img src=""http://comicsdungeon.com/images/dcdigitalcdi.jpg"" border=""0"" width=""300"" height=""250"" alt=""Advertisement - Comics Dungeon Digital DC Comics"" /></a>";

        /// <summary>Round-trip a creative</summary>
        [TestMethod]
        public void RoundtripCreative()
        {
            // Create an advertiser
            var advertiserName = Guid.NewGuid().ToString("n");
            var advertiserEntityId =
                this.CreateCompany(
                    advertiserName,
                    "Advertiser");
            Assert.IsNotNull(advertiserEntityId);

            // Create a creative
            var creativeName = Guid.NewGuid().ToString("n");
            var creativeWidth = 500m;
            var creativeHeight = 50m;
            var newCreative = new Dictionary<string, object>
                {
                    { "EntityCategory", "Creative" },
                    { "ExternalName", creativeName },
                    { "ExternalType", "AdThirdParty" },
                    {
                        "Properties",
                        new Dictionary<string, object>
                        {
                            { "Tag", TestTag },
                            { "Width", creativeWidth },
                            { "Height", creativeHeight }
                        }
                    }
                };
            var creativeJson = JsonConvert.SerializeObject(newCreative);
            var createCampaignUrl =
                "entity/company/{0}/creative"
                .FormatInvariant(advertiserEntityId);
            var createCampaignResponse =
                this.RestClient.SendRequest(HttpMethod.POST, createCampaignUrl, creativeJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Creative")
                .AssertContainsJsonValue("EntityCategory", "Creative")
                .AssertContainsJsonValue("ExternalName", creativeName)
                .AssertContainsJsonValue("Properties")
                .AssertContainsJsonValue("Tag", TestTag)
                .AssertContainsJsonValue("Width", creativeWidth)
                .AssertContainsJsonValue("Height", creativeHeight);
            var campaign = createCampaignResponse
                .TryDeserializeContentJson()["Creative"]
                as IDictionary<string, object>;
            var creativeEntityId = campaign["ExternalEntityId"];

            // Get the created creative
            var getCreativeUrl =
                "entity/company/{0}/creative/{1}"
                .FormatInvariant(
                    advertiserEntityId,
                    creativeEntityId);
            this.RestClient.SendRequest(HttpMethod.GET, getCreativeUrl)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Creative")
                .AssertContainsJsonValue("EntityCategory", "Creative")
                .AssertContainsJsonValue("ExternalName", creativeName)
                .AssertContainsJsonValue("Properties")
                .AssertContainsJsonValue("Tag", TestTag)
                .AssertContainsJsonValue("Width", creativeWidth)
                .AssertContainsJsonValue("Height", creativeHeight);
        }

        /// <summary>Test creating and associating a creative to a campaign</summary>
        [TestMethod]
        public void AssociateCreativeToCampaign()
        {
            // Create an advertiser and campaign
            var companyName = Guid.NewGuid().ToString("n");
            var advertiserEntityId = this.CreateCompany(companyName, "Advertiser");
            var campaignName = Guid.NewGuid().ToString("n");
            var campaignEntityId = this.CreateCampaign(campaignName, "DynamicAllocationCampaign", advertiserEntityId);

            // Create the creative
            var creativeName = Guid.NewGuid().ToString("n");
            var creativeWidth = 500m;
            var creativeHeight = 50m;
            var newCreative = new Dictionary<string, object>
                {
                    { "EntityCategory", "Creative" },
                    { "ExternalName", creativeName },
                    { "ExternalType", "AdThirdParty" },
                    {
                        "Properties",
                        new Dictionary<string, object>
                        {
                            { "Tag", TestTag },
                            { "Width", creativeWidth },
                            { "Height", creativeHeight }
                        }
                    }
                };
            var creativeJson = JsonConvert.SerializeObject(newCreative);
            var createCampaignUrl =
                "entity/company/{0}/creative"
                .FormatInvariant(advertiserEntityId);
            var createCampaignResponse =
                this.RestClient.SendRequest(HttpMethod.POST, createCampaignUrl, creativeJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson()
                .AssertContainsJsonValue("Creative");
            var campaign = createCampaignResponse
                .TryDeserializeContentJson()["Creative"]
                as IDictionary<string, object>;
            var creativeEntityId = campaign["ExternalEntityId"];

            // Add the creative to the campaign
            var creativeAssociationLabel = "CreativeA";
            var addCreativeUrl =
                "entity/company/{0}/campaign/{1}?Message=AddCreative"
                .FormatInvariant(
                    advertiserEntityId,
                    campaignEntityId);
            var addCreativeJson =
                @"{{ ""AssociationName"":""{0}"", ""ParentEntity"":""{1}"", ""ChildEntity"":""{2}"" }}"
                .FormatInvariant(
                    creativeAssociationLabel,
                    campaignEntityId,
                    creativeEntityId);

            var addCreativeResponse =
                this.RestClient.SendRequest(HttpMethod.POST, addCreativeUrl, addCreativeJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson();

            // Allow time for the associate to process
            Thread.Sleep(5000);

            // Get the campaign's creatives
            var getCreativesUrl =
                "entity/company/{0}/campaign/{1}?Flags=Creatives"
                .FormatInvariant(
                    advertiserEntityId,
                    campaignEntityId);
            this.RestClient.SendRequest(HttpMethod.GET, getCreativesUrl)
                .AssertContainsJsonValue("Creatives")
                .AssertContainsJsonValue("EntityCategory", "Creative")
                .AssertContainsJsonValue("ExternalName", creativeName)
                .AssertContainsJsonValue("Properties")
                .AssertContainsJsonValue("Tag", TestTag)
                .AssertContainsJsonValue("Width", creativeWidth)
                .AssertContainsJsonValue("Height", creativeHeight);

            // Remove the creative from the campaign
            var removeCreativeUrl =
                "entity/company/{0}/campaign/{1}?Message=RemoveCreative"
                .FormatInvariant(
                    advertiserEntityId,
                    campaignEntityId);
            var removeCreativeJson =
                @"{{ ""AssociationName"":""{0}"", ""ParentEntity"":""{1}"", ""ChildEntity"":""{2}"" }}"
                .FormatInvariant(
                    creativeAssociationLabel,
                    campaignEntityId,
                    creativeEntityId);

            var removeCreativeResponse =
                this.RestClient.SendRequest(HttpMethod.POST, removeCreativeUrl, removeCreativeJson)
                .AssertIsValidRedirect()
                .Sleep(FollowRedirectWait)
                .FollowIfRedirect(this.RestClient)
                .AssertStatusCode(HttpStatusCode.OK)
                .AssertContentIsJson();

            // Wait for the remove to process
            Thread.Sleep(5000);

            // Get the campaign's creatives
            var creativeResponse = this.RestClient.SendRequest(HttpMethod.GET, getCreativesUrl)
                .AssertContainsJsonValue("Creatives")
                .TryDeserializeContentJson();
            Assert.IsNotNull(creativeResponse);
            var creatives = creativeResponse["Creatives"] as object[];
            Assert.IsNotNull(creatives);
            Assert.AreEqual(0, creatives.Length);
        }
    }
}
