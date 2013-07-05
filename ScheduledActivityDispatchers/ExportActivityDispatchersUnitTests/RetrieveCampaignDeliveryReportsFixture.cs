//-----------------------------------------------------------------------
// <copyright file="RetrieveCampaignDeliveryReportsFixture.cs" company="Rare Crowds Inc">
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
using Activities;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkActivityDispatchers;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using ScheduledActivities;
using TestUtilities;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace DeliveryNetworkActivityDispatchersUnitTests
{
    /// <summary>
    /// Test the RetrieveAppNexusCampaignReports ScheduledActivitySource
    /// </summary>
    [TestClass]
    public class RetrieveCampaignDeliveryReportsFixture
    {
        /// <summary>Slot key for in-progress entries</summary>
        internal const string InProgressSlotKey = "<InProgress>";

        /// <summary>Maximum number of simultaneous AppNexus report requests</summary>
        private const int AppNexusMaxReportRequests = 5;

        /// <summary>Maximum number of simultaneous Google DFP report requests</summary>
        private const int GoogleDfpMaxReportRequests = 10;

        /// <summary>Random number generator</summary>
        private static Random random = new Random();

        /// <summary>Mock IQueuer used for testing</summary>
        private IQueuer mockQueuer;

        /// <summary>List of work items queued by the mock queuer</summary>
        private IList<WorkItem> queuedWorkItems;

        /// <summary>Work-item being enqueued by the mock queuer</summary>
        private WorkItem enqueuingWorkItem;

        /// <summary>Initializes mocks, etc for the tests</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["System.AuthUserId"] = Guid.NewGuid().ToString("n");
            ConfigurationManager.AppSettings["Delivery.RetrieveCampaignReportsSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.ReportsRequestExpiry"] = "00:01:00";
            ConfigurationManager.AppSettings["Delivery.ReportsRetrieveExpiry"] = "00:01:00";
            ConfigurationManager.AppSettings["AppNexus.MaxReportRequests"] = AppNexusMaxReportRequests.ToString(CultureInfo.InvariantCulture);
            ConfigurationManager.AppSettings["GoogleDfp.MaxReportRequests"] = GoogleDfpMaxReportRequests.ToString(CultureInfo.InvariantCulture);

            Scheduler.Registries = null;
            SimulatedPersistentDictionaryFactory.Initialize();
            
            this.mockQueuer = MockRepository.GenerateMock<IQueuer>();
            this.queuedWorkItems = new List<WorkItem>();

            LambdaConstraint<WorkItem> workItemCaptureConstraint = new LambdaConstraint<WorkItem>(wi =>
            {
                this.enqueuingWorkItem = wi;
                return true;
            });

            this.mockQueuer.Stub(q => q.EnqueueWorkItem(ref Arg<WorkItem>.Ref(workItemCaptureConstraint, null).Dummy))
                .Return(true)
                .WhenCalled(call =>
                    {
                        this.enqueuingWorkItem.Status = WorkItemStatus.Pending;
                        this.queuedWorkItems.Add(this.enqueuingWorkItem);
                        call.Arguments[0] = this.enqueuingWorkItem;
                    });
        }

        /// <summary>Cleans up after the tests</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            SimulatedPersistentStorage.Clear();
        }

        /// <summary>Test creating the activity source</summary>
        [TestMethod]
        public void Create()
        {
            var source = this.CreateScheduledActivitySource();
            Assert.IsNotNull(source);
        }

        /// <summary>Test creating a request for a report request</summary>
        [TestMethod]
        public void CreateRequestReportRequest()
        {
            var campaignEntityId = new EntityId().ToString();
            var companyEntityId = new EntityId().ToString();

            var source = this.CreateScheduledActivitySource();
            Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                DateTime.UtcNow,
                campaignEntityId,
                companyEntityId,
                DeliveryNetworkDesignation.AppNexus);

            source.CreateScheduledRequests();

            Assert.AreEqual(1, this.queuedWorkItems.Count);
            var queuedWorkItem = this.queuedWorkItems.First();
            ValidateWorkItem(
                queuedWorkItem,
                "delivery.retrievecampaignreports",
                AppNexusActivityTasks.RequestCampaignReport);
        }

        /// <summary>Test creating a request for a report request</summary>
        [TestMethod]
        public void CreateRetrieveReportRequest()
        {
            var reportId = Guid.NewGuid().ToString("n");
            var campaignEntityId = new EntityId().ToString();
            var companyEntityId = new EntityId().ToString();

            var source = this.CreateScheduledActivitySource();
            var appNexusReportsToRetrieveRegistry =
                DeliveryNetworkSchedulerRegistries.ReportsToRetrieve +
                DeliveryNetworkDesignation.AppNexus.ToString();
            var registry = Scheduler.GetRegistry<Tuple<string, string>>(
                appNexusReportsToRetrieveRegistry);
            registry.Add(
                DateTime.UtcNow,
                reportId,
                new Tuple<string, string>(
                    campaignEntityId,
                    companyEntityId));

            source.CreateScheduledRequests();

            Assert.AreEqual(1, this.queuedWorkItems.Count);
            var queuedWorkItem = this.queuedWorkItems.First();
            ValidateWorkItem(
                queuedWorkItem,
                "delivery.retrievecampaignreports",
                AppNexusActivityTasks.RetrieveCampaignReport);
        }

        /// <summary>
        /// Test queuing the maximum number of simultaneous requests
        /// </summary>
        [TestMethod]
        public void QueueMaxSimultaneousRequests()
        {
            var numberOfReportsToRequest = AppNexusMaxReportRequests * 2;
            var numberOfReportsToRetrieve = 2;

            // Create the scheduled activity source
            var source = this.CreateScheduledActivitySource();

            // Add a couple reports to the reports to retrieve
            var appNexusReportsToRetrieveRegistry =
                DeliveryNetworkSchedulerRegistries.ReportsToRetrieve +
                DeliveryNetworkDesignation.AppNexus.ToString();
            for (int i = 0; i < numberOfReportsToRetrieve; i++)
            {
                Scheduler.AddToSchedule<string, string>(
                    appNexusReportsToRetrieveRegistry,
                    DateTime.UtcNow,
                    "1000{0}".FormatInvariant(i),
                    new EntityId().ToString(),
                    new EntityId().ToString());
            }

            // Add more than the maximum to the reports to request
            for (int i = 0; i < numberOfReportsToRequest; i++)
            {
                Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.ReportsToRequest,
                    DateTime.UtcNow,
                    new EntityId(),
                    new EntityId(),
                    DeliveryNetworkDesignation.AppNexus);
            }

            // Schedule the requests
            source.CreateScheduledRequests();

            // Assert that only up to the max activity requests were queued
            Assert.AreEqual(AppNexusMaxReportRequests, this.queuedWorkItems.Count);

            // Assert that only up to the max registry entries were moved to in-progress
            // (minus the number of pending reports to retrieve)
            var reportsToRequestInProgressCount = 
                Scheduler.GetInProgressCount<string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.ReportsToRequest);
            Assert.AreEqual(
                 AppNexusMaxReportRequests - numberOfReportsToRetrieve,
                 reportsToRequestInProgressCount);
            
            // Simulate a report being retrieved
            Assert.IsTrue(
                Scheduler.RemoveCompletedEntry<string, string>(
                    appNexusReportsToRetrieveRegistry,
                    "10000"));

            // Schedule more requests
            source.CreateScheduledRequests();

            // Assert exactly one more activity requests were queued
            Assert.AreEqual(AppNexusMaxReportRequests + 1, this.queuedWorkItems.Count);

            // Assert exactly one more registry entry was moved to in-progress
            reportsToRequestInProgressCount =
                Scheduler.GetInProgressCount<string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.ReportsToRequest);
            Assert.AreEqual(
                AppNexusMaxReportRequests - numberOfReportsToRetrieve + 1,
                reportsToRequestInProgressCount);
        }

        /// <summary>
        /// Test that failed report request are immediately rescheduled.
        /// </summary>
        [TestMethod]
        public void RequestReportFailed()
        {
            // Test ids
            var campaignEntityId = new EntityId().ToString();
            var companyEntityId = new EntityId().ToString();

            // Test request for RequestCampaignReportActivity
            var requestRequest = new ActivityRequest
            {
                Task = AppNexusActivityTasks.RequestCampaignReport,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                }
            };

            // Test result from RequestCampaignReportActivity
            var requestResult = new ActivityResult
            {
                Task = AppNexusActivityTasks.RequestCampaignReport,
                Succeeded = true,
                RequestId = requestRequest.Id,
                Values =
                {
                    { AppNexusActivityValues.ReportId, string.Empty },
                    { AppNexusActivityValues.Reschedule, false.ToString() },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                }
            };

            // Create the scheduled activity source
            var source = this.CreateScheduledActivitySource();

            // Add an entry to the in-progress registry for a report request
            var reportsToRequest = Scheduler.GetRegistry<Tuple<string, DeliveryNetworkDesignation>>(
                DeliveryNetworkSchedulerRegistries.ReportsToRequest);
            reportsToRequest.Add(
                DateTime.UtcNow,
                campaignEntityId,
                new Tuple<string, DeliveryNetworkDesignation>(
                    companyEntityId,
                    DeliveryNetworkDesignation.AppNexus));

            // Handle the result of the request
            // This will schedule the retrieve
            RetrieveCampaignDeliveryReports.OnRequestReportResult(requestRequest, requestResult);

            // Assert that the request was rescheduled
            Assert.AreEqual(1, reportsToRequest[DateTime.UtcNow].Count);

            // Assert that the retrieve was NOT scheduled
            var appNexusReportsToRetrieveRegistry =
                DeliveryNetworkSchedulerRegistries.ReportsToRetrieve +
                DeliveryNetworkDesignation.AppNexus.ToString();
            var reportsToRetrieve = Scheduler.GetRegistry<Tuple<string, string>>(
                appNexusReportsToRetrieveRegistry);
            Assert.AreEqual(0, reportsToRetrieve[DateTime.UtcNow].Count);
        }

        /// <summary>
        /// Test that report retrieve entries are removed from the in-progress
        /// registry slot once they have been received.
        /// </summary>
        [TestMethod]
        public void RetrieveReport()
        {
            // Test ids
            var reportId = Guid.NewGuid().ToString("N");
            var campaignEntityId = new EntityId().ToString();
            var companyEntityId = new EntityId().ToString();

            // Test request for RequestCampaignReportActivity
            var requestRequest = new ActivityRequest
            {
                Task = AppNexusActivityTasks.RequestCampaignReport,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                }
            };

            // Test result from RequestCampaignReportActivity
            var requestResult = new ActivityResult
            {
                Task = AppNexusActivityTasks.RequestCampaignReport,
                Succeeded = true,
                RequestId = requestRequest.Id,
                Values =
                {
                    { AppNexusActivityValues.ReportId, reportId },
                    { AppNexusActivityValues.Reschedule, false.ToString() },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                }
            };

            // Create the scheduled activity source
            var source = this.CreateScheduledActivitySource();

            // Add an entry to the in-progress registry for a report request
            var reportsToRequest = Scheduler.GetRegistry<Tuple<string, DeliveryNetworkDesignation>>(
                DeliveryNetworkSchedulerRegistries.ReportsToRequest);
            reportsToRequest.Add(
                DateTime.UtcNow,
                campaignEntityId,
                new Tuple<string, DeliveryNetworkDesignation>(
                    companyEntityId,
                    DeliveryNetworkDesignation.AppNexus));
            
            // Handle the result of the request
            // This will schedule the retrieve
            RetrieveCampaignDeliveryReports.OnRequestReportResult(requestRequest, requestResult);

            // Assert that the retrieve was scheduled
            var appNexusReportsToRetrieveRegistry =
                DeliveryNetworkSchedulerRegistries.ReportsToRetrieve +
                DeliveryNetworkDesignation.AppNexus.ToString();
            var reportsToRetrieve = Scheduler.GetRegistry<Tuple<string, string>>(
                appNexusReportsToRetrieveRegistry);

            Assert.AreEqual(1, reportsToRetrieve[DateTime.UtcNow].Count);
            var scheduledRetrieve = reportsToRetrieve[DateTime.UtcNow].SingleOrDefault();
            Assert.IsNotNull(scheduledRetrieve);
            Assert.AreEqual(reportId, scheduledRetrieve.Item2);
            Assert.AreEqual(campaignEntityId, scheduledRetrieve.Item3.Item1);
            Assert.AreEqual(companyEntityId, scheduledRetrieve.Item3.Item2);

            // This will submit the request to retrieve the report and move the
            // schedule entry for the report to in-progress
            source.RetrieveReports();
            
            // Verify the activity request was submitted
            Assert.AreEqual(1, this.queuedWorkItems.Count);
            var retrieveWorkItem = this.queuedWorkItems.SingleOrDefault();
            Assert.IsNotNull(retrieveWorkItem);
            Assert.AreEqual(source.Name, retrieveWorkItem.Source);
            
            var retrieveRequest = ActivityRequest.DeserializeFromXml(retrieveWorkItem.Content);
            Assert.IsNotNull(retrieveRequest);

            // Test result from RetrieveCampaignReportActivity
            var retrieveResult = new ActivityResult
            {
                Task = AppNexusActivityTasks.RetrieveCampaignReport,
                Succeeded = true,
                RequestId = retrieveRequest.Id,
                Values =
                {
                    { AppNexusActivityValues.ReportId, reportId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                }
            };

            // Assert the schedule entry was moved to in-progress
            Assert.AreEqual(0, reportsToRetrieve[DateTime.UtcNow].Count);
            Assert.AreEqual(1, reportsToRetrieve.InProgress.Count);
            var inProgressRetrieve = reportsToRetrieve.InProgress.SingleOrDefault();
            Assert.IsNotNull(inProgressRetrieve);
            Assert.AreEqual(reportId, inProgressRetrieve.Key);
            Assert.AreEqual(campaignEntityId, inProgressRetrieve.Value.Item1);
            Assert.AreEqual(companyEntityId, inProgressRetrieve.Value.Item2);

            // Handle the result of the successful retrieve
            RetrieveCampaignDeliveryReports.OnRetrieveReportResult(retrieveRequest, retrieveResult);

            // Verify the schedule entry was removed from in-progress
            Assert.AreEqual(0, reportsToRetrieve.InProgress.Count);
        }

        /// <summary>Validates the work item has the expected values</summary>
        /// <param name="workItem">The work item</param>
        /// <param name="expectedWorkItemSource">Expected Workitem.Source</param>
        /// <param name="expectedActivityTaskName">Expected ActivityRequest.TaskName</param>
        private static void ValidateWorkItem(WorkItem workItem, string expectedWorkItemSource, string expectedActivityTaskName)
        {
            Assert.IsNotNull(workItem);
            Assert.AreEqual(expectedWorkItemSource, workItem.Source);
            var activityRequest = ActivityRequest.DeserializeFromXml(workItem.Content);
            Assert.AreEqual(expectedActivityTaskName, activityRequest.Task);
        }

        /// <summary>Creates an instance of the scheduled activity source</summary>
        /// <returns>The RetrieveCampaignDeliveryReports instance</returns>
        private RetrieveCampaignDeliveryReports CreateScheduledActivitySource()
        {
            return ScheduledActivities.ScheduledActivitySource.Create(
                typeof(RetrieveCampaignDeliveryReports),
                null,
                this.mockQueuer)
                as RetrieveCampaignDeliveryReports;
        }
    }
}
