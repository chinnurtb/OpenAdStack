//-----------------------------------------------------------------------
// <copyright file="QuotaLoggerFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonUnitTests
{
    /// <summary>Unit tests for the QuotaLogger</summary>
    [TestClass]
    public sealed class QuotaLoggerFixture
    {
        /// <summary>
        /// Directory for the test quota logger
        /// </summary>
        private DirectoryInfo logDirectory;

        /// <summary>
        /// Creates the test log folder and initializes settings
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.logDirectory = new DirectoryInfo(@".\QuotaLog\");
            if (this.logDirectory.Exists)
            {
                this.logDirectory.Delete(true);
            }
            
            this.logDirectory.Create();

            ConfigurationManager.AppSettings["Logging.ConnectionString"] = "UseDevelopmentStorage=true";
            ConfigurationManager.AppSettings["Logging.BlobContainer"] = "quotaLog";
            ConfigurationManager.AppSettings["Logging.MaximumSizeInMegabytes"] = "10240";
            ConfigurationManager.AppSettings["Logging.RootPath"] = this.logDirectory.FullName;
            ConfigurationManager.AppSettings["Logging.ScheduledTransferPeriodMinutes"] = "0.1";
            ConfigurationManager.AppSettings["Logging.FileSizeBytes"] = "256";
        }

        /// <summary>Test creating a quota logger</summary>
        [TestMethod]
        public void CreateLogger()
        {
            var logger = new QuotaLogger() as ILogger;
            Assert.IsNotNull(logger);
        }

        /// <summary>Test logging a message</summary>
        [TestMethod]
        public void LogMessage()
        {
            var quotaLogger = new QuotaLogger();
            quotaLogger.LogMessage(
                LogLevels.Information,
                Process.GetCurrentProcess().ProcessName,
                Thread.CurrentThread.Name,
                "QuotaLoggerFixture.LogMessage()",
                "This is a test");

            // Verify a log file was created
            Assert.AreEqual(1, this.logDirectory.GetFiles().Length);
        }

        /// <summary>Test logging messages to sized part files</summary>
        [TestMethod]
        public void SizedParts()
        {
            var quotaLogger = new QuotaLogger();

            // The below message comes out to about 130 bytes. With a part size limit of 256, 6 messages
            // should result in at least 3 files. By going for 3 part files, even if a timestamp boundary
            // is crossed during the milliseconds it takes to run the test, this will still assert that
            // at least one part file was created due to size.
            for (int i = 0; i < 6; i++)
            {
                quotaLogger.LogMessage(
                    LogLevels.Information,
                    "Deployment Role Instance",
                    "Thread Name",
                    "QuotaLoggerFixture.LogMessage()",
                    "This is a test");
            }

            // Verify that at least 3 files were created
            Assert.IsTrue(this.logDirectory.GetFiles().Length >= 3);
        }
    }
}
