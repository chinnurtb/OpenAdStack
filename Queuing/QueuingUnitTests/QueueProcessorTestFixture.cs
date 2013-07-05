//-----------------------------------------------------------------------
// <copyright file="QueueProcessorTestFixture.cs" company="Rare Crowds Inc">
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
    /// Tests for the queue processor
    /// </summary>
    [TestClass]
    public class QueueProcessorTestFixture
    {
        /// <summary>Mock work item processor</summary>
        private IWorkItemProcessor workItemProcessor;

        /// <summary>Mock dequeuer</summary>
        private IDequeuer dequeuer;

        /// <summary>Mock queuer</summary>
        private IQueuer queuer;

        /// <summary>Test logger</summary>
        private TestLogger logger;

        /// <summary>
        /// Initialize the mocks and config before each test
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            ConfigurationManager.AppSettings["QueueProcessor.MinQueuePollWait"] = "50";
            ConfigurationManager.AppSettings["QueueProcessor.MaxQueuePollWait"] = "500";
            ConfigurationManager.AppSettings["QueueProcessor.QueuePollBackoff"] = "2";
            ConfigurationManager.AppSettings["QueueProcessor.InactiveQueueTime"] = "2000";
            ConfigurationManager.AppSettings["QueueProcessor.InactiveQueuePollWait"] = "600";
            ConfigurationManager.AppSettings["QueueProcessor.WorkItemCleanupFrequency"] = "0:00:10";
            ConfigurationManager.AppSettings["QueueProcessor.MaxPollBatchSize"] = "10";
            ConfigurationManager.AppSettings["QueueProcessor.LogStatsFrequency"] = "0:05:00";
            ConfigurationManager.AppSettings["QueueProcessor.MaxWarnings"] = "2";
            ConfigurationManager.AppSettings["QueueProcessor.WarningWait"] = "0:00:05";
            ConfigurationManager.AppSettings["Queue.WorkItemStoreName"] = "workitems";
            ConfigurationManager.AppSettings["Queue.FailedWorkItemStoreName"] = "failedworkitems";
            ConfigurationManager.AppSettings["Queue.WorkItemRetentionPeriod"] = "1:00:00";
            
            SimulatedPersistentDictionaryFactory.Initialize();
            
            this.workItemProcessor = MockRepository.GenerateStub<IWorkItemProcessor>();
            this.dequeuer = MockRepository.GenerateStub<IDequeuer>();
            this.queuer = MockRepository.GenerateStub<IQueuer>();

            this.logger = new TestLogger();
            LogManager.Initialize(new[] { this.logger });
        }

        /// <summary>Tests processing a work item</summary>
        [TestMethod]
        public void ProcessTest()
        {
            var workItem1 = new WorkItem { Id = Guid.NewGuid().ToString("N"), Status = WorkItemStatus.InProgress };
            var workItem2 = new WorkItem { Id = Guid.NewGuid().ToString("N"), Status = WorkItemStatus.InProgress };
            var expectedResult = Guid.NewGuid().ToString();
            var expectedWorkItem = new WorkItem { Id = workItem1.Id, Result = expectedResult };

            // Use thread interrupted exception because it is not handled by the queue processor
            // All other exceptions are caught and turned into error results
            var expectedException = new ThreadInterruptedException("Expected Exception");
            var expectedErrorMessage = "QueueProcessor exiting due to unhandled exception: System.Threading.ThreadInterruptedException: Expected Exception";

            // If the work item matches the first work item return the expected work item
            this.workItemProcessor.Stub(f => f.ProcessWorkItem(
                ref Arg<WorkItem>.Ref(
                Is.Matching<WorkItem>(wi => wi.Id == workItem1.Id),
                expectedWorkItem)
                .Dummy));

            // If the work item matches the second work item throw the expected exception
            this.workItemProcessor.Stub(f => f.ProcessWorkItem(
                ref Arg<WorkItem>.Ref(
                Is.Matching<WorkItem>(wi => wi.Id == workItem2.Id),
                null)
                .Dummy))
                .Throw(expectedException);

            // When work items are dequeued, return the two work items
            this.dequeuer.Stub(f => f.DequeueWorkItems(Arg<string>.Is.Anything, Arg<int>.Is.Anything)).Return(new[] { workItem1, workItem2 });

            // Create a QueueProcessor with the mock work item processor and dequeuer
            var processor = new QueueProcessor(this.workItemProcessor, this.dequeuer)
            {
                Categories = new[] { Guid.NewGuid().ToString("N") }
            };

            // Run the processor
            // When the second work item is processed, the mock will throw ThreadInterruptedException
            processor.Run();
            Assert.IsTrue(
                this.logger.Entries.Last().Message.StartsWith(expectedErrorMessage),
                "Expected ThreadInterruptedException not thrown");
        }

        /// <summary>Test idle queue polling backoff</summary>
        [TestMethod]
        public void IdleQueueBackOff()
        {
            var workItem = new WorkItem { Id = Guid.NewGuid().ToString("N"), Status = WorkItemStatus.InProgress };

            // Track the times when the processor attempts to dequeue work items
            var dequeueAttempts = new List<DateTime>();
            this.dequeuer.Stub(f =>
                f.DequeueWorkItems(Arg<string>.Is.Anything, Arg<int>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    // Only return work on the first attempt
                    call.ReturnValue = dequeueAttempts.Count == 0 ? new[] { workItem } : new WorkItem[0];
                    dequeueAttempts.Add(DateTime.UtcNow);
                });

            // Create a QueueProcessor with the mock work item processor and dequeuer
            var processor = new QueueProcessor(this.workItemProcessor, this.dequeuer)
            {
                Categories = new[] { Guid.NewGuid().ToString("N") }
            };

            // Run the processor for a bit and then kill it
            var processorThread = new Thread(() => { processor.Run(); }) { Name = "QueueProcessor" };
            var processorStart = DateTime.UtcNow;
            processorThread.Start();
            Thread.Sleep(3600);
            processorThread.Abort();

            // Verify at least one dequeue attempt was made
            Assert.IsTrue(dequeueAttempts.Count > 0);

            // Verify the dequeue attempt times are as expected
            var prevTime = 0.0;
            var dequeueTimes = dequeueAttempts
                .Select(t => (t - processorStart).TotalSeconds)
                .Select(t => { var d = t - prevTime; prevTime = t; return d; })
                .Select(t => Math.Ceiling(t * 1000.0))
                .ToArray();

            // Need at least 9 to validate all the way to idle
            Assert.IsTrue(dequeueTimes.Length >= 9);

            // Check the idle times against the expected times
            var expected = new[] { 0, 0, 100, 200, 400, 500, 500, 500, 600, 600, 600 };
            for (int i = 2; i < expected.Length && i < dequeueTimes.Length; i++)
            {
                // Verify actual time is not less than and within 25% of expected
                var maxExpected = expected[i] * 1.25;
                Assert.IsTrue(
                    dequeueTimes[i] >= expected[i] &&
                    dequeueTimes[i] < maxExpected,
                    "Expected: <{0} - {1}> Actual: <{2}>\n{3}",
                    expected[i],
                    maxExpected,
                    dequeueTimes[i],
                    string.Join(", ", dequeueTimes));
            }
        }

        /// <summary>Test statistics</summary>
        [TestMethod]
        public void LogStatistics()
        {
            var categories = new[] { "A", "B", "C", "D" };
            var processor = new QueueProcessor(this.workItemProcessor, this.dequeuer)
            {
                Categories = categories
            };
            var stats = new QueueProcessorStats(processor)
            {
                StartTime = DateTime.UtcNow.AddHours(-12)
            };

            var q = 0;
            foreach (var category in categories)
            {
                var dequeued = 10 * (10 - ++q);
                var failed = dequeued / 10;
                var processed = dequeued - failed;
                
                stats.AddDequeued(category, dequeued);
                
                for (int i = 0; i < processed; i++)
                {
                    var workItem = new WorkItem
                    {
                        Category = category,
                        QueuedTime = DateTime.UtcNow.AddMinutes(-30),
                        DequeueTime = DateTime.UtcNow.AddMinutes(-30).AddSeconds(q),
                        ProcessingStartTime = DateTime.UtcNow.AddMinutes(-29),
                        ProcessingCompleteTime = DateTime.UtcNow.AddMinutes(-29).AddSeconds(q * 10),
                    };
                    stats.AddProcessed(workItem);
                }

                for (int i = 0; i < failed; i++)
                {
                    var workItem = new WorkItem
                    {
                        Category = category,
                        QueuedTime = DateTime.UtcNow.AddMinutes(-30),
                        DequeueTime = DateTime.UtcNow.AddMinutes(-30).AddSeconds(q),
                        ProcessingStartTime = DateTime.UtcNow.AddMinutes(-29),
                        ProcessingCompleteTime = DateTime.UtcNow.AddMinutes(-29).AddMilliseconds(q * 10),
                    };
                    stats.AddFailed(workItem);
                }
            }

            var statistics = stats.ToString();
            
            // TODO: More intelligent verification
            Assert.IsNotNull(statistics);
        }
    }
}
