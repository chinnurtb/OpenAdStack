//-----------------------------------------------------------------------
// <copyright file="ActivityProcessorTestFixture.cs" company="Rare Crowds Inc">
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
using Activities;
using ActivityProcessor;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using TestUtilities;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace ActivityUnitTests
{
    /// <summary>
    /// Tests for ActivityProcessor
    /// </summary>
    [TestClass]
    public class ActivityProcessorTestFixture
    {
        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            LogManager.Initialize(new[] { new TestLogger() });
            SimulatedPersistentStorage.Clear();
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
            PersistentDictionaryFactory.Initialize(new[]
            {
                new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Cloud),
                new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Sql)
            });
        }

        /// <summary>Test for activity processor</summary>
        [TestMethod]
        public void CreateWithValidActivityProvider()
        {
            var activityProvider = MockRepository.GenerateMock<IActivityProvider>();
            activityProvider.Stub(f => f.ActivityContext)
                .Return(new Dictionary<Type, object> { { typeof(string), "context" } });
            activityProvider.Stub(f => f.ActivityTypes)
                .Return(new[] { typeof(TestHelpers.ValidRequiredValuesActivity) });
            var processor = new ActivityWorkItemProcessor(
                new[] { activityProvider },
                null);
            Assert.IsNotNull(processor);
        }

        /// <summary>
        /// Test for processing requests submitted by an activity
        /// </summary>
        [TestMethod]
        public void ProcessRequestWithActivitySource()
        {
            var activityProvider = MockRepository.GenerateMock<IActivityProvider>();
            activityProvider.Stub(f => f.ActivityContext)
                .Return(new Dictionary<Type, object> { { typeof(string), "context" } });
            activityProvider.Stub(f => f.ActivityTypes)
                .Return(new[] { typeof(TestHelpers.ValidActivity), typeof(TestHelpers.HandlesResultActivity) });

            var request = new ActivityRequest
            {
                Task = "ValidActivity",
                Values =
                {
                    { "TestValue", Guid.NewGuid().ToString("N") }
                }
            };

            var workItem = new WorkItem
            {
                Id = request.Id,
                Content = request.SerializeToXml(),
                Status = WorkItemStatus.InProgress,
                Source = "HandlesResultActivity",
                ResultType = WorkItemResultType.Direct
            };

            var processor = new ActivityWorkItemProcessor(
                new[] { activityProvider },
                null);
            Assert.IsNotNull(processor);

            processor.ProcessWorkItem(ref workItem);
            Assert.AreEqual(WorkItemStatus.Completed, workItem.Status);

            var handledResult = TestHelpers.HandlesResultActivity.LastHandledResult;
            Assert.IsNotNull(handledResult);
            
            var result = ActivityResult.DeserializeFromXml(workItem.Result);
            Assert.AreEqual(request.Values["TestValue"], result.Values["TestValue"]);
            Assert.AreEqual(result.Values["TestValue"], handledResult.Values["TestValue"]);
        }
    }
}
