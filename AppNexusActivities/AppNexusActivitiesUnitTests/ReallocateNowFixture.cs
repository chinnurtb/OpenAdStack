//-----------------------------------------------------------------------
// <copyright file="ReallocateNowFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Activities;
using AppNexusActivities;
using AppNexusUtilities;
using DataAccessLayer;
using Diagnostics;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusActivitiesUnitTests
{
    using Rhino.Mocks.Interfaces;

    /// <summary>
    /// Tests for the ExportCreativeFixture activity
    /// </summary>
    [TestClass]
    public class ReallocateNowFixture
    {
        /// <summary>
        /// Tracking dictionary
        /// </summary>
        private IPersistentDictionary<ReallocateNowActivity.TrackingInfo> trackingDictionary;

        /// <summary>Mock logger for testing</summary>
        private ILogger mockLogger;

        /// <summary>Mock entity repository for testing</summary>
        private IEntityRepository mockRepository;

        /// <summary>
        /// The last request submitted via the test SubmitActivityRequestHandler
        /// </summary>
        private ActivityRequest submittedRequest;

        /// <summary>Company for testing</summary>
        private CompanyEntity testCompany;

        /// <summary>
        /// Campaign for testing
        /// </summary>
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
            ConfigurationManager.AppSettings["Activities.SubmitRequestRetries"] = "1";
            ConfigurationManager.AppSettings["AppNexus.RetrieveReportRetryTime"] = "00:00:01";
            ConfigurationManager.AppSettings["System.AuthUserId"] = "1AD0DD27B0DE4605B8DA6F5C6C26A9E8";
            ConfigurationManager.AppSettings["Activities.SubmitRequestRetryWait"] = "10";

            SimulatedPersistentDictionaryFactory.Initialize();

            this.CreateTestEntities();

            this.mockLogger = MockRepository.GenerateMock<ILogger>();
            LogManager.Initialize(new[] { this.mockLogger });

            this.mockRepository = MockRepository.GenerateMock<IEntityRepository>();

            this.mockRepository.Stub(
                f => f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything)).Return(
                    new HashSet<IEntity>(
                        new[]
                            {
                                this.testCampaign
                            }));
        }

        /// <summary>Basic activity create test</summary>
        [TestMethod]
        public void Create()
        {
            var activity = this.CreateActivity();
            Assert.IsNotNull(activity);
        }

        /// <summary>Test Reallocate Now activity</summary>
        [TestMethod]
        public void RunReallocateNowActivity()
        {
            var request = new ActivityRequest
            {
                Task = "APNXReallocateNow",
                Values =
                        {
                            { "AuthUserId", "6Az3F8$9BA274Cf0!8gE/q98w13oB6u3==" },
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId }
                        }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);
        }

        /// <summary>Test Reallocate Now activity with failed submit</summary>
        [TestMethod]
        public void RunReallocateNowActivityFailSubmit()
        {
            var request = new ActivityRequest
            {
                Task = "APNXReallocateNow",
                Values =
                        {
                            { "AuthUserId", "6Az3F8$9BA274Cf0!8gE/q98w13oB6u3==" },
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId }
                        }
            };

            var activity = this.CreateActivityFailSubmit();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
        }

        /// <summary>Test Reallocate Now activity without a line id</summary>
        [TestMethod]
        public void RunReallocateNowActivityNoLineId()
        {
            this.testCampaign.SetPropertyValueByName(AppNexusEntityProperties.LineItemId, null);
            var request = new ActivityRequest
            {
                Task = "APNXReallocateNow",
                Values =
                        {
                            { "AuthUserId", "6Az3F8$9BA274Cf0!8gE/q98w13oB6u3==" },
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId }
                        }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
        }

        /// <summary>
        /// Test OnActivityResult for RequestReportActivity, RetrieveReportActivity and DAGetBudgetAllocations
        /// </summary>
        [TestMethod]
        public void RequestReportActivityActivityResult()
        {
            var requestId = Guid.NewGuid().ToString("N");
            var request = new ActivityResult
            {
                Task = "APNXRequestCampaignReport",
                Succeeded = true,
                RequestId = requestId,
                Values =
                        {
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId },
                            { "LineItemId", "Test line id" },
                            { "ReportId", "TestReportID" }
                        }
            };

            var activity = this.CreateActivity();
            this.trackingDictionary = activity.Tracking;
            var trackingInfo = new ReallocateNowActivity.TrackingInfo(this.testCampaignEntityId, "TestCampaignName", 12345);
            trackingInfo.WorkItemDictionary[requestId] = "{0}|{1}".FormatInvariant(request.Task, "Submitted");
            this.trackingDictionary[this.testCampaign.ExternalEntityId.ToString()] = trackingInfo;
            activity.OnActivityResult(request);

            Assert.IsNotNull(activity.Tracking);
            var trackingKey = activity.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(requestId)).Select(kvp => kvp.Key).Single();
            Assert.AreEqual(trackingKey, this.testCampaign.ExternalEntityId.ToString());
            var trackingEntry = activity.Tracking[trackingKey];
            Assert.AreEqual(
                trackingEntry.WorkItemDictionary[requestId].Substring(
                    trackingEntry.WorkItemDictionary[requestId].Length - "Completed".Length),
                "Completed");
            Assert.AreEqual(trackingEntry.WorkItemDictionary.Count(), 2);

            // now send the RetrieveReportActivity
            // use the key from the second dictionary entry as the request id
            var requestId2 = trackingEntry.WorkItemDictionary.Keys.ElementAt(1); 
            var request2 = new ActivityResult
            {
                Task = "APNXRetrieveCampaignReport",
                Succeeded = true,
                RequestId = requestId2,
                Values =
                        {
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId },
                            { "LineItemId", "Test line id" },
                            { "ReportId", "TestReportID" },
                            { "Ready", "true" }
                        }
            };
            activity.OnActivityResult(request2);

            var trackingKey2 = activity.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(requestId2)).Select(kvp => kvp.Key).Single();
            trackingEntry = activity.Tracking[trackingKey2];
            Assert.AreEqual(
                trackingEntry.WorkItemDictionary[requestId2].Substring(
                    trackingEntry.WorkItemDictionary[requestId2].Length - "Completed".Length),
                "Completed");
            Assert.AreEqual(trackingEntry.WorkItemDictionary.Count(), 3);

            // finally send the DAGetBudgetAllocations
            var requestId3 = trackingEntry.WorkItemDictionary.Keys.ElementAt(2);
            var request3 = new ActivityResult
            {
                Task = "DAGetBudgetAllocations",
                Succeeded = true,
                RequestId = requestId3,
                Values =
                        {
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId },
                            { "LineItemId", "Test line id" },
                            { "ReportId", "TestReportID" }
                        }
            };
            activity.OnActivityResult(request3);

            var trackingKey3 = activity.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(requestId3)).Select(kvp => kvp.Key).Single();
            trackingEntry = activity.Tracking[trackingKey3];
            Assert.AreEqual(
                trackingEntry.WorkItemDictionary[requestId3].Substring(
                    trackingEntry.WorkItemDictionary[requestId3].Length - "Completed".Length),
                "Completed");
        }

        /// <summary>
        /// Test OnActivityResult for RequestReportActivity, RetrieveReportActivity and DAGetBudgetAllocations
        /// </summary>
        [TestMethod]
        public void RequestReportActivityActivityResultFailSubmit()
        {
            var requestId = Guid.NewGuid().ToString("N");
            var request = new ActivityResult
                {
                    Task = "APNXRequestCampaignReport",
                    Succeeded = true,
                    RequestId = requestId,
                    Values =
                        {
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId },
                            { "LineItemId", "Test line id" },
                            { "ReportId", "TestReportID" }
                        }
                };

            var activity = this.CreateActivityFailSubmit();
            this.trackingDictionary = activity.Tracking;
            var trackingInfo = new ReallocateNowActivity.TrackingInfo(
                this.testCampaignEntityId, "TestCampaignName", 12345);
            trackingInfo.WorkItemDictionary[requestId] = "{0}|{1}".FormatInvariant(request.Task, "Submitted");
            this.trackingDictionary[this.testCampaign.ExternalEntityId.ToString()] = trackingInfo;
            activity.OnActivityResult(request);

            Assert.IsNotNull(activity.Tracking);
            var trackingKey =
                activity.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(requestId)).Select(
                    kvp => kvp.Key).Single();
            Assert.AreEqual(trackingKey, this.testCampaign.ExternalEntityId.ToString());
            var trackingEntry = activity.Tracking[trackingKey];
            Assert.AreEqual(
                trackingEntry.WorkItemDictionary.Last().Value.Substring(
                trackingEntry.WorkItemDictionary.Last().Value.Length - "Failed".Length),
                "Failed");
            Assert.AreEqual(trackingEntry.WorkItemDictionary.Count(), 2);
        }

        /// <summary>
        /// Test OnActivityResult for RequestReportActivity, RetrieveReportActivity fail submit
        /// </summary>
        [TestMethod]
        public void RetrieveReportActivityActivityResultFailSubmit()
        {
            var requestId = Guid.NewGuid().ToString("N");
            var request = new ActivityResult
                {
                    Task = "APNXRequestCampaignReport",
                    Succeeded = true,
                    RequestId = requestId,
                    Values =
                        {
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId },
                            { "LineItemId", "Test line id" },
                            { "ReportId", "TestReportID" }
                        }
                };

            var activity = this.CreateActivity();
            this.trackingDictionary = activity.Tracking;
            var trackingInfo = new ReallocateNowActivity.TrackingInfo(
                this.testCampaignEntityId, "TestCampaignName", 12345);
            trackingInfo.WorkItemDictionary[requestId] = "{0}|{1}".FormatInvariant(request.Task, "Submitted");
            this.trackingDictionary[this.testCampaign.ExternalEntityId.ToString()] = trackingInfo;
            activity.OnActivityResult(request);

            Assert.IsNotNull(activity.Tracking);
            var trackingKey =
                activity.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(requestId)).Select(
                    kvp => kvp.Key).Single();
            Assert.AreEqual(trackingKey, this.testCampaign.ExternalEntityId.ToString());
            var trackingEntry = activity.Tracking[trackingKey];
            Assert.AreEqual(
                trackingEntry.WorkItemDictionary[requestId].Substring(
                    trackingEntry.WorkItemDictionary[requestId].Length - "Completed".Length),
                "Completed");
            Assert.AreEqual(trackingEntry.WorkItemDictionary.Count(), 2);

            // now send the RetrieveReportActivity
            // use the key from the second dictionary entry as the request id
            var requestId2 = trackingEntry.WorkItemDictionary.Keys.ElementAt(1);
            var request2 = new ActivityResult
                {
                    Task = "APNXRetrieveCampaignReport",
                    Succeeded = true,
                    RequestId = requestId2,
                    Values =
                        {
                            { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                            { EntityActivityValues.CompanyEntityId, this.testCompanyEntityId },
                            { "LineItemId", "Test line id" },
                            { "ReportId", "TestReportID" },
                            { "Ready", "true" }
                        }
                };
            var failActivity = this.CreateActivityFailSubmit();
            failActivity.OnActivityResult(request2);

            var trackingKey2 =
                activity.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(requestId2)).Select(
                    kvp => kvp.Key).Single();
            trackingEntry = activity.Tracking[trackingKey2];
            Assert.AreEqual(
                 trackingEntry.WorkItemDictionary.Last().Value.Substring(
                 trackingEntry.WorkItemDictionary.Last().Value.Length - "Failed".Length),
                 "Failed");
            Assert.AreEqual(trackingEntry.WorkItemDictionary.Count(), 3);
        }

        /// <summary>
        /// Test OnActivityResult failure for RequestReportActivity, RetrieveReportActivity and DAGetBudgetAllocations
        /// </summary>
        [TestMethod]
        public void RequestReportActivityActivityResultNotSucceeded()
        {
            var requestId = Guid.NewGuid().ToString("N");
            var request = new ActivityResult
                { Task = "APNXRequestCampaignReport", Succeeded = false, RequestId = requestId, };

            var activity = this.CreateActivity();
            activity.OnActivityResult(request);
            Assert.IsNotNull(activity.Tracking);            
  
            var requestId2 = Guid.NewGuid().ToString("N");
            var request2 = new ActivityResult
            {
                Task = "APNXRetrieveCampaignReport",
                Succeeded = false,
                RequestId = requestId2,
            };
            activity.OnActivityResult(request2);
            Assert.IsNotNull(activity.Tracking);

            var requestId3 = Guid.NewGuid().ToString("N");
            var request3 = new ActivityResult
            {
                Task = "DAGetBudgetAllocations",
                Succeeded = false,
                RequestId = requestId3,
            };
            activity.OnActivityResult(request3);
            Assert.IsNotNull(activity.Tracking);
        }

        /// <summary>
        /// Creates an instance of the ReallocateNowActivity activity
        /// </summary>
        /// <returns>The activity instance</returns>
        private ReallocateNowActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository },
            };

            return Activity.CreateActivity(typeof(ReallocateNowActivity), context, this.SubmitActivityRequest) as ReallocateNowActivity;
        }

        /// <summary>
        /// Creates an instance of the ReallocateNowActivity activity with fail response
        /// </summary>
        /// <returns>The activity instance</returns>
        private ReallocateNowActivity CreateActivityFailSubmit()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.mockRepository },
            };

            return Activity.CreateActivity(typeof(ReallocateNowActivity), context, this.SubmitFailedActivityRequest) as ReallocateNowActivity;
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
       
        /// <summary>Test submit activity request handler with failure</summary>
        /// <param name="request">The request</param>
        /// <param name="sourceName">The source name</param>
        /// <returns>True if successful; otherwise, false.</returns>
        private bool SubmitFailedActivityRequest(ActivityRequest request, string sourceName)
        {
            this.submittedRequest = request;
            return false;
        }

        /// <summary>Create the test entities</summary>
        private void CreateTestEntities()
        {
            this.testCompany = EntityTestHelpers.CreateTestCompanyEntity(
                (this.testCompanyEntityId = new EntityId()).ToString(),
                "Test Company");

            this.testCampaign =
                EntityTestHelpers.CreateTestCampaignEntity(
                    (this.testCampaignEntityId = new EntityId()).ToString(),
                    "Test Campaign",
                    100000,
                    DateTime.UtcNow,
                    DateTime.UtcNow + new TimeSpan(48, 0, 0),
                    "Persona Name");

            this.testCampaign.SetAppNexusLineItemId(2000);
        }
    }
}
