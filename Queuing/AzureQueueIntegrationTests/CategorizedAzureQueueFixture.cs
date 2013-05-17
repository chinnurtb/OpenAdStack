//-----------------------------------------------------------------------
// <copyright file="CategorizedAzureQueueFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Queuing;
using Queuing.Azure;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace AzureQueueIntegrationTests
{
    /// <summary>
    /// is a test class for CategorizedAzureQueueTest and is intended
    /// contain all CategorizedAzureQueueTest Unit Tests
    /// </summary>
    [TestClass]
    public class CategorizedAzureQueueFixture
    {
        /// <summary>
        /// Storage account used for the unit tests
        /// </summary>
        private static CloudStorageAccount testStorageAccount;

        /// <summary>
        /// The queue that the test will be using
        /// </summary>
        private CloudQueue testQueue;

        /// <summary>
        /// Name of the queue the test will be using
        /// </summary>
        private string testQueueName;

        /// <summary>
        /// The category value of the queue the test will be using
        /// </summary>
        private string testQueueCategory;

        /// <summary>
        /// Setup the test storage account and container address for this run
        /// </summary>
        /// <param name="testContext">Test Context (unused)</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            testStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
        }

        /// <summary>
        /// Drain the queue before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();

            SimulatedPersistentStorage.Clear();
            PersistentDictionaryFactory.Initialize(new[]
            {
                new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Cloud),
                new SimulatedPersistentDictionaryFactory(PersistentDictionaryType.Sql)
            });
            this.testQueueCategory = "Test({0})".FormatInvariant(Guid.NewGuid().ToString("N").Substring(0, 10));
            this.testQueueName = "workitemqueue-{0}".FormatInvariant(this.testQueueCategory);
            this.testQueue = testStorageAccount.CreateCloudQueueClient().GetQueueReference(this.testQueueName);
        }

        /// <summary>
        /// Delete the queue when done
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                this.testQueue.Delete();
            }
            catch (StorageClientException)
            {
                // Thrown if the queue didn't exist
            }
        }

        /// <summary>
        /// test for Dequeue
        /// </summary>
        [TestMethod]
        public void DequeueMultipleTest()
        {
            CategorizedAzureQueue target = new CategorizedAzureQueue();
            int maxEntries = 10;
            WorkItemQueueEntry expected = new WorkItemQueueEntry { Category = this.testQueueCategory, WorkItemId = Guid.NewGuid().ToString("N") };
            WorkItemQueueEntry[] actual;
            target.Enqueue(expected);

            actual = target.Dequeue(this.testQueueCategory, maxEntries);
            Assert.IsNotNull(actual);
            Assert.IsTrue(actual.Length > 0);
            Assert.IsNotNull(actual[0]);
            Assert.AreEqual(expected.WorkItemId, actual[0].WorkItemId);
        }
    }
}
