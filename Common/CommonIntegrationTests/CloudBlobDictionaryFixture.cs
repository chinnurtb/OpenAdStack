//-----------------------------------------------------------------------
// <copyright file="CloudBlobDictionaryFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using AzureUtilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using TestUtilities;
using Utilities.Storage;

namespace CommonIntegrationTests
{
    /// <summary>Tests for the CloudBlobDictionary</summary>
    [TestClass]
    public class CloudBlobDictionaryFixture : PersistentDictionaryFixtureBase
    {
        /// <summary>Azure storage connection string</summary>
        private const string ConnectionString = "UseDevelopmentStorage=true";

        /// <summary>Cloud blob client used to cleanup after tests</summary>
        private static CloudBlobClient blobClient;

        /// <summary>Container used by the test</summary>
        private CloudBlobContainer container;

        /// <summary>Container address for the test</summary>
        private string containerAddress;

        /// <summary>Reinitialize azure emulated storage</summary>
        /// <param name="context">Parameter not used.</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Force Azure emulated storage to reinitialize (clears data)
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            var storageInitializerPath = ConfigurationManager.AppSettings["AzureStorageInitExe"];
            var storageEmulatorSqlInstance = ConfigurationManager.AppSettings["AzureStorageEmulatorSqlInstance"];
            AzureEmulatorHelper.StopStorageEmulator(emulatorRunnerPath);
            AzureEmulatorHelper.ClearEmulatedStorage(storageInitializerPath, storageEmulatorSqlInstance);
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);

            // Get a blob client to use cleaning up after tests
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        /// <summary>Sets the address of the container to be used for the test</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.containerAddress = Guid.NewGuid().ToString("N");
            this.container = blobClient.GetContainerReference(this.containerAddress);
            this.container.CreateIfNotExist();
            this.container.Delete();
        }

        /// <summary>Deletes the container used by the test</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            this.container.CreateIfNotExist();
            this.container.Delete();
        }

        /// <summary>Asserts the underlying store for the dictionary was created</summary>
        protected override void AssertPersistentStoreCreated()
        {
            Assert.IsFalse(this.container.CreateIfNotExist(), "Container was not created");
        }

        /// <summary>Asserts the value with the specified <paramref name="key"/> was persisted</summary>
        /// <param name="key">Key for the value</param>
        protected override void AssertValuePersisted(string key)
        {
            var blob = this.container.GetBlobReference(key);
            Assert.IsNotNull(blob);
            Assert.IsTrue(blob.DeleteIfExists(), "Blob did not exist");
        }

        /// <summary>Creates a new IPersistentDictionary for testing</summary>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The IPersistentDictionary</returns>
        /// <typeparam name="TValue">Entry type to create the dictionary for</typeparam>
        protected override IPersistentDictionary<TValue> CreateTestDictionary<TValue>(bool raw)
        {
            return new CloudBlobDictionary<TValue>(ConnectionString, this.containerAddress, raw);
        }
    }
}
