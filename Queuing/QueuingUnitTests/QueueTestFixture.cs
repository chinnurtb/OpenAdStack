//-----------------------------------------------------------------------
// <copyright file="QueueTestFixture.cs" company="Rare Crowds Inc">
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
using System.Threading;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace QueuingUnitTests
{
    /// <summary>
    /// Tests for the queue
    /// </summary>
    [TestClass]
    public class QueueTestFixture
    {
        /// <summary>test logger</summary>
        private ILogger logger;

        /// <summary>Mock ICategorizedQueue</summary>
        private ICategorizedQueue mockQueue;

        /// <summary>
        /// Initialize the mocks and config before each test
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            ConfigurationManager.AppSettings["QueueProcessor.MinQueuePollWait"] = "10";
            ConfigurationManager.AppSettings["QueueProcessor.MaxQueuePollWait"] = "100";
            ConfigurationManager.AppSettings["QueueProcessor.QueuePollBackoff"] = "1.5";
            ConfigurationManager.AppSettings["QueueProcessor.InactiveQueueTime"] = "1000";
            ConfigurationManager.AppSettings["QueueProcessor.InactiveQueuePollWait"] = "200";
            ConfigurationManager.AppSettings["QueueProcessor.MaxPollBatchSize"] = "0";
            ConfigurationManager.AppSettings["Queue.WorkItemStoreName"] = "workitems";
            ConfigurationManager.AppSettings["Queue.FailedWorkItemStoreName"] = "failedworkitems";
            ConfigurationManager.AppSettings["Queue.WorkItemRetentionPeriod"] = "1:00:00";
            ConfigurationManager.AppSettings["Queue.EnqueueRetries"] = "5";

            SimulatedPersistentDictionaryFactory.Initialize();

            this.logger = new TestLogger();
            LogManager.Initialize(new[] { this.logger });

            this.InitializeMockQueue();
        }

        /// <summary>Test periodic work item cleanup</summary>
        [TestMethod]
        public void CleanupProcessedWorkItem()
        {
            var finishedStatuses = new[] { WorkItemStatus.Processed, WorkItemStatus.Completed, WorkItemStatus.Failed };
            var workItemsStore = PersistentDictionaryFactory.CreateDictionary<WorkItem>("workitems");
            var failedWorkItemsStore = PersistentDictionaryFactory.CreateDictionary<WorkItem>("failedworkitems");

            // Create test work items (one of each status)
            var expiredWorkItems = Enum.GetValues(typeof(WorkItemStatus))
                .Cast<WorkItemStatus>()
                .Select(status =>
                    new WorkItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Status = status,
                        ProcessingCompleteTime =
                            finishedStatuses.Contains(status) ?
                            DateTime.UtcNow.AddHours(-2) :
                            default(DateTime)
                    });
            var unexpiredWorkItems = finishedStatuses
                .Select(status =>
                    new WorkItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        Status = status,
                        ProcessingCompleteTime = DateTime.UtcNow.AddMinutes(-5)
                    });
            var workItems = expiredWorkItems.Concat(unexpiredWorkItems);
            foreach (var workItem in workItems)
            {
                workItemsStore[workItem.Id] = workItem;
            }

            // Run cleanup
            var queue = new Queue(this.mockQueue);
            queue.CleanupWorkItems();

            Assert.AreEqual(6, workItemsStore.Count);
            var unremovedFinishedWorkItems = workItemsStore.Values
                .Where(wi =>
                    wi.Status == WorkItemStatus.Processed ||
                    wi.Status == WorkItemStatus.Completed ||
                    wi.Status == WorkItemStatus.Failed);
            Assert.AreEqual(3, unremovedFinishedWorkItems.Count());
            Assert.AreEqual(1, failedWorkItemsStore.Count);
            Assert.IsNotNull(failedWorkItemsStore.Values.SingleOrDefault(wi =>
                wi.Status == WorkItemStatus.Failed));
        }

        /// <summary>
        /// Enqueue a processed work item and dequeue it
        /// </summary>
        [TestMethod]
        public void EnqueueDequeueProcessedWorkItem()
        {
            var resultType = WorkItemResultType.Shared;
            var source = Guid.NewGuid().ToString("N");
            var workItem = new WorkItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = WorkItemStatus.Processed,
                Category = "RequestCategory",
                Source = source,
                ResultType = resultType,
                Content = "Request",
                Result = "Result"
            };
            var sourceTypeCategory = Queue.GetResultCategory(resultType, source);

            var queue = new Queue(this.mockQueue);
            queue.EnqueueProcessedWorkItem(workItem);

            var processedWorkItems = queue.DequeueProcessedWorkItems(resultType, source, 10);
            var result = processedWorkItems.SingleOrDefault();
            Assert.IsNotNull(result);
            Assert.AreEqual(workItem.Id, result.Id);
            Assert.AreEqual(workItem.Source, result.Source);
            Assert.AreEqual(workItem.ResultType, result.ResultType);
            Assert.AreEqual(sourceTypeCategory, result.Category);

            queue.RemoveFromQueue(workItem);
            Assert.AreEqual(0, queue.DequeueProcessedWorkItems(resultType, null, 10).Length);
        }

        /// <summary>Initialize the queue mock</summary>
        private void InitializeMockQueue()
        {
            var queueEntries = new Dictionary<string, IDictionary<string, WorkItemQueueEntry>>();
            this.mockQueue = MockRepository.GenerateMock<ICategorizedQueue>();
            this.mockQueue.Stub(f => f.Enqueue(Arg<WorkItemQueueEntry>.Is.Anything))
                .WhenCalled(call =>
                {
                    var queueEntry = call.Arguments[0] as WorkItemQueueEntry;
                    if (!queueEntries.ContainsKey(queueEntry.Category))
                    {
                        queueEntries[queueEntry.Category] = new Dictionary<string, WorkItemQueueEntry>();
                    }

                    queueEntries[queueEntry.Category][queueEntry.WorkItemId] = queueEntry;
                });
            this.mockQueue.Stub(f => f.Dequeue(Arg<string>.Is.Anything, Arg<int>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var category = call.Arguments[0] as string;
                    call.ReturnValue = queueEntries[category].Values.ToArray();
                });
            this.mockQueue.Stub(f => f.Delete(Arg<WorkItemQueueEntry>.Is.Anything))
                .WhenCalled(call =>
                {
                    var queueEntry = call.Arguments[0] as WorkItemQueueEntry;
                    if (queueEntries.ContainsKey(queueEntry.Category))
                    {
                        queueEntries[queueEntry.Category].Remove(queueEntry.WorkItemId);
                    }
                });
        }
    }
}
