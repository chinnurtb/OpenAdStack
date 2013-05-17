//-----------------------------------------------------------------------
// <copyright file="ExportCreativeActivityFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Activities;
using ActivityTestUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Google.Api.Ads.Common.Util;
using Google.Api.Ads.Dfp.Lib;
using GoogleDfpActivities;
using GoogleDfpClient;
using GoogleDfpUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using TestUtilities;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Tests for ExportCreativeActivity</summary>
    [TestClass]
    public class ExportCreativeActivityFixture : DfpActivityFixtureBase<ExportCreativeActivity>
    {
        /// <summary>Gets the bytes of a 300x250 test GIF</summary>
        private byte[] TestImageBytes
        {
            get { return EmbeddedResourceHelper.GetEmbeddedResourceAsByteArray(this.GetType(), "Resources.test.gif"); }
        }

        /// <summary>Initialize per-test object(s)/settings</summary>
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
        }

        /// <summary>Test exporting an image creative</summary>
        [TestMethod]
        public void ExportImageCreative()
        {
            var companyEntity = TestNetwork.AdvertiserCompanyEntity;
            var creativeEntity = this.CreateTestImageAdCreative();
            this.AddEntitiesToMockRepository(companyEntity, creativeEntity);

            var request = new ActivityRequest
            {
                Task = GoogleDfpActivityTasks.ExportCreative,
                Values =
                {
                    { EntityActivityValues.AuthUserId, Guid.NewGuid().ToString("N") },
                    { EntityActivityValues.CompanyEntityId, companyEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.CreativeEntityId, creativeEntity.ExternalEntityId.ToString() },
                }
            };
            
            var activity = this.CreateActivity();
            var result = activity.Run(request);

            // Validate result
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(
                result,
                EntityActivityValues.CreativeEntityId,
                GoogleDfpActivityValues.CreativeId);
            
            // Verify creative was created correctly in DFP
            long creativeId;
            Assert.IsTrue(long.TryParse(result.Values[GoogleDfpActivityValues.CreativeId], out creativeId));
            var creative = this.DfpClient.GetCreatives(new[] { creativeId }).FirstOrDefault() as Dfp.ImageCreative;
            Assert.IsNotNull(creative);
            Assert.AreEqual(creativeId, creative.id);
            Assert.AreEqual<string>(creativeEntity.ExternalName, creative.name);
            Assert.AreEqual(TestNetwork.AdvertiserId, creative.advertiserId);
            Assert.IsNotNull(creative.previewUrl);
            Assert.IsFalse(creative.size.isAspectRatio);
            Assert.AreEqual(300, creative.size.width);
            Assert.AreEqual(250, creative.size.height);
            Assert.AreEqual(creativeEntity.GetClickUrl(), creative.destinationUrl);
        }

        /// <summary>Test exporting third party tag creative</summary>
        [TestMethod]
        [Ignore]
        public void ExportThirdPartyTagCreative()
        {
            Assert.Fail();
        }

        /// <summary>Creates a test creative entity for a 300x250 image ad</summary>
        /// <returns>The creative entity</returns>
        private CreativeEntity CreateTestImageAdCreative()
        {
            return EntityTestHelpers.CreateTestImageAdCreativeEntity(
                new EntityId(),
                "Test Creative - " + this.UniqueId,
                300,
                250,
                "http://www.rarecrowds.com/",
                this.TestImageBytes);
        }
    }
}
