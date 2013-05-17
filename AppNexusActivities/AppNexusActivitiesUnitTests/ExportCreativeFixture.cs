//-----------------------------------------------------------------------
// <copyright file="ExportCreativeFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
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
    public class ExportCreativeFixture
    {
        /// <summary>Test 3rd party ad tag</summary>
        private const string TestAdTag = @"
<a href=""${CLICK_URL}http://comicsdungeon.com/DCDigitalStore.aspx?${CACHEBUSTER}"" TARGET=""_blank"">
<img src=""http://comicsdungeon.com/images/dcdigitalcdi.jpg"" border=""0"" width=""300"" height=""250"" alt=""Advertisement - Comics Dungeon Digital DC Comics"" /></a>";

        /// <summary>JSON Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };

        /// <summary>Mock logger for testing</summary>
        private ILogger mockLogger;

        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>Mock AppNexus client for testing</summary>
        private IAppNexusApiClient mockAppNexusClient;

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
            ConfigurationManager.AppSettings["Delivery.CreativeUpdateFrequency"] = "00:01:00";
            SimulatedPersistentDictionaryFactory.Initialize();
            ScheduledActivities.Scheduler.Registries = null;

            this.mockLogger = MockRepository.GenerateMock<ILogger>();
            LogManager.Initialize(new[] { this.mockLogger });

            this.CreateTestEntities();

            this.mockAppNexusClient = MockRepository.GenerateMock<IAppNexusApiClient>();

            var mockClientFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockClientFactory.Stub(f => f.ClientType).Return(typeof(IAppNexusApiClient));
            mockClientFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything)).Return(this.mockAppNexusClient);
            DeliveryNetworkClientFactory.Initialize(new[] { mockClientFactory });

            // TODO: Add some validations to these stubs
            var rand = new Random();
            this.mockAppNexusClient.Stub(f =>
                f.CreateAdvertiser(Arg<string>.Is.Anything, Arg<string>.Is.Anything))
                .Return(rand.Next());

            this.mockAppNexusClient.Stub(f =>
                f.CreateCreative(
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything))
                .Return(rand.Next());
            var creativeFormats = JsonSerializer.Deserialize<IDictionary<string, object>[]>(
                EmbeddedResourceHelper.GetEmbeddedResourceAsString(this.GetType(), "Resources.creative-formats.js"));
            this.mockAppNexusClient.Stub(f =>
                f.GetCreativeFormats())
                .Return(creativeFormats);

            var creativeTemplates = JsonSerializer.Deserialize<IDictionary<string, object>[]>(
                    EmbeddedResourceHelper.GetEmbeddedResourceAsString(this.GetType(), "Resources.creative-templates.js"));
            this.mockAppNexusClient.Stub(f =>
                f.GetCreativeTemplates())
                .Return(creativeTemplates);

            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();

            RepositoryStubUtilities.SetupGetUserStub(
                this.mockRepository, this.testUserId, this.testUser, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCompanyEntityId, this.testCompany, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCreativeEntityId, this.testCreative, false);
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

            this.AssertCreateCreativeWasCalled();
        }

        /// <summary>Test exporting an image creative</summary>
        [TestMethod]
        public void ExportImageCreative()
        {
            this.testCreative.SetCreativeType(CreativeType.ImageAd);
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

            this.AssertCreateCreativeWasCalled();
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
                DeliveryNetworkEntityProperties.Creative.ImageBytes,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(TestAdTag)));
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.ClickUrl,
                "http://www.example.com/");
            this.testCreative.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.Creative.FlashClickVariable,
                "FlashAdClicked");

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

            this.AssertCreateCreativeWasCalled();
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
                "Test Company");

            this.testCreative = EntityTestHelpers.CreateTestCreativeEntity(
                (this.testCreativeEntityId = new EntityId()).ToString(),
                "Test Creative",
                AppNexusApiClient.JsonEscape(TestAdTag),
                768,
                90);
            this.testCreative.SetOwnerId(this.testUser.UserId);
            this.testCreative.SetCreativeType(CreativeType.ThirdPartyAd);
        }

        /// <summary>Assert the IAppNexusApiClient.CreateCreative stub was called</summary>
        private void AssertCreateCreativeWasCalled()
        {
            this.mockAppNexusClient.AssertWasCalled(f =>
                f.CreateCreative(
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything));
        }
    }
}