//-----------------------------------------------------------------------
// <copyright file="RequestCampaignReportFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using Activities;
using AppNexusActivities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesUnitTests
{
    /// <summary>Tests for RequestCampaignReportActivity</summary>
    [TestClass]
    public class RequestCampaignReportFixture
    {
        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>Mock AppNexus client for testing</summary>
        private IAppNexusApiClient mockAppNexusClient;

        /// <summary>
        /// The last request submitted via the test SubmitActivityRequestHandler
        /// </summary>
        private ActivityRequest submittedRequest;

        /// <summary>Company for testing</summary>
        private CompanyEntity testCompany;

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity testCampaign;

        /// <summary>EntityId for the test company</summary>
        private EntityId testCompanyEntityId;

        /// <summary>EntityId for the test campaign</summary>
        private EntityId testCampaignEntityId;

        /// <summary>AppNexus id for the test campaign's line-item</summary>
        private int testLineItemId;

        /// <summary>Report id for the mock AppNexus response</summary>
        private string testReportId;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["AppNexus.PostEndDateReportPeriod"] = "1.00:00:00";
            ConfigurationManager.AppSettings["Delivery.ReportFrequency"] = "06:00:00";

            SimulatedPersistentDictionaryFactory.Initialize();
            LogManager.Initialize(new[] { new TestLogger() });
            
            this.CreateTestEntities();

            this.mockAppNexusClient = MockRepository.GenerateMock<IAppNexusApiClient>();
            this.mockAppNexusClient.Stub(f =>
                f.RequestDeliveryReport(
                    Arg<int>.Is.Anything,
                    Arg<int>.Is.Anything))
                .Return(this.testReportId = Guid.NewGuid().ToString("N"));

            var mockClientFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockClientFactory.Stub(f => f.ClientType).Return(typeof(IAppNexusApiClient));
            mockClientFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything)).Return(this.mockAppNexusClient);
            DeliveryNetworkClientFactory.Initialize(new[] { mockClientFactory });

            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCompanyEntityId, this.testCompany, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.mockRepository, this.testCampaignEntityId, this.testCampaign, false);
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
        public void RequestReport()
        {
            var request = new ActivityRequest
            {
                Task = "APNXRequestCampaignReport",
                Values =
                {
                    { "AuthUserId", "6Az3F8$9BA274Cf0!8gE/q98w13oB6u3==" },
                    { "CompanyEntityId", this.testCompanyEntityId.ToString() },
                    { "CampaignEntityId", this.testCampaignEntityId.ToString() }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            
            this.mockAppNexusClient.AssertWasCalled(f => f.RequestDeliveryReport(Arg<int>.Is.Anything, Arg<int>.Is.Anything));

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey("CampaignEntityId"));
            Assert.AreEqual(this.testCampaignEntityId.ToString(), result.Values["CampaignEntityId"]);
            Assert.IsTrue(result.Values.ContainsKey("CompanyEntityId"));
            Assert.AreEqual(this.testCompanyEntityId.ToString(), result.Values["CompanyEntityId"]);
            Assert.IsTrue(result.Values.ContainsKey("LineItemId"));
            Assert.AreEqual(this.testLineItemId.ToString(), result.Values["LineItemId"]);
            Assert.IsTrue(result.Values.ContainsKey("ReportId"));
            Assert.AreEqual(this.testReportId, result.Values["ReportId"]);
            Assert.IsTrue(result.Values.ContainsKey("Reschedule"));
            Assert.AreEqual(true.ToString(), result.Values["Reschedule"]);
        }

        /// <summary>Test exporting a creative</summary>
        [TestMethod]
        public void RequestReportNoReschedule()
        {
            this.testCampaign.EndDate = DateTime.UtcNow.AddDays(-5);

            var request = new ActivityRequest
            {
                Task = "APNXRequestCampaignReport",
                Values =
                {
                    { "AuthUserId", "6Az3F8$9BA274Cf0!8gE/q98w13oB6u3==" },
                    { "CompanyEntityId", this.testCompanyEntityId.ToString() },
                    { "CampaignEntityId", this.testCampaignEntityId.ToString() }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            
            this.mockAppNexusClient.AssertWasCalled(f => f.RequestDeliveryReport(Arg<int>.Is.Anything, Arg<int>.Is.Anything));

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey("CampaignEntityId"));
            Assert.AreEqual(this.testCampaignEntityId.ToString(), result.Values["CampaignEntityId"]);
            Assert.IsTrue(result.Values.ContainsKey("CompanyEntityId"));
            Assert.AreEqual(this.testCompanyEntityId.ToString(), result.Values["CompanyEntityId"]);
            Assert.IsTrue(result.Values.ContainsKey("LineItemId"));
            Assert.AreEqual(this.testLineItemId.ToString(), result.Values["LineItemId"]);
            Assert.IsTrue(result.Values.ContainsKey("ReportId"));
            Assert.AreEqual(this.testReportId, result.Values["ReportId"]);

            Assert.IsTrue(result.Values.ContainsKey("Reschedule"));
            Assert.AreEqual(false.ToString(), result.Values["Reschedule"]);
        }

        /// <summary>
        /// Creates an instance of the DeleteLineItemActivity activity
        /// </summary>
        /// <returns>The activity instance</returns>
        private RequestCampaignReportActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository }
            };

            return Activity.CreateActivity(
                typeof(RequestCampaignReportActivity),
                context,
                this.SubmitActivityRequest)
                as RequestCampaignReportActivity;
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
            Random random = new Random();

            this.testCompany = EntityTestHelpers.CreateTestCompanyEntity(
                (this.testCompanyEntityId = new EntityId()).ToString(),
                "Test Company");
            this.testCompany.SetAppNexusAdvertiserId(random.Next());

            this.testCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                (this.testCampaignEntityId = new EntityId()).ToString(),
                "Test Campaign",
                20000,
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(5),
                "???");
            this.testCampaign.SetAppNexusLineItemId(this.testLineItemId = random.Next());
        }
    }
}
