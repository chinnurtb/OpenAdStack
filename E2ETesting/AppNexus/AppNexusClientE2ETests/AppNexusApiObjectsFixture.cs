//-----------------------------------------------------------------------
// <copyright file="AppNexusApiObjectsFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Globalization;
using System.Linq;
using AppNexusClient;
using AppNexusTestUtilities;
using ConfigManager;
using Diagnostics;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Net;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusClientE2ETests
{
    /// <summary>Test the AppNexus object management APIs</summary>
    [TestClass]
    public class AppNexusApiObjectsFixture
    {
        /// <summary>Test 3rd party ad tag</summary>
        private const string TestAdTag = @"
<a href=""${CLICK_URL}http://comicsdungeon.com/DCDigitalStore.aspx?${CACHEBUSTER}"" TARGET=""_blank"">
<img src=""http://comicsdungeon.com/images/dcdigitalcdi.jpg"" border=""0"" width=""300"" height=""250"" alt=""Advertisement - Comics Dungeon Digital DC Comics"" /></a>";

        /// <summary>Random number generator</summary>
        private static readonly Random R = new Random();

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void Initialize()
        {
            LogManager.Initialize(new[] { new TraceLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            AppNexusClientHelper.InitializeDeliveryNetworkClientFactory();
        }

        /// <summary>Per-test cleanup</summary>
        [TestCleanup]
        public void Cleanup()
        {
            AppNexusClientHelper.Cleanup();
        }
        
        /// <summary>Test creating and deleting objects</summary>
        [TestMethod]
        public void CreateObjects()
        {
            // TODO: Break into individual tests?
            using (var client = this.CreateClient())
            {
                // Get the account member and its id
                var member = client.GetMember();
                Assert.IsNotNull(member);
                Assert.IsTrue(member.ContainsKey(AppNexusValues.Id));
                int memberId = (int)member[AppNexusValues.Id];

                // Get the segments for the member
                var segments = client.GetMemberSegments(0, 3);
                Assert.IsNotNull(segments);

                // Create advertiser
                var advertiserCode = Guid.NewGuid().ToString("N");
                var advertiserId = client.CreateAdvertiser(
                    "Test Company - " + Guid.NewGuid().ToString(),
                    advertiserCode);
                Assert.IsNotNull(advertiserId);
                AppNexusClientHelper.AddAdvertiserForCleanup(advertiserId);

                // Get advertiser
                var advertiser = client.GetAdvertiserByCode(advertiserCode);
                Assert.IsNotNull(advertiser);
                Assert.AreEqual(advertiserId, (int)advertiser[AppNexusValues.Id]);

                // Create test segments
                var segmentIdA = client.CreateSegment(
                    memberId,
                    "Test Segment - {0}".FormatInvariant(DateTime.UtcNow.ToString("o")),
                    Guid.NewGuid().ToString("N"));
                var segmentIdB = client.CreateSegment(
                    memberId,
                    "Test Segment - {0}".FormatInvariant(DateTime.UtcNow.ToString("o")),
                    Guid.NewGuid().ToString("N"));

                // Create include domain list
                var includeDomainListId = client.CreateDomainList(
                    "Test Domain List " + DateTime.UtcNow.ToString("o"),
                    string.Empty,
                    new[] { "msnbc.com", "cnn.com", "bbc.co.uk" });
                AppNexusClientHelper.AddDomainListForCleanup(includeDomainListId);

                // Create line item and profile
                var frequencyCaps = new Dictionary<AppNexusFrequencyType, int>
                {
                    { AppNexusFrequencyType.Lifetime, 10 },
                };

                var lineItemCode = Guid.NewGuid().ToString("N");
                var lineItemProfileId = client.CreateLineItemProfile(
                    advertiserId,
                    lineItemCode,
                    frequencyCaps,
                    includeDomainListId,
                    new[] { "rarecrowds.com" });
                var lineItemId = client.CreateLineItem(
                    advertiserId,
                    lineItemProfileId,
                    "Test Line Item " + DateTime.UtcNow.ToString("o"),
                    lineItemCode,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow + new TimeSpan(1, 0, 0),
                    20m);
                Assert.IsNotNull(lineItemId);

                // Get line item
                var lineItem = client.GetLineItemByCode(advertiserCode, lineItemCode);
                Assert.IsNotNull(lineItem);
                Assert.AreEqual(lineItemId, (int)lineItem[AppNexusValues.Id]);

                // Update line item
                client.UpdateLineItem(
                    lineItemId,
                    advertiserId,
                    "Test Line Item " + DateTime.UtcNow.ToString("o"),
                    lineItemCode,
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow + new TimeSpan(1, 0, 0),
                    40m);

                // Get line item
                var updatedLineItem = client.GetLineItemByCode(advertiserCode, lineItemCode);
                Assert.IsNotNull(updatedLineItem);
                Assert.AreEqual(lineItemId, (int)updatedLineItem[AppNexusValues.Id]);
                Assert.AreEqual(40m, (int)updatedLineItem[AppNexusValues.LifetimeBudget]);

                // Create targeting profile with no age, no gender, segments, dma,
                // no region, content categories (not allow unknown) and include domain list
                var contentCategories = client.GetContentCategories()
                    .Select(c => Convert.ToInt32(c[AppNexusValues.Id]))
                    .Take(2)
                    .ToArray();
                var profileCode = Guid.NewGuid().ToString("N");
                var profileId = client.CreateCampaignProfile(
                    advertiserId,
                    profileCode,
                    false,
                    null,
                    null,
                    new Dictionary<int, string>
                    {
                        { segmentIdA, "SegmentA" },
                        { segmentIdB, "SegmentB" }
                    },
                    new Dictionary<int, string>
                    {
                        { 635, "Austin TX" },
                        { 644, "Alexandria LA" }
                    },
                    new string[0],
                    PageLocation.Above,
                    new int[0],
                    new Dictionary<int, bool>
                    {
                        { contentCategories[0], true },
                        { contentCategories[1], false }
                    },
                    new Dictionary<int, bool>
                    {
                        { includeDomainListId, true }
                    });

                // Get targeting profile
                var profile = client.GetProfileByCode(advertiserId, profileCode);
                Assert.IsNotNull(profile);
                Assert.AreEqual(profileId, (int)profile[AppNexusValues.Id]);

                // Create targeting profile with allow unknown age, gender, segments, region, no dma, no content categories and exclude domain list
                profileCode = Guid.NewGuid().ToString("N");
                profileId = client.CreateCampaignProfile(
                    advertiserId,
                    profileCode,
                    true,
                    null,
                    "m",
                    new Dictionary<int, string>
                    {
                        { segmentIdA, "SegmentA" },
                        { segmentIdB, "SegmentB" }
                    },
                    new Dictionary<int, string>(0),
                    new string[] { "US:TX" },
                    PageLocation.Any,
                    new[] { 2, 4, 8, 14 },
                    new Dictionary<int, bool>(0),
                    new Dictionary<int, bool>
                    {
                        { includeDomainListId, false }
                    });

                // Get targeting profile
                profile = client.GetProfileByCode(advertiserId, profileCode);
                Assert.IsNotNull(profile);
                Assert.AreEqual(profileId, (int)profile[AppNexusValues.Id]);

                // Create targeting profile with age 25-34, no gender, segments, dma, region,
                // content categories (including unknown) and no domain list targets
                profileCode = Guid.NewGuid().ToString("N");
                profileId = client.CreateCampaignProfile(
                    advertiserId,
                    profileCode,
                    false,
                    new Tuple<int, int>(25, 34),
                    null,
                    new Dictionary<int, string>
                    {
                        { segmentIdA, "SegmentA" },
                        { segmentIdB, "SegmentB" }
                    },
                    new Dictionary<int, string>
                    {
                        { 635, "Austin TX" },
                        { 644, "Alexandria LA" }
                    },
                    new[] { "US:TX" },
                    PageLocation.Below,
                    new[] { 4, 2, 6 },
                    new Dictionary<int, bool>
                    {
                        { 0, true },
                        { contentCategories[0], true },
                        { contentCategories[1], false }
                    },
                    null);

                // Get targeting profile
                profile = client.GetProfileByCode(advertiserId, profileCode);
                Assert.IsNotNull(profile);
                Assert.AreEqual(profileId, (int)profile[AppNexusValues.Id]);

                // Create creative
                var creativeId = client.CreateCreative(
                    advertiserId,
                    "Test creative " + DateTime.UtcNow.ToString("o"),
                    Guid.NewGuid().ToString("N"),
                    7,
                    768,
                    90,
                    AppNexusApiClient.JsonEscape(TestAdTag));

                // Create campaign
                var campaignCode = Guid.NewGuid().ToString("N");
                var campaignId = client.CreateCampaign(
                    advertiserId,
                    "Test campaign " + DateTime.UtcNow.ToString("o"),
                    campaignCode,
                    lineItemId,
                    profileId,
                    new[] { creativeId },
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow + new TimeSpan(1, 0, 0),
                    10m,
                    200,
                    1.25m);

                // Get campaign
                var campaign = client.GetCampaignByCode(advertiserId, campaignCode);
                Assert.IsNotNull(campaign);
                Assert.AreEqual(campaignId, (int)campaign[AppNexusValues.Id]);

                // Update campaign
                client.UpdateCampaign(
                    campaignCode,
                    advertiserId,
                    "Test campaign " + DateTime.UtcNow.ToString("o"),
                    new[] { creativeId },
                    true,
                    DateTime.UtcNow,
                    DateTime.UtcNow + new TimeSpan(1, 0, 0),
                    15m,
                    200,
                    1.25m);

                // Get updated campaign
                var updatedCampaign = client.GetCampaignByCode(advertiserId, campaignCode);
                Assert.IsNotNull(updatedCampaign);
                Assert.AreEqual(campaignId, (int)updatedCampaign[AppNexusValues.Id]);
                Assert.AreEqual(15m, (int)updatedCampaign[AppNexusValues.LifetimeBudget]);
            }
        }

        /// <summary>Smoke test creating various creative types</summary>
        [TestMethod]
        public void CreateCreatives()
        {
            var client = this.CreateClient();

            // Create advertiser
            var advertiserCode = Guid.NewGuid().ToString("N");
            var advertiserId = client.CreateAdvertiser(
                "Test Company - " + Guid.NewGuid().ToString(),
                advertiserCode);
            Assert.IsNotNull(advertiserId);
            AppNexusClientHelper.AddAdvertiserForCleanup(advertiserId);

            // Create ad tag creative
            var adTagcreativeId = client.CreateCreative(
                advertiserId,
                "Test 3rd Party Tag Creative " + DateTime.UtcNow.ToString("o"),
                Guid.NewGuid().ToString("N"),
                7,
                768,
                90,
                TestAdTag);
            Assert.IsNotNull(adTagcreativeId);

            // Create image ad creative
            var imageBytes = new byte[102400];
            R.NextBytes(imageBytes);
            var imageCreativeId = client.CreateCreative(
                advertiserId,
                "Test Image Creative " + DateTime.UtcNow.ToString("o"),
                Guid.NewGuid().ToString("N"),
                7,
                768,
                90,
                Convert.ToBase64String(imageBytes),
                "TestImage.jpg",
                "http://www.example.com/");
            Assert.IsNotNull(imageCreativeId);

            // Create image ad creative
            var flashBytes = new byte[512000];
            R.NextBytes(flashBytes);
            var flashCreativeId = client.CreateCreative(
                advertiserId,
                "Test Flash Creative " + DateTime.UtcNow.ToString("o"),
                Guid.NewGuid().ToString("N"),
                7,
                768,
                90,
                Convert.ToBase64String(flashBytes),
                "TestFlash.swf",
                "http://www.example.com/",
                Convert.ToBase64String(imageBytes),
                "TestBackup.gif",
                "FlashAdClicked");
            Assert.IsNotNull(flashCreativeId);
        }

        /// <summary>
        /// Creates an instance of the AppNexus API client for testing
        /// </summary>
        /// <returns>The client</returns>
        private IAppNexusApiClient CreateClient()
        {
            var client = new AppNexusApiClient();
            Assert.IsNotNull(client);
            return client;
        }
    }
}
