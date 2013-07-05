//-----------------------------------------------------------------------
// <copyright file="ExportDynamicAllocationCampaignsFixture.cs" company="Rare Crowds Inc">
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
using DynamicAllocationUtilities;
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
    /// Test the ExportDynamicAllocationCampaigns ScheduledActivitySource
    /// </summary>
    [TestClass]
    public class ExportDynamicAllocationCampaignsFixture
    {
        /// <summary>Random number generator</summary>
        private static Random random = new Random();

        /// <summary>Test AuthUserId</summary>
        private string authUserId;

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
            this.authUserId = Guid.NewGuid().ToString("n");
            ConfigurationManager.AppSettings["System.AuthUserId"] = this.authUserId;
            ConfigurationManager.AppSettings["Delivery.ExportDACampaignsSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.ExportDACampaignRequestExpiry"] = "00:01:00";
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
        public void ExportCampaignsRequest()
        {
            var companyEntityId = new EntityId().ToString();
            var campaignEntityIds = new[]
                {
                    new EntityId().ToString(),
                    new EntityId().ToString(),
                    new EntityId().ToString()
                };
            var exportAllocationsEntityId = new Dictionary<string, string>();
            foreach (var campaignEntityId in campaignEntityIds)
            {
                var allocationIds = new string[250];
                for (var i = 0; i < 250; i++)
                {
                    allocationIds[i] = Guid.NewGuid().ToString("N");
                }

                exportAllocationsEntityId[campaignEntityId] = new EntityId();

                Assert.IsTrue(Scheduler.AddToSchedule<string, string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.CampaignsToExport,
                    DateTime.UtcNow,
                    campaignEntityId,
                    companyEntityId,
                    exportAllocationsEntityId[campaignEntityId],
                    DeliveryNetworkDesignation.AppNexus));
            }

            var source = this.CreateScheduledActivitySource();
            source.CreateScheduledRequests();

            Assert.AreEqual(campaignEntityIds.Length, this.queuedWorkItems.Count);
            foreach (var campaignEntityId in campaignEntityIds)
            {
                var queuedWorkItem = this.queuedWorkItems
                    .Where(wi => wi.Content.Contains(campaignEntityId))
                    .SingleOrDefault();
                var expectedRequestValues = new Dictionary<string, string>
                {
                    { EntityActivityValues.AuthUserId, this.authUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId },
                    { DynamicAllocationActivityValues.ExportAllocationsEntityId, exportAllocationsEntityId[campaignEntityId] }
                };
                ValidateWorkItem(
                    queuedWorkItem,
                    "delivery.exportdacampaigns",
                    AppNexusActivityTasks.ExportDACampaign,
                    expectedRequestValues);
            }
        }

        /// <summary>Validates the work item has the expected values</summary>
        /// <param name="workItem">The work item</param>
        /// <param name="expectedWorkItemSource">Expected Workitem.Source</param>
        /// <param name="expectedActivityTaskName">Expected ActivityRequest.TaskName</param>
        /// <param name="expectedRequestValues">Expected ActivityRequest.Values</param>
        private static void ValidateWorkItem(
            WorkItem workItem,
            string expectedWorkItemSource,
            string expectedActivityTaskName,
            IDictionary<string, string> expectedRequestValues)
        {
            Assert.IsNotNull(workItem);
            Assert.AreEqual(expectedWorkItemSource, workItem.Source);
            var request = ActivityRequest.DeserializeFromXml(workItem.Content);
            Assert.AreEqual(expectedActivityTaskName, request.Task);
            Assert.AreEqual(expectedRequestValues.Count, request.Values.Count);
            foreach (var kvp in expectedRequestValues)
            {
                Assert.IsTrue(request.Values.ContainsKey(kvp.Key));
                Assert.AreEqual(kvp.Value, request.Values[kvp.Key]);
            }
        }

        /// <summary>Creates an instance of the scheduled activity source</summary>
        /// <returns>The RetrieveAppNexusCampaignReports instance</returns>
        private ExportDynamicAllocationCampaigns CreateScheduledActivitySource()
        {
            return ScheduledActivities.ScheduledActivitySource.Create(
                typeof(ExportDynamicAllocationCampaigns),
                null,
                this.mockQueuer)
                as ExportDynamicAllocationCampaigns;
        }
    }
}
