//-----------------------------------------------------------------------
// <copyright file="ScheduledActivitySourceFixtureBase.cs" company="Rare Crowds Inc">
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
using Activities;
using ConfigManager;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using ScheduledActivities;
using ScheduledWorkItems;
using TestUtilities;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace ScheduledActivityDispatchersTestHelpers
{
    /// <summary>Base class for scheduled activity source test fixtures</summary>
    /// <typeparam name="TActivitySource">Type of the scheduled activity source being tested</typeparam>
    [TestClass]
    public abstract class ScheduledActivitySourceFixtureBase<TActivitySource>
        where TActivitySource : ScheduledActivitySource
    {
        /// <summary>The time offset to UtcNow for the time returned by MockSchedule.NextTime</summary>
        private TimeSpan timeUntilNextScheduled;

        /// <summary>Work item being enqueued via the mock</summary>
        private WorkItem enqueuingWorkItem;

        /// <summary>Gets the work items queued with the mock queuer</summary>
        protected IList<WorkItem> QueuedWorkItems { get; private set; }

        /// <summary>Gets the mock schedule</summary>
        protected IActivitySourceSchedule MockSchedule { get; private set; }

        /// <summary>Gets the mock queuer</summary>
        protected IQueuer MockQueuer { get; private set; }

        /// <summary>Gets the test system user auth id</summary>
        protected string SystemAuthUserId
        {
            get { return Config.GetValue("System.AuthUserId"); }

            private set { ConfigurationManager.AppSettings["System.AuthUserId"] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether MockLogger.NextTime should be in the past
        /// </summary>
        protected bool ScheduledNow
        {
            get { return this.timeUntilNextScheduled.Ticks < 0; }

            set { this.timeUntilNextScheduled = new TimeSpan(value ? -1 : 1); }
        }

        /// <summary>Setup mocks</summary>
        [TestInitialize]
        public virtual void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
            LogManager.Initialize(new[] { new TestLogger() });
            this.SystemAuthUserId = Guid.NewGuid().ToString("N");
            this.ScheduledNow = false;
            this.InitializeMockQueuer();
            this.InitializeMockSchedule();
        }

        /// <summary>Test creating an instance of the activity source</summary>
        [TestMethod]
        public virtual void Create()
        {
            var source = this.CreateActivitySource();
            Assert.IsNotNull(source);
            Assert.IsInstanceOfType(source, typeof(TActivitySource));
        }

        /// <summary>
        /// Verify that the values of the actual ActivityRequest match
        /// the expected ActivityRequest.
        /// </summary>
        /// <param name="expected">The activity request containing expected values</param>
        /// <param name="actual">The actual activity request</param>
        /// <exception cref="System.ArgumentNullException">
        /// The expected ActivityRequest is null.
        /// </exception>
        protected static void VerifyActivityRequest(ActivityRequest expected, ActivityRequest actual)
        {
            if (expected == null)
            {
                throw new ArgumentNullException("expected");
            }

            Assert.IsNotNull(actual, "ActivityRequest was null");
            Assert.AreEqual(expected.Task, actual.Task, "ActivityRequest had the wrong Task");

            if (expected.Values == null)
            {
                Assert.IsNull(actual.Values, "ActivityRequest had values when none were expected");
            }
            else
            {
                Assert.AreEqual(expected.Values.Count, actual.Values.Count, "ActivityRequest did not have the correct number of values");
                foreach (var value in expected.Values)
                {
                    Assert.IsTrue(actual.Values.ContainsKey(value.Key), string.Format(CultureInfo.InvariantCulture, "ActivityRequest did not have a value for '{0}'", value.Key));

                    Assert.AreEqual(value.Value, actual.Values[value.Key], string.Format(CultureInfo.InvariantCulture, "ActivityRequest did not have the correct value for '{0}'", value.Key));
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the activity source to test and swap out
        /// its schedule with the mock schedule
        /// </summary>
        /// <returns>The created activity source</returns>
        protected TActivitySource CreateActivitySource()
        {
            var source = (TActivitySource)ScheduledActivitySource.Create(
                typeof(TActivitySource),
                null,
                this.MockQueuer);
            source.Schedule = this.MockSchedule;
            return source;
        }

        /// <summary>Initialize the mock IQueuer</summary>
        protected virtual void InitializeMockQueuer()
        {
            this.QueuedWorkItems = new List<WorkItem>();
            var workItemCaptureConstraint = new LambdaConstraint<WorkItem>(wi =>
            {
                this.enqueuingWorkItem = wi;
                return true;
            });

            this.MockQueuer = MockRepository.GenerateMock<IQueuer>();
            this.MockQueuer.Stub(q => q.EnqueueWorkItem(ref Arg<WorkItem>.Ref(workItemCaptureConstraint, null).Dummy))
                .Return(true)
                .WhenCalled(call =>
                {
                    this.enqueuingWorkItem.Status = WorkItemStatus.Pending;
                    this.QueuedWorkItems.Add(this.enqueuingWorkItem);
                    call.Arguments[0] = this.enqueuingWorkItem;
                });
        }

        /// <summary>Initialize the mock schedule</summary>
        protected virtual void InitializeMockSchedule()
        {
            this.MockSchedule = MockRepository.GenerateMock<IActivitySourceSchedule>();
            this.MockSchedule.Stub(f => f.GetNextTime(Arg<DateTime>.Is.Anything)).Return(DateTime.UtcNow)
                .WhenCalled(call =>
                {
                    call.ReturnValue = DateTime.UtcNow + this.timeUntilNextScheduled;
                });
        }
    }
}
