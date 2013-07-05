// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActivitySubmitterFixture.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using Activities;
using ActivitySubmitter;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Storage.Testing;
using WorkItems;
using Constraints = Rhino.Mocks.Constraints;

namespace ActivityUnitTests
{
    /// <summary>Test fixture for Activity Submitter Implementation</summary>
    [TestClass]
    public class ActivitySubmitterFixture
    {
        /// <summary>Test thread</summary>
        private Thread testThread;

        /// <summary>Test logger</summary>
        private TestLogger testLogger;

        /// <summary>Test submitter name</summary>
        private string testSubmitterName;

        /// <summary>Test activity submitter</summary>
        private IActivitySubmitter testSubmitter;

        /// <summary>Mock IQueuer</summary>
        private IQueuer mockQueuer;

        /// <summary>Test completed workitems</summary>
        private IDictionary<string, WorkItem> testCompletedWorkItems;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["ActivitySubmitter.MaxIdleWait"] = "100";
            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            this.testSubmitterName = Guid.NewGuid().ToString("N");
            this.SetupMockQueuer();
        }

        /// <summary>Per-test cleanup</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            if (this.testThread != null &&
                this.testThread.ThreadState == ThreadState.Running)
            {
                this.testThread.Abort();
                this.testThread = null;
            }

            if (this.testSubmitter != null && this.testSubmitter is IDisposable)
            {
                ((IDisposable)this.testSubmitter).Dispose();
                this.testSubmitter = null;
                GC.Collect();
            }

            if (this.testLogger != null)
            {
                Assert.AreEqual(0, this.testLogger.ErrorEntries.Count(), string.Join("\n", this.testLogger.ErrorEntries));
                Assert.AreEqual(0, this.testLogger.WarningEntries.Count(), string.Join("\n", this.testLogger.WarningEntries));
            }
        }

        /// <summary>
        /// Test creating an instance of the ActivitySubmitter implementation and shutdown it.
        /// The ActivityWorkItemSubmitter runs a background thread which is supposed to cleanly
        /// exit when it's wait handle is signaled as the instance is disposed.
        /// </summary>
        [TestMethod]
        public void CreateAndDisposeActivitySubmitter()
        {
            this.RunInTestThread(
                () =>
                {
                    var submitter = (this.testSubmitter = this.CreateSubmitter()) as ActivityWorkItemSubmitter;
                    Assert.IsNotNull(submitter);
                    Thread.Sleep(500);
                    submitter.Dispose();
                    submitter = null;
                    GC.Collect();
                },
                2000);
        }

        /// <summary>
        /// Test submitting a request and waiting for the result
        /// </summary>
        [TestMethod]
        public void SubmitAndWait()
        {
            var request = new ActivityRequest { Task = "TestTask" };

            this.testSubmitter = this.CreateSubmitter();

            var result = this.testSubmitter.SubmitAndWait(
                request, ActivityRuntimeCategory.InteractiveFetch, 1000);
            Assert.IsNotNull(result);
            Assert.AreEqual(request.Id, result.RequestId);
            Assert.IsTrue(result.Succeeded);
        }

        /// <summary>
        /// Test submitting a request with a callback
        /// </summary>
        [TestMethod]
        public void SubmitWithCallback()
        {
            ActivityResult result = null;
            var waitHandle = new AutoResetEvent(false);
            var request = new ActivityRequest { Task = "TestTask" };

            this.testSubmitter = this.CreateSubmitter();

            Assert.IsTrue(this.testSubmitter.SubmitWithCallback(
                request, ActivityRuntimeCategory.InteractiveFetch, r => { result = r; waitHandle.Set(); }));
            Assert.IsTrue(waitHandle.WaitOne(500));
            Assert.IsNotNull(result);
            Assert.AreEqual(request.Id, result.RequestId);
            Assert.IsTrue(result.Succeeded);
        }

        /// <summary>
        /// Creates an instance of the activity submitter implementation
        /// </summary>
        /// <returns>The activity submitter instance</returns>
        private IActivitySubmitter CreateSubmitter()
        {
            return new ActivityWorkItemSubmitter("TestSubmitter", this.mockQueuer);
        }

        /// <summary>
        /// Run the thread proc in the test thread and assert it completes before the timeout
        /// </summary>
        /// <param name="threadProc">Thread process</param>
        /// <param name="timeout">How long to wait</param>
        private void RunInTestThread(ThreadStart threadProc, int timeout)
        {
            this.testThread = new Thread(threadProc);
            this.testThread.Start();
            Assert.IsTrue(this.testThread.Join(timeout));
            Assert.AreEqual(ThreadState.Stopped, this.testThread.ThreadState);
        }

        /// <summary>Setup the queuer mock</summary>
        private void SetupMockQueuer()
        {
            this.testCompletedWorkItems = new Dictionary<string, WorkItem>();
            this.mockQueuer = MockRepository.GenerateMock<IQueuer>();
            this.mockQueuer.Stub(f => f.DequeueProcessedWorkItems(
                Arg<WorkItemResultType>.Is.Anything, Arg<string>.Is.Anything, Arg<int>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    call.ReturnValue = this.testCompletedWorkItems.Values.ToArray();
                });

            this.mockQueuer.Stub(f => f.RemoveFromQueue(Arg<WorkItem>.Is.Anything))
                .WhenCalled(call =>
                {
                    var workItem = call.Arguments[0] as WorkItem;
                    this.testCompletedWorkItems.Remove(workItem.Id);
                });

            // Scope for workItem variable used in capture constraint
            {
                WorkItem workItem = null;
                var workItemCaptureConstraint = new TestUtilities.LambdaConstraint<WorkItem>(wi => { workItem = wi; return true; });
                this.mockQueuer.Stub(f => f.EnqueueWorkItem(ref Arg<WorkItem>.Ref(workItemCaptureConstraint, workItem).Dummy))
                    .Return(true)
                    .WhenCalled(call =>
                    {
                        workItem.Result = new ActivityResult { RequestId = workItem.Id, Succeeded = true }.SerializeToXml();
                        workItem.Status = WorkItemStatus.Processed;
                        this.testCompletedWorkItems.Add(workItem.Id, workItem);
                    });
            }
        }
    }
}
