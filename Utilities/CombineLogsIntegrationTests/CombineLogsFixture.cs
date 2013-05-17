// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CombineLogsFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using TestUtilities;
using Utilities.CombineLogs;

namespace CombineLogsIntegrationTests
{
    /// <summary>
    /// Test class for the DeleteLogsEngine
    /// </summary>
    [TestClass]
    public class CombineLogsFixture
    {
        /// <summary>
        /// blobClient to use for testing
        /// </summary>
        private static CloudBlobClient blobClient;

        /// <summary>
        /// Dummy log message template to be used to create logs for testing
        /// </summary>
        private static string logTemplate = "<msg time=\"{0}\" lvl=\"Information\" inst=\"5c2f51d015b94f94a3423b32f93e0907/WorkerRole_IN_0\" thrd=\"Role Initialization Thread\" src=\"WorkerRole.AzureWorkerRole.OnStart()\" stk=\"at WorkerRole.AzureWorkerRole.OnStart()\nat Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.InitializeRoleInternal(RoleType roleTypeEnum)\nat Microsoft.WindowsAzure.ServiceRuntime.Implementation.Loader.RoleRuntimeBridge.&lt;InitializeRole&gt;b__0()\nat System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean ignoreSyncCtx)\nat System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state)\nat System.Threading.ThreadHelper.ThreadStart()\"><![CDATA[\nLogManager initialized with 3 ILoggers: Diagnostics.QuotaLogger, Diagnostics.TraceLogger, Utilities.Diagnostics.MailAlertLogger\n]]></msg>";

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
            for (var k = 0; k < 2; k++)
            {
                // create log containers for testing
                var container = blobClient.GetContainerReference("testlogs" + k);
                container.CreateIfNotExist();

                // add some log blobs to storage for the purposes of combining them 
                for (var i = 0; i < 12; i++)
                {
                    var logBlob = container.GetBlockBlobReference(
                        "/deployment17(63)/WorkerRole/deployment17(63).Azure.WorkerRole_IN_0/" +
                        DateTime.UtcNow.Date.AddHours(-54 + i).ToString("yyyyMMddHH", CultureInfo.InvariantCulture) +
                        ".0");

                    // add some log messages with different times
                    var sb = new StringBuilder();
                    for (var j = 0; j < 12; j++) 
                    {
                        sb.Append(string.Format(logTemplate, DateTime.UtcNow.Date.AddHours(-54 + i).AddMinutes(j).ToString("o", CultureInfo.InvariantCulture)));
                    }

                    logBlob.UploadText(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Test for the DeleteLogsEngine
        /// </summary>
        [TestMethod]
        public void CombineLogsEngineTest()
        {
            // create the input
            var arguments = new CombineLogsArgs();
            arguments.ConnectionString = "UseDevelopmentStorage=true";
            arguments.StartDate = DateTime.UtcNow.Date.AddHours(-54);
            arguments.EndDate = DateTime.UtcNow.Date.AddHours(-45);
            arguments.LogContainers = "testlogs0,testlogs1";
            arguments.OutputFile = new FileInfo(arguments.StartDate.ToString("yyyyMMddHH", CultureInfo.InvariantCulture) + ".txt");

            var combinedLog = CombineLogsEngine.CombineLogs(arguments);

            var elementDates = combinedLog
                .Root
                .Elements()
                .Select(msg => DateTime.Parse(msg.Attribute("time").Value, CultureInfo.InvariantCulture))
                .ToList();

            // Assert the elements are in increasing time order
            Assert.IsTrue(elementDates.SequenceEqual(elementDates.OrderBy(date => date).ToList()));
           
            // Assert the correct number appear 
            // (2 log containers, 12 log messages per log blob, 1 log blob per hour, 10 hours between start and end date)
            Assert.AreEqual(2 * 12 * 10, elementDates.Count);
        }

        /// <summary>
        /// Clean up
        /// </summary>
        [TestCleanup]
        public void CleanUpTest()
        {
            for (var i = 0; i < 2; i++)
            {
                // delete the log container used for testing
                var container = blobClient.GetContainerReference("testlogs" + i);
                container.Delete();
            }
        }
    }
}
