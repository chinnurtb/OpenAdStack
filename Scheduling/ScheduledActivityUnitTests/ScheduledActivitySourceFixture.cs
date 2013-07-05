//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySourceFixture.cs" company="Rare Crowds Inc">
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
using System.Reflection;
using System.Threading;
using Activities;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using ScheduledActivities;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace ScheduledActivityUnitTests
{
    /// <summary>Tests for ScheduledActivitySource</summary>
    [TestClass]
    public class ScheduledActivitySourceFixture
    {
        /// <summary>Mock IQueuer for use by the test activity sources</summary>
        private IQueuer mockQueuer;

        /// <summary>Mock IDequeuer for use by the test activity sources</summary>
        private IDequeuer mockDequeuer;

        /// <summary>Mock ILogger for use by the LogManager</summary>
        private ILogger mockLogger;

        /// <summary>Dictionary of work items submitted via the mock queuer</summary>
        private IDictionary<string, IList<WorkItem>> workItems;

        /// <summary>The last work item enqueued via the mock queuer</summary>
        private WorkItem lastEnqueuedWorkItem;

        /// <summary>Delegate for mocking IQueuer.EnqueueWorkItem</summary>
        /// <param name="workItem">The work item being enqueued</param>
        private delegate void MockEnqueueWorkItem(ref WorkItem workItem);

        /// <summary>Rhino mock callback for IQueuer.EnqueueWorkItem</summary>
        /// <param name="workItem">The work item being enqueued</param>
        /// <returns>Whether the call is valid</returns>
        private delegate bool MockEnqueueWorkItemCallback(ref WorkItem workItem);

        /// <summary>Initialize mocks, etc</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.workItems = new Dictionary<string, IList<WorkItem>>();
            this.lastEnqueuedWorkItem = null;

            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
            PersistentDictionaryFactory.Initialize(new[] { new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Cloud) });

            this.mockLogger = MockRepository.GenerateMock<ILogger>();
            LogManager.Initialize(new[] { this.mockLogger });

            this.mockQueuer = MockRepository.GenerateMock<IQueuer>();
            this.mockQueuer.Stub(x => x.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), null).Dummy))
                .Callback((MockEnqueueWorkItemCallback)delegate(ref WorkItem workItem)
                {
                    if (workItem == null)
                    {
                        return false;
                    }

                    this.lastEnqueuedWorkItem = workItem;
                    workItem.Status = WorkItemStatus.Pending;

                    if (!this.workItems.ContainsKey(workItem.Category))
                    {
                        this.workItems[workItem.Category] = new List<WorkItem>();
                    }
                    
                    this.workItems[workItem.Category].Add(workItem);

                    return true;
                })
                .WhenCalled(call =>
                {
                    call.Arguments[0] = this.lastEnqueuedWorkItem;
                });

            this.mockDequeuer = MockRepository.GenerateMock<IDequeuer>();
            this.mockDequeuer.Stub(x => x.EnqueueProcessedWorkItem(Arg<WorkItem>.Is.Anything))
                .WhenCalled(call =>
                {
                    var workItem = call.Arguments[0] as WorkItem;
                    if (!this.workItems.ContainsKey(workItem.Category))
                    {
                        this.workItems[workItem.Category] = new List<WorkItem>();
                    }

                    this.workItems[workItem.Category].Add(workItem);
                })
                .Return(true);
        }

        /// <summary>Test creating a valid activity source</summary>
        [TestMethod]
        public void CreateValidSource()
        {
            var source = new TestSources.ValidActivitySource();
            Assert.IsNotNull(source);
        }

        /// <summary>Test creating an activity source missing the SourceNameAttribute</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateInvalidSourceMissingSourceName()
        {
            new TestSources.MissingSourceNameActivitySource();
        }

        /// <summary>Test creating an activity source missing the SourceNameAttribute</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateInvalidSourceMissingSchedule()
        {
            new TestSources.MissingScheduleActivitySource();
        }

        /// <summary>Test creating an activity source missing the SourceNameAttribute</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void CreateInvalidSourceMissingSourceNameAndSchedule()
        {
            new TestSources.MissingSourceNameAndScheduleActivitySource();
        }

        /// <summary>Test creating an activity source missing the scheduleArgs</summary>
        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void CreateInvalidSourceMissingScheduleArgs()
        {
            new TestSources.MissingScheduleArgsActivitySource();
        }

        /// <summary>Test creating an activity source with invalid scheduleArgs</summary>
        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void CreateInvalidSourceInvalidScheduleArgs()
        {
            new TestSources.InvalidScheduleArgsActivitySource();
        }

        /// <summary>Test creating an activity source with invalid scheduleArgs</summary>
        [TestMethod]
        [ExpectedException(typeof(TargetInvocationException))]
        public void CreateInvalidSourceInvalidScheduleArgValue()
        {
            new TestSources.InvalidScheduleArgValueActivitySource();
        }

        /// <summary>Test basic scheduling</summary>
        [TestMethod]
        public void RunOnSchedule()
        {
            var source = this.CreateScheduledActivitySource<TestSources.ValidActivitySource>();

            // Test that a request is created the first time
            source.CreateNewWorkItems();
            var nextTime = source.Schedule.GetNextTime(DateTime.UtcNow);
            Assert.IsNotNull(this.lastEnqueuedWorkItem);
            this.lastEnqueuedWorkItem = null;

            // Test that another request is NOT created until the next scheduled time.
            if (nextTime < DateTime.UtcNow)
            {
                Assert.Inconclusive(
                    "Scheduled interval ellapsed before testing that a new" +
                    "request is not created before the next scheduled time");
            }
            
            source.CreateNewWorkItems();
            Assert.IsNull(this.lastEnqueuedWorkItem);

            // Test that a new request IS created after the next scheduled time.
            int millisecondsUntilNextTime = (int)(nextTime - DateTime.UtcNow).TotalMilliseconds;
            if (millisecondsUntilNextTime > 0)
            {
                Thread.Sleep(millisecondsUntilNextTime);
            }

            source.CreateNewWorkItems();
            Assert.IsNotNull(this.lastEnqueuedWorkItem);
        }

        /// <summary>
        /// Test handling a successfully processed activity response
        /// </summary>
        [TestMethod]
        public void HandleSubmittedProcessedWorkItem()
        {
            var source = this.CreateScheduledActivitySource<TestSources.ValidActivitySource>();

            // Create a successfully processed work item
            var workItem = this.CreateProcessedWorkItem(source.Name);
            var expectedResult = ActivityResult.DeserializeFromXml(workItem.Result);
            this.mockDequeuer.EnqueueProcessedWorkItem(workItem);

            // Process the processed work item
            source.OnWorkItemProcessed(workItem);

            var handledResult = ((TestSources.ValidActivitySource)source).LastResult;
            Assert.IsNotNull(handledResult);
            Assert.AreEqual(expectedResult.Task, handledResult.Task);
            Assert.AreEqual(expectedResult.Values.Count, handledResult.Values.Count);
            foreach (var pair in expectedResult.Values.Zip(handledResult.Values))
            {
                Assert.AreEqual(pair.Item1.Key, pair.Item2.Key);
                Assert.AreEqual(pair.Item1.Value, pair.Item2.Value);
            }
        }

        /// <summary>
        /// Test handling a failed activity
        /// </summary>
        [TestMethod]
        public void HandleSubmittedFailedActivity()
        {
            var source = this.CreateScheduledActivitySource<TestSources.ValidActivitySource>();

            // Create a successfully processed work item
            var workItem = this.CreateProcessedWorkItem(source.Name);
            var request = ActivityRequest.DeserializeFromXml(workItem.Content);
            var failedResult = new ActivityResult
            {
                Task = request.Task,
                Succeeded = false,
                Error =
                {
                    ErrorId = new Random().Next(),
                    Message = Guid.NewGuid().ToString()
                }
            };
            workItem.Status = WorkItemStatus.Failed;
            workItem.Result = failedResult.SerializeToXml();

            // Process the processed work item
            source.OnWorkItemProcessed(workItem);

            var handledResult = ((TestSources.ValidActivitySource)source).LastResult;
            Assert.IsNotNull(handledResult);
            Assert.AreEqual(request.Task, handledResult.Task);
            Assert.IsFalse(handledResult.Succeeded);
            Assert.IsNotNull(handledResult.Error);
            Assert.AreEqual(failedResult.Error.ErrorId, handledResult.Error.ErrorId);
            Assert.AreEqual(failedResult.Error.Message, handledResult.Error.Message);
        }

        /// <summary>Creates a new scheduled activity source instance using mocks</summary>
        /// <typeparam name="TSourceType">Type of the scheduled activity source</typeparam>
        /// <returns>The created scheduled activity source instance</returns>
        private TSourceType CreateScheduledActivitySource<TSourceType>()
            where TSourceType : ScheduledActivitySource
        {
            return (TSourceType)ScheduledActivitySource.Create(
                typeof(TSourceType),
                null,
                this.mockQueuer);
        }

        /// <summary>
        /// Creates a successfully processed work item and stubs IQueuer.CheckWorkItem
        /// to return it when called with the created work item's id.
        /// </summary>
        /// <param name="sourceName">Source name to use</param>
        /// <returns>The created work item</returns>
        private WorkItem CreateProcessedWorkItem(string sourceName)
        {
            var request = new ActivityRequest
            {
                Task = Guid.NewGuid().ToString(),
                Values = { { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() } }
            };
            var result = new ActivityResult
            {
                Task = request.Task,
                Succeeded = true,
                Values = { { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() } }
            };
            var workItem = new WorkItem
            {
                Id = request.Id,
                Category = Guid.NewGuid().ToString("N").Substring(0, 10),
                Source = sourceName,
                ResultType = WorkItemResultType.Shared,
                Status = WorkItemStatus.Processed,
                Content = request.SerializeToXml(),
                Result = result.SerializeToXml(),
                QueuedTime = DateTime.UtcNow.AddMinutes(-4),
                DequeueTime = DateTime.UtcNow.AddMinutes(-3),
                ProcessingStartTime = DateTime.UtcNow.AddMinutes(-2),
                ProcessingCompleteTime = DateTime.UtcNow.AddMinutes(-1)
            };
            this.mockQueuer.Stub(f =>
                f.CheckWorkItem(Arg.Is(workItem.Id)))
                .Return(workItem);
            return workItem;
        }
    }
}
