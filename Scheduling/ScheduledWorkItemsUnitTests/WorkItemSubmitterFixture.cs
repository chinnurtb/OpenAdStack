//-----------------------------------------------------------------------
// <copyright file="WorkItemSubmitterFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using ScheduledWorkItems;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace ScheduledWorkItemsUnitTests
{
    /// <summary>Tests for the WorkItemSubmitter</summary>
    [TestClass]
    public class WorkItemSubmitterFixture
    {
        /// <summary>Name of the store where work items are persisted</summary>
        private const string WorkItemStoreName = "workitems";

        /// <summary>The store containing the work items</summary>
        private IPersistentDictionary<WorkItem> workItemStore;

        /// <summary>Mock scheduled work item source</summary>
        private IScheduledWorkItemSource mockSource;

        /// <summary>Mock scheduled work item source provider</summary>
        private IScheduledWorkItemSourceProvider mockProvider;

        /// <summary>Mock queuer</summary>
        private IQueuer mockQueuer;

        /// <summary>Test logger</summary>
        private ILogger testLogger;

        /// <summary>Default work item used by the mocks</summary>
        private WorkItem workItem;

        /// <summary>
        /// Initialization for the test fixture.
        /// Sets configuraiton settings.
        /// </summary>
        /// <param name="context">Parameter not used.</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ConfigurationManager.AppSettings["Queue.WorkItemStoreName"] = WorkItemStoreName;
            ConfigurationManager.AppSettings["Scheduler.UpdateInterval"] = "5";
        }

        /// <summary>Clear the test storage and initialize the mocks before each test</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentStorage.Clear();
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
            PersistentDictionaryFactory.Initialize(new[] { new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Cloud) });

            this.workItemStore = PersistentDictionaryFactory.CreateDictionary<WorkItem>(WorkItemStoreName);

            this.workItem = new WorkItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = WorkItemStatus.None,
                Source = "MockSource",
                ResultType = WorkItemResultType.Shared,
                Content = "CONTENT",
                Result = "RESULT"
            };

            this.mockSource = MockRepository.GenerateMock<IScheduledWorkItemSource>();
            this.mockSource.Stub(f => f.Name).Return("MockSource");
            this.mockSource.Stub(f => f.CreateNewWorkItems());
            this.mockProvider = MockRepository.GenerateMock<IScheduledWorkItemSourceProvider>();
            this.mockProvider.Stub(f => f.CreateScheduledWorkItemSources()).Return(new[] { this.mockSource });
            this.mockQueuer = MockRepository.GenerateMock<IQueuer>();

            this.testLogger = new TestLogger();
            LogManager.Initialize(new[] { this.testLogger });
        }

        /// <summary>Test initializing a new work item submitter</summary>
        [TestMethod]
        public void InitializeWorkItemSubmitter()
        {
            var submitter = this.CreateWorkItemSubmitter();
            Assert.IsNotNull(submitter);
        }

        /// <summary>Test getting scheduled work items from sources</summary>
        [TestMethod]
        public void GetNewScheduledWorkItems()
        {
            var submitter = this.CreateWorkItemSubmitter();
            Assert.IsNotNull(submitter);

            submitter.CreateNewWorkItems();
            this.mockSource.AssertWasCalled(f => f.CreateNewWorkItems());
        }

        /// <summary>
        /// Creates an instance of WorkItemSubmitter, providing it with mocks for its dependencies.
        /// </summary>
        /// <returns>The created WorkItemSubmitter</returns>
        private WorkItemSubmitter CreateWorkItemSubmitter()
        {
            var providers = new[] { this.mockProvider };
            return new WorkItemSubmitter(providers, this.mockQueuer);
        }
    }
}
