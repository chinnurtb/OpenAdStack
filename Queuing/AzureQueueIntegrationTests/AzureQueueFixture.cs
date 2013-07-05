//-----------------------------------------------------------------------
// <copyright file="AzureQueueFixture.cs" company="Rare Crowds Inc">
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
using System.Threading;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Queuing.Azure;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace AzureQueueIntegrationTests
{
    /// <summary>
    /// This is a test class for AzureQueueTest and is intended
    /// to contain all AzureQueueTest Unit Tests
    /// </summary>
    [TestClass]
    public class AzureQueueFixture
    {
        /// <summary>Random number generator instance</summary>
        private static readonly Random R = new Random();

        /// <summary>Storage account used for the unit tests</summary>
        private static CloudStorageAccount testStorageAccount;

        /// <summary>Name of the queue used for testing</summary>
        private string testQueueName;

        /// <summary>Diagnostic logger used for testing</summary>
        private TestLogger testLogger;

        /// <summary>
        /// Gets the queue that the tests will be using
        /// </summary>
        private CloudQueue TestQueue
        {
            get
            {
                CloudQueueClient queueClient = testStorageAccount.CreateCloudQueueClient();
                return queueClient.GetQueueReference(this.testQueueName);
            }
        }

        /// <summary>
        /// Setup the test storage account and container address for this run
        /// </summary>
        /// <param name="testContext">Test Context (unused)</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            testStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
        }

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            LogManager.Initialize(new[] { this.testLogger = new TestLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            this.testQueueName = "testqueue-{0}".FormatInvariant(R.Next());
        }

        /// <summary>Drain and delete the test queue after each test</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            this.TestQueue.Delete();
        }

        /// <summary>A test for AzureQueue constructor</summary>
        [TestMethod]
        public void AzureQueueConstructorTest()
        {
            var queue = this.CreateTestQueue();

            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.IsNotNull(queue);
            Assert.IsFalse(this.TestQueue.CreateIfNotExist());
        }

        /// <summary>Test for Dequeue</summary>
        [TestMethod]
        public void DequeueTest()
        {
            var queue = this.CreateTestQueue();
            var expected = CreateTestEntry();

            queue.Enqueue(expected);
            var actual = queue.Dequeue(1);

            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(1, actual.Length);
            Assert.AreEqual(expected.WorkItemId, actual[0].WorkItemId);
            Assert.AreEqual(expected.Category, actual[0].Category);
        }

        /// <summary>Test for dequeuing multiple entries</summary>
        [TestMethod]
        public void DequeueMultipleTest()
        {
            const int MaxEntries = 10;
            var expected = Enumerable.Range(0, MaxEntries)
                .Select(i => CreateTestEntry())
                .ToArray();
            var queue = this.CreateTestQueue();
            foreach (var entry in expected)
            {
                queue.Enqueue(entry);
            }

            var actual = queue.Dequeue(MaxEntries);

            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.IsNotNull(actual);
            Assert.AreEqual(MaxEntries, actual.Length);
            Assert.IsTrue(expected.All(entry => null !=
                actual.Where(e =>
                    e.WorkItemId == entry.WorkItemId && e.Category == entry.Category)
                .SingleOrDefault()));
        }

        /// <summary>Test for Enqueue</summary>
        [TestMethod]
        public void EnqueueTest()
        {
            var queue = this.CreateTestQueue();
            var expected = CreateTestEntry();
            queue.Enqueue(expected);
            var dequeued = queue.Dequeue(10);

            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(1, dequeued.Length);
            Assert.AreEqual(expected.WorkItemId, dequeued[0].WorkItemId);
            Assert.AreEqual(expected.Category, dequeued[0].Category);
        }

        /// <summary>Test enqueuing to a deleted queue</summary>
        [TestMethod]
        public void EnqueueToDeletedQueueTest()
        {
            var queue = this.CreateTestQueue();
            var expected = CreateTestEntry();

            this.TestQueue.Delete();
            queue.Enqueue(expected);
            var dequeued = queue.Dequeue(10);

            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(1, dequeued.Length);
            Assert.AreEqual(expected.WorkItemId, dequeued[0].WorkItemId);
            Assert.AreEqual(expected.Category, dequeued[0].Category);
        }

        /// <summary>Test for Dequeue</summary>
        [TestMethod]
        public void DequeueFromDeletedQueueTest()
        {
            var queue = this.CreateTestQueue();
            var expected = CreateTestEntry();

            queue.Enqueue(expected);
            this.TestQueue.Delete();
            var dequeued = queue.Dequeue(10);

            Assert.AreEqual(0, this.testLogger.ErrorEntries.Count());
            Assert.AreEqual(0, dequeued.Length);
        }

        /// <summary>Test attempting to delete an entry that does not exist from the queue</summary>
        [TestMethod]
        public void DeleteNonexistentEntryFromQueue()
        {
            var queue = this.CreateTestQueue();
            var entry = CreateTestEntry();
            queue.Delete(entry);
            Assert.AreEqual(1, this.testLogger.WarningEntries.Count());
        }

        /// <summary>Test re-dequeueing an entry after the visibility timeout has lapsed</summary>
        [TestMethod]
        public void DequeueAfterVisibilityTimeout()
        {
            ConfigurationManager.AppSettings["Queue.VisibilityTimeout"] = "00:00:01";
            var queue = this.CreateTestQueue();
            var expected = CreateTestEntry();
            queue.Enqueue(expected);

            var firstDequeue = queue.Dequeue(10);
            var secondDequeue = queue.Dequeue(10);
            Assert.AreEqual(1, firstDequeue.Length);
            Assert.AreEqual(0, secondDequeue.Length);

            Thread.Sleep(1100);

            var thirdDequeue = queue.Dequeue(10);
            Assert.AreEqual(1, thirdDequeue.Length);
        }

        /// <summary>Creates a unique work item queue entry for testing</summary>
        /// <returns>The test work item queue entry</returns>
        private static WorkItemQueueEntry CreateTestEntry()
        {
            return new WorkItemQueueEntry
            {
                WorkItemId = Guid.NewGuid().ToString(),
                Category = Guid.NewGuid().ToString()
            };
        }

        /// <summary>Creates an AzureQueue instance for testing</summary>
        /// <returns>The test AzureQueue instance</returns>
        private AzureQueue CreateTestQueue()
        {
            return new AzureQueue(testStorageAccount, this.testQueueName);
        }

        /// <summary>Drain the test queue</summary>
        private void DrainTestQueue()
        {
            CloudQueueMessage message = null;
            while (true)
            {
                try
                {
                    message = this.TestQueue.GetMessage();
                }
                catch (StorageClientException)
                {
                    // Thrown if the queue doesn't exist yet
                    break;
                }

                if (message == null)
                {
                    break;
                }
                else
                {
                    this.TestQueue.DeleteMessage(message);
                }
            }
        }
    }
}
