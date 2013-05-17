// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteLogsEngineFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using TestUtilities;
using Utilities.DeleteLogs;

namespace DeleteLogsIntegrationTests
{
    /// <summary>
    /// Test class for the DeleteLogsEngine
    /// </summary>
    [TestClass]
    public class DeleteLogsEngineFixture
    {
        /// <summary>
        /// blobClient to use for testing
        /// </summary>
        private static CloudBlobClient blobClient;

        /// <summary> Initialize the assembly </summary>
        /// <param name="context">the context</param>
        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext context)
        {
            // Force Azure emulated storage to start. DSService can still be running
            // but the emulated storage not available. The most reliable way to make sure
            // it's running and available is to stop it then start again.
            var emulatorRunnerPath = @"C:\Program Files\Microsoft SDKs\Windows Azure\Emulator\csrun.exe";
            AzureEmulatorHelper.StopStorageEmulator(emulatorRunnerPath);
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);

            // connect to storage to set up test
            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            blobClient = storageAccount.CreateCloudBlobClient();

            // Specify a retry backoff of 10 seconds max instead of using default values. 
            blobClient.RetryPolicy = RetryPolicies.RetryExponential(
                3, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 10), new TimeSpan(0, 0, 3));
        }

        /// <summary>Initialize Azure storage emulator and create log blobs used by tests.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // create a log container for testing
            var container = blobClient.GetContainerReference("testlogs");
            container.CreateIfNotExist();

            // add some log blobs to storage for the purposes of deleting them 
            for (var i = 0; i < 12; i++)
            {
                // TODO: the hours to subract (-54) depend on the app config (48). Make that dependency active.
                var logBlob = container.GetBlockBlobReference(
                    "/deployment17(63)/WorkerRole/deployment17(63).Azure.WorkerRole_IN_0/" +
                    DateTime.Now.AddHours(-54 + i).ToString("yyyyMMddHH", CultureInfo.InvariantCulture) +
                    ".0");

                logBlob.UploadText("test junk data");
            }
        }

        /// <summary>
        /// Test for the DeleteLogsEngine
        /// </summary>
        [TestMethod]
        public void DeleteLogsEngineTest()
        {
            // create the input
            var arguments = new DeleteLogsArgs();
            arguments.ConnectionString = "UseDevelopmentStorage=true";
            arguments.HoursAgoThresholdForDeleting = 48;
            arguments.LogContainers = "testlogs";

            var options = new BlobRequestOptions();
            options.UseFlatBlobListing = true;
            var container = blobClient.GetContainerReference("testlogs");

            var blobs = container.ListBlobs(options).ToList();
            Assert.AreEqual(12, blobs.Count);

            var deleteDate = DateTime.Now.AddHours(-1 * arguments.HoursAgoThresholdForDeleting)
                .ToString("yyyyMMddHH", CultureInfo.InvariantCulture);
            DeleteLogsEngine.DeleteLogs(arguments);

            blobs = container.ListBlobs(options).ToList();
            Assert.AreEqual(6, blobs.Count);

            foreach (var item in container.ListBlobs(options))
            {
                var blob = (CloudBlockBlob)item;
                var blobDate = blob.Name.Split('/').Last().Split('.').First();

                Assert.IsFalse(string.Compare(blobDate, deleteDate, StringComparison.OrdinalIgnoreCase) < 0);
            }
        }

        /// <summary>
        /// Clean up
        /// </summary>
        [TestCleanup]
        public void CleanUpTest()
        {   
            // delete the log container used for testing
            var container = blobClient.GetContainerReference("testlogs");
            container.Delete();
        }
    }
}
