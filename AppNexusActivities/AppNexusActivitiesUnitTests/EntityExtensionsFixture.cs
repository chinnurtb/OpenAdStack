//-----------------------------------------------------------------------
// <copyright file="EntityExtensionsFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AppNexusActivities;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AppNexusActivitiesUnitTests
{
    /// <summary>
    /// Tests for the entity extensions
    /// </summary>
    [TestClass]
    public class EntityExtensionsFixture
    {
        /// <summary>
        /// Per-test initialization
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["AppNexus.Sandbox"] = "true";
        }

        /// <summary>
        /// Tests getting and setting the AppNexus advertiserId on a CompanyEntity
        /// </summary>
        [TestMethod]
        public void CompanyAppNexusAdvertiserId()
        {
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(new EntityId().ToString(), Guid.NewGuid().ToString());
            var expectedAdvertiserId = new Random().Next();

            Assert.IsNull(companyEntity.GetAppNexusAdvertiserId());
            companyEntity.SetAppNexusAdvertiserId(expectedAdvertiserId);
            var advertiserId = companyEntity.GetAppNexusAdvertiserId();
            Assert.IsNotNull(advertiserId);
            Assert.AreEqual(expectedAdvertiserId, (int)advertiserId);
        }

        /// <summary>
        /// Tests getting and setting the AppNexus lifetime media budget cap on a CampaignEntity
        /// </summary>
        [TestMethod]
        public void CampaignAppNexusLifetimeMediaBudgetCap()
        {
            var random = new Random();
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString(),
                random.Next(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid().ToString());
            var expectedLifetimeMediaBudgetCap = (decimal)(random.NextDouble() * random.Next());

            Assert.IsNull(campaignEntity.GetLifetimeMediaBudgetCap());
            campaignEntity.SetLifetimeMediaBudgetCap(expectedLifetimeMediaBudgetCap);
            Assert.AreEqual(expectedLifetimeMediaBudgetCap, campaignEntity.GetLifetimeMediaBudgetCap());
        }

        /// <summary>
        /// Tests getting and setting the AppNexus lineItemId on a CampaignEntity
        /// </summary>
        [TestMethod]
        public void CampaignAppNexusLineItemId()
        {
            var random = new Random();
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString(),
                random.Next(),
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7),
                Guid.NewGuid().ToString());
            var expectedLineItemId = random.Next();

            Assert.IsNull(campaignEntity.GetAppNexusLineItemId());
            campaignEntity.SetAppNexusLineItemId(expectedLineItemId);
            var lineItemId = campaignEntity.GetAppNexusLineItemId();
            Assert.IsNotNull(lineItemId);
            Assert.AreEqual(expectedLineItemId, (int)lineItemId);
        }

        /// <summary>
        /// Tests getting and setting the AppNexus creativeId on a CreativeEntity
        /// </summary>
        [TestMethod]
        public void CreativeAppNexusCreativeId()
        {
            var random = new Random();
            var creativeEntity = EntityTestHelpers.CreateTestCreativeEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString());
            var expectedCreativeId = new Random().Next();

            Assert.IsNull(creativeEntity.GetAppNexusCreativeId());
            creativeEntity.SetAppNexusCreativeId(expectedCreativeId);
            var creativeId = creativeEntity.GetAppNexusCreativeId();
            Assert.IsNotNull(creativeId);
            Assert.AreEqual(expectedCreativeId, (int)creativeId);
        }

        /// <summary>
        /// Tests getting the creative width and height
        /// </summary>
        [TestMethod]
        public void GetCreativeWidthAndHeight()
        {
            var random = new Random();
            var expectedWidth = random.Next();
            var expectedHeight = random.Next();

            var creativeEntity = EntityTestHelpers.CreateTestCreativeEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                expectedWidth,
                expectedHeight);

            var width = creativeEntity.GetWidth();
            Assert.IsNotNull(width);
            Assert.AreEqual(expectedWidth, width);

            var height = creativeEntity.GetHeight();
            Assert.IsNotNull(height);
            Assert.AreEqual(expectedHeight, height);
        }

        /// <summary>
        /// Tests getting the creative third-party ad tag
        /// </summary>
        [TestMethod]
        public void GetCreativeThirdPartyAdTag()
        {
            const string ExpectedCreativeAdTag = "<a href=\"${CLICK_URL}http://example.com/?${CACHEBUSTER}\" TARGET=\"_blank\"><img src=\"http://example.com/images/ad.jpg\"></a>";
            var creativeEntity = EntityTestHelpers.CreateTestCreativeEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString(),
                AppNexusClient.AppNexusApiClient.JsonEscape(ExpectedCreativeAdTag));

            var creativeAdTag = creativeEntity.GetThirdPartyAdTag();
            Assert.IsNotNull(creativeAdTag);
            Assert.AreEqual(ExpectedCreativeAdTag, creativeAdTag);
        }
    }
}
