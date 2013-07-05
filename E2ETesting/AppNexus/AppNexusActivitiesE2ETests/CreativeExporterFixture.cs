//-----------------------------------------------------------------------
// <copyright file="CreativeExporterFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Activities;
using AppNexusActivities;
using AppNexusClient;
using AppNexusTestUtilities;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesUnitTests
{
    /// <summary>Tests for ExportCreativeActivity</summary>
    [TestClass]
    public class CreativeExporterFixture
    {
        /// <summary>Test 3rd party ad tag</summary>
        private const string TestAdTag = @"
<a href=""${CLICK_URL}http://comicsdungeon.com/DCDigitalStore.aspx?${CACHEBUSTER}"" TARGET=""_blank"">
<img src=""http://comicsdungeon.com/images/dcdigitalcdi.jpg"" border=""0"" width=""300"" height=""250"" alt=""Advertisement - Comics Dungeon Digital DC Comics"" /></a>";

        /// <summary>JSON Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };

        /// <summary>Test logger</summary>
        private TestLogger testLogger;

        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>AppNexus client for test verification</summary>
        private IAppNexusApiClient testClient;

        /// <summary>
        /// The last request submitted via the test SubmitActivityRequestHandler
        /// </summary>
        private ActivityRequest submittedRequest;

        /// <summary>User for testing</summary>
        private UserEntity testUser;

        /// <summary>Company for testing</summary>
        private CompanyEntity testCompany;

        /// <summary>Creative entity for testing</summary>
        private CreativeEntity testCreative;

        /// <summary>UserId for the test user</summary>
        private string testUserId;

        /// <summary>EntityId for the test company</summary>
        private EntityId testCompanyEntityId;

        /// <summary>EntityId for the test creative</summary>
        private EntityId testCreativeEntityId;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            // Setup basic infrastructure
            ConfigurationManager.AppSettings["Delivery.CreativeUpdateFrequency"] = "00:01:00";
            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            ScheduledActivities.Scheduler.Registries = null;

            // Initialize delivery network client factory
            AppNexusClientHelper.InitializeDeliveryNetworkClientFactory();
            this.testClient = DeliveryNetworkClientFactory.CreateClient<IAppNexusApiClient>(new CustomConfig());
            
            // Create entities for testing
            this.CreateTestEntities();

            // Setup mock repository
            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();
            RepositoryStubUtilities.SetupGetUserStub(
                this.mockRepository, this.testUserId, this.testUser, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCompanyEntityId, this.testCompany, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCreativeEntityId, this.testCreative, false);
        }

        /// <summary>Cleanup any AppNexus objects created by the test</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            var advertiserId = this.testCompany.GetAppNexusAdvertiserId();
            if (advertiserId != null)
            {
                AppNexusClientHelper.AddAdvertiserForCleanup(advertiserId.Value);
            }

            AppNexusClientHelper.Cleanup();
        }

        /// <summary>Basic activity create test</summary>
        [TestMethod]
        public void Create()
        {
            var activity = this.CreateActivity();
            Assert.IsNotNull(activity);
        }

        /// <summary>Test exporting a creative</summary>
        [TestMethod]
        public void ExportCreative()
        {
            var request = new ActivityRequest
            {
                Task = "APNXExportCreative",
                Values =
                {
                    { "AuthUserId", this.testUserId },
                    { "CompanyEntityId", this.testCompanyEntityId },
                    { "CreativeEntityId", this.testCreativeEntityId }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey("CreativeId"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values["CreativeId"]));
            
            var creativeId = Convert.ToInt32(result.Values["CreativeId"]);
            var creative = this.testClient.GetCreative(creativeId);
            Assert.IsNotNull(creative);
        }

        /// <summary>Test exporting an image creative</summary>
        [TestMethod]
        public void ExportImageCreative()
        {
            this.testCreative.SetCreativeType(CreativeType.ImageAd);
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ImageName,
                "TestCreative-{0}.jpg".FormatInvariant(Guid.NewGuid().ToString("N")));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ImageBytes,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(TestAdTag)));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ClickUrl,
                "http://www.example.com/");

            var request = new ActivityRequest
            {
                Task = "APNXExportCreative",
                Values =
                {
                    { "AuthUserId", this.testUserId },
                    { "CompanyEntityId", this.testCompanyEntityId },
                    { "CreativeEntityId", this.testCreativeEntityId }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey("CreativeId"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values["CreativeId"]));

            var creativeId = Convert.ToInt32(result.Values["CreativeId"]);
            var creative = this.testClient.GetCreative(creativeId);
            Assert.IsNotNull(creative);
        }

        /// <summary>Test exporting a flash creative</summary>
        [TestMethod]
        public void ExportFlashCreative()
        {
            this.testCreative.SetCreativeType(CreativeType.FlashAd);
            
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.FlashBytes,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(TestAdTag)));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.FlashName,
                "TestCreative-{0}.swf".FormatInvariant(Guid.NewGuid().ToString("N")));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ImageBytes,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(TestAdTag)));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ImageName,
                "BackupCreative-{0}.jpg".FormatInvariant(Guid.NewGuid().ToString("N")));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.FlashClickVariable,
                "FlashAdClicked");
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ClickUrl,
                "http://www.example.com/");

            var request = new ActivityRequest
            {
                Task = "APNXExportCreative",
                Values =
                {
                    { "AuthUserId", this.testUserId },
                    { "CompanyEntityId", this.testCompanyEntityId },
                    { "CreativeEntityId", this.testCreativeEntityId }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey("CreativeId"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values["CreativeId"]));

            var creativeId = Convert.ToInt32(result.Values["CreativeId"]);
            var creative = this.testClient.GetCreative(creativeId);
            Assert.IsNotNull(creative);
        }

        /// <summary>
        /// Test looking up format ids for all supported formats
        /// </summary>
        [TestMethod]
        public void LookupSupportedCreativeFormatIds()
        {
            var exporter = new AppNexusCreativeExporter(0, this.testCompany, this.testCreative, this.testUser);
            foreach (var creativeType in AppNexusCreativeExporter.SupportedTypes)
            {
                try
                {
                    var formatId = exporter.LookupAppNexusCreativeFormatId(creativeType);
                    Assert.AreNotEqual(0, formatId);
                }
                catch (Exception e)
                {
                    Assert.Fail(
                        "Failed to lookup creative format id for creative type '{0}'\n{1}",
                        creativeType,
                        e);
                }
            }
        }

        /// <summary>
        /// Test looking up standard templates for all supported formats
        /// </summary>
        [TestMethod]
        public void LookupSupportedCreativeStandardTemplateIds()
        {
            var exporter = new AppNexusCreativeExporter(0, this.testCompany, this.testCreative, this.testUser);
            var formatIds = AppNexusCreativeExporter.SupportedTypes
                .Select(creativeType => exporter.LookupAppNexusCreativeFormatId(creativeType));

            foreach (var formatId in formatIds)
            {
                try
                {
                    var templateId = exporter.LookupAppNexusStandardTemplateId(formatId);
                    Assert.AreNotEqual(0, templateId);
                }
                catch (Exception e)
                {
                    Assert.Fail(
                        "Failed to lookup creative standard template id for creative format id '{0}'\n{1}",
                        formatId,
                        e);
                }
            }
        }

        /// <summary>
        /// Creates an instance of the ExportDynamicAllocationCreative activity
        /// </summary>
        /// <returns>The activity instance</returns>
        private ExportCreativeActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository }
            };

            return Activity.CreateActivity(typeof(ExportCreativeActivity), context, this.SubmitActivityRequest) as ExportCreativeActivity;
        }

        /// <summary>Test submit activity request handler</summary>
        /// <param name="request">The request</param>
        /// <param name="sourceName">The source name</param>
        /// <returns>True if successful; otherwise, false.</returns>
        private bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            this.submittedRequest = request;
            return true;
        }

        /// <summary>Create the test entities</summary>
        private void CreateTestEntities()
        {
            this.testUser = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(),
                (this.testUserId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")))),
                "nobody@rc.dev");
            this.testUser.SetUserType(UserType.StandAlone);

            this.testCompany = EntityTestHelpers.CreateTestCompanyEntity(
                (this.testCompanyEntityId = new EntityId()).ToString(),
                "Test Company - {0}".FormatInvariant(DateTime.UtcNow));

            this.testCreative = EntityTestHelpers.CreateTestCreativeEntity(
                (this.testCreativeEntityId = new EntityId()).ToString(),
                "Test Creative - {0}".FormatInvariant(DateTime.UtcNow),
                AppNexusApiClient.JsonEscape(TestAdTag),
                768,
                90);
            this.testCreative.SetOwnerId(this.testUser.UserId);
            this.testCreative.SetCreativeType(CreativeType.ThirdPartyAd);
        }
    }
}