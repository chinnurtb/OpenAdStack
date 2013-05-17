//-----------------------------------------------------------------------
// <copyright file="DeleteLineItemFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
    /// <summary>Tests for DeleteLineItemActivity</summary>
    [TestClass]
    public class DeleteLineItemFixture
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

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            LogManager.Initialize(new[] { new TestLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();

            this.mockAppNexusClient = MockRepository.GenerateMock<IAppNexusApiClient>();

            var mockClientFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockClientFactory.Stub(f => f.ClientType).Return(typeof(IAppNexusApiClient));
            mockClientFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything)).Return(this.mockAppNexusClient);
            DeliveryNetworkClientFactory.Initialize(new[] { mockClientFactory });
                
            this.CreateTestEntities();
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

        /// <summary>Test deleting a line-item</summary>
        [TestMethod]
        public void DeleteLineItem()
        {
            var request = new ActivityRequest
            {
                Task = "APNXExportCreative",
                Values =
                {
                    { "AuthUserId", "6Az3F8$9BA274Cf0!8gE/q98w13oB6u3==" },
                    { "CompanyEntityId", this.testCompanyEntityId.ToString() },
                    { "CampaignEntityId", this.testCampaignEntityId.ToString() }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);

            this.mockAppNexusClient.AssertWasCalled(f => f.DeleteLineItem(Arg<int>.Is.Anything, Arg<int>.Is.Anything));
            
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.Values.ContainsKey("CampaignEntityId"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values["CampaignEntityId"]));
            Assert.IsTrue(result.Values.ContainsKey("LineItemId"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Values["LineItemId"]));
        }

        /// <summary>
        /// Creates an instance of the DeleteLineItemActivity activity
        /// </summary>
        /// <returns>The activity instance</returns>
        private DeleteLineItemActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository }
            };

            return Activity.CreateActivity(
                typeof(DeleteLineItemActivity),
                context,
                this.SubmitActivityRequest)
                as DeleteLineItemActivity;
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
                DateTime.UtcNow.AddDays(-14),
                DateTime.UtcNow.AddDays(-4),
                "???");
            this.testCampaign.SetAppNexusLineItemId(random.Next());
        }
    }
}
