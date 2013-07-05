//-----------------------------------------------------------------------
// <copyright file="UpdateExportedCreativesStatusFixture.cs" company="Rare Crowds Inc">
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
using Diagnostics;
using Diagnostics.Testing;
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
    /// Test the UpdateExportedCreativesStatus ScheduledActivitySource
    /// </summary>
    [TestClass]
    public class UpdateExportedCreativesStatusFixture
    {
        /// <summary>Slot key for in-progress entries</summary>
        internal const string InProgressSlotKey = "<InProgress>";

        /// <summary>Random number generator</summary>
        private static readonly Random R = new Random();

        /// <summary>Test logger</summary>
        private TestLogger testLogger;

        /// <summary>Test alert logger</summary>
        private TestLogger testAlertLogger;

        /// <summary>Mock IQueuer used for testing</summary>
        private IQueuer mockQueuer;

        /// <summary>List of work items queued by the mock queuer</summary>
        private IList<WorkItem> queuedWorkItems;

        /// <summary>Work-item being enqueued by the mock queuer</summary>
        private WorkItem enqueuingWorkItem;

        /// <summary>Test company entity id</summary>
        private string companyEntityId;

        /// <summary>Test creative entity id</summary>
        private string creativeEntityId;

        /// <summary>Initializes mocks, etc for the tests</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["System.AuthUserId"] = Guid.NewGuid().ToString("n");
            ConfigurationManager.AppSettings["Delivery.UpdateCreativeStatusSchedule"] = "00:00:01";
            ConfigurationManager.AppSettings["Delivery.CreativeUpdateFrequency"] = "00:00:30";
            ConfigurationManager.AppSettings["Delivery.CreativeStatusUpdateRequestExpiry"] = "00:02:00";

            Scheduler.Registries = null;
            SimulatedPersistentDictionaryFactory.Initialize();
            LogManager.Initialize(new[]
            {
                this.testLogger = new TestLogger(),
                this.testAlertLogger = new TestLogger { AlertsOnly = true },
            });

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

            this.creativeEntityId = new EntityId().ToString();
            this.companyEntityId = new EntityId().ToString();
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

        /// <summary>Test creating a request for creative audit status update</summary>
        [TestMethod]
        public void CreateStatusUpdateRequest()
        {
            var source = this.CreateScheduledActivitySource();
            Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow,
                this.creativeEntityId,
                this.companyEntityId,
                DeliveryNetworkDesignation.AppNexus);

            source.CreateScheduledRequests();
            
            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(0, this.testLogger.WarningEntries.Count());

            Assert.AreEqual(1, this.queuedWorkItems.Count);
            var queuedWorkItem = this.queuedWorkItems.First();
            ValidateWorkItem(
                queuedWorkItem,
                "delivery.updatecreativestatus",
                AppNexusActivityTasks.UpdateCreativeAuditStatus);
        }

        /// <summary>Test handling an approved creative audit status result</summary>
        [TestMethod]
        public void HandleApprovedStatusUpdateResult()
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                }
            };
            var result = new ActivityResult
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Succeeded = true,
                RequestId = request.Id,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                    { "AuditStatus", "audited" },
                }
            };
            var source = this.CreateScheduledActivitySource();

            // Process the result
            source.OnActivityResult(request, result);

            // Verify no schedule entries created
            var scheduledCount = Scheduler.GetScheduledCount<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow.AddYears(1));
            Assert.AreEqual(0, scheduledCount);

            // Verify no warnings or errors logged
            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(0, this.testLogger.WarningEntries.Count());

            // Verify audit complete info log entry made
            Assert.IsNotNull(this.testLogger.InfoEntries
                .Where(e =>
                    e.Message.Contains(this.companyEntityId) &&
                    e.Message.Contains(this.creativeEntityId) &&
                    e.Message.ToUpperInvariant().Contains("COMPLETE"))
                .FirstOrDefault());
        }

        /// <summary>Test handling an approved creative audit status result</summary>
        [TestMethod]
        public void HandleNoAuditStatusUpdateResult()
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                }
            }; 
            var result = new ActivityResult
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Succeeded = true,
                RequestId = request.Id,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                    { "AuditStatus", "no_audit" },
                }
            };
            var source = this.CreateScheduledActivitySource();

            // Process the result
            source.OnActivityResult(request, result);

            // Verify no schedule entries created
            var scheduledCount = Scheduler.GetScheduledCount<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow.AddYears(1));
            Assert.AreEqual(0, scheduledCount);

            // Verify no warnings or errors logged
            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(0, this.testLogger.WarningEntries.Count());

            // Verify audit complete info log entry made
            Assert.IsNotNull(this.testLogger.InfoEntries
                .Where(e =>
                    e.Message.Contains(this.companyEntityId) &&
                    e.Message.Contains(this.creativeEntityId) &&
                    e.Message.ToUpperInvariant().Contains("COMPLETE"))
                .FirstOrDefault());
        }

        /// <summary>Test handling a rejected creative audit status result</summary>
        [TestMethod]
        public void HandleRejectedStatusUpdateResult()
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                }
            };
            var result = new ActivityResult
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Succeeded = true,
                RequestId = request.Id,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                    { "AuditStatus", "rejected" },
                }
            };
            var source = this.CreateScheduledActivitySource();

            // Process the result
            source.OnActivityResult(request, result);

            // Verify no schedule entries created
            var scheduledCount = Scheduler.GetScheduledCount<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow.AddYears(1));
            Assert.AreEqual(0, scheduledCount);

            // Verify no warnings or related info entries logged
            Assert.AreEqual(0, this.testLogger.WarningEntries.Count());
            Assert.AreEqual(0, this.testLogger.InfoEntries.Where(e => e.Message.Contains(this.creativeEntityId)).Count());

            // Verify audit rejected alert error log entry made
            Assert.IsTrue(new[] { this.testLogger, this.testAlertLogger }
                .All(log =>
                    null != log.ErrorEntries
                    .Where(e =>
                        e.Message.Contains(this.companyEntityId) &&
                        e.Message.Contains(this.creativeEntityId) &&
                        e.Message.ToUpperInvariant().Contains("FAILED") &&
                        e.Message.ToUpperInvariant().Contains("REJECTED"))
                    .FirstOrDefault()));
        }

        /// <summary>Test handling a pending creative audit status result</summary>
        [TestMethod]
        public void HandlePendingStatusUpdateResult()
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                }
            };
            var result = new ActivityResult
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Succeeded = true,
                RequestId = request.Id,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                    { "AuditStatus", "pending" },
                }
            };
            var source = this.CreateScheduledActivitySource();

            // Process the result
            source.OnActivityResult(request, result);

            // Verify update was rescheduled
            var scheduledCount = Scheduler.GetScheduledCount<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow.AddYears(1));
            Assert.AreEqual(1, scheduledCount);

            // Verify no warnings or related info entries logged
            Assert.AreEqual(0, this.testLogger.InfoEntries.Where(e => e.Message.Contains(this.creativeEntityId)).Count());
            Assert.AreEqual(0, this.testLogger.WarningEntries.Count());
            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
        }

        /// <summary>Test handling a failed creative audit status result</summary>
        [TestMethod]
        public void HandleFailedStatusUpdateResult()
        {
            var failureMessage = Guid.NewGuid().ToString();
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CreativeEntityId, this.creativeEntityId },
                }
            };
            var result = new ActivityResult
            {
                Task = AppNexusActivityTasks.UpdateCreativeAuditStatus,
                RequestId = request.Id,
                Succeeded = false,
                Error =
                {
                    ErrorId = 1,
                    Message = failureMessage,
                }
            };
            var source = this.CreateScheduledActivitySource();

            // Process the result
            source.OnActivityResult(request, result);

            // Verify update was not rescheduled
            var scheduledCount = Scheduler.GetScheduledCount<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow.AddYears(1));
            Assert.AreEqual(0, scheduledCount);

            // Verify no warnings or related info entries logged
            Assert.AreEqual(0, this.testLogger.InfoEntries.Where(e => e.Message.Contains(this.creativeEntityId)).Count());
            Assert.AreEqual(0, this.testLogger.WarningEntries.Count());
            
            // Verify audit failed alert log entry made with the expected message
            Assert.IsTrue(new[] { this.testLogger, this.testAlertLogger }
                .All(log =>
                    null != log.ErrorEntries
                    .Where(e => e.Message.Contains(failureMessage))
                    .FirstOrDefault()));
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
        private UpdateExportedCreativesStatus CreateScheduledActivitySource()
        {
            return ScheduledActivities.ScheduledActivitySource.Create(
                typeof(UpdateExportedCreativesStatus),
                null,
                this.mockQueuer)
                as UpdateExportedCreativesStatus;
        }
    }
}
