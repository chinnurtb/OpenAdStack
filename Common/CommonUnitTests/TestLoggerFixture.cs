//-----------------------------------------------------------------------
// <copyright file="TestLoggerFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Linq;
using System.Threading;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonUnitTests
{
    /// <summary>Unit tests for the test logger</summary>
    [TestClass]
    public class TestLoggerFixture
    {
        /// <summary>
        /// TestLogger instance for testing
        /// </summary>
        private TestLogger logger;

        /// <summary>
        /// Initializes the TestLogger instance and the LogManager
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.logger = new TestLogger();
            LogManager.Initialize(new[] { this.logger });
        }

        /// <summary>
        /// Tests creating a test log entry
        /// </summary>
        [TestMethod]
        public void CreateTestLogEntry()
        {
            var logLevel = RandomLogLevel();
            var message = Guid.NewGuid().ToString("N");
            var source = LogManager.FindMessageSource();
            
            var before = DateTime.UtcNow;
            Thread.Sleep(1);

            LogManager.Log(logLevel, message);
            
            Thread.Sleep(1);
            var after = DateTime.UtcNow;

            Assert.IsTrue(this.logger.HasEntriesLoggedWithLevel(logLevel));
            Assert.IsTrue(this.logger.HasEntriesLoggedAtOrAfter(before));
            Assert.IsTrue(this.logger.HasEntriesLoggedAtOrBefore(after));
            Assert.IsTrue(this.logger.HasSourcesEqualTo(source));
            Assert.IsTrue(this.logger.HasMessagesEqualTo(message));
        }

        /// <summary>
        /// Tests checking if a test log entry source contains text
        /// </summary>
        [TestMethod]
        public void TestLogEntrySourceContains()
        {
            var message = Guid.NewGuid().ToString("N");
            
            LogManager.Log(RandomLogLevel(), message);
            Assert.IsTrue(this.logger.HasSourcesContaining(this.GetType().Name));
        }

        /// <summary>
        /// Tests checking if a test log entry message contains text
        /// </summary>
        [TestMethod]
        public void TestLogEntryMessageContains()
        {
            var someText = Guid.NewGuid().ToString("N");
            var message = Guid.NewGuid().ToString("N") + someText + Guid.NewGuid().ToString("N");
            LogManager.Log(RandomLogLevel(), message);
            Assert.IsTrue(this.logger.HasMessagesContaining(someText));
        }

        /// <summary>
        /// Tests checking if a test log entry source matches a regex pattern
        /// </summary>
        [TestMethod]
        public void TestLogEntrySourceMatches()
        {
            var someText = Guid.NewGuid().ToString("N");
            var pattern = @"[a-zA-Z0-9_\.]+(" + this.GetType().Name + @")[a-zA-Z0-9_\.\(\)]+";
            var message = Guid.NewGuid().ToString("N");

            LogManager.Log(RandomLogLevel(), message);
            Assert.IsTrue(this.logger.HasSourcesMatching(pattern));
        }

        /// <summary>
        /// Tests checking if a test log entry source matches a regex pattern
        /// </summary>
        [TestMethod]
        public void TestLogEntryMessageMatches()
        {
            var someText = Guid.NewGuid().ToString("N");
            var pattern = "[a-z0-9]+(" + someText + ")[a-z0-9]+";
            var message = Guid.NewGuid().ToString("N") + someText + Guid.NewGuid().ToString("N");

            LogManager.Log(RandomLogLevel(), message);

            Assert.IsTrue(this.logger.HasMessagesMatching(pattern));
        }

        /// <summary>Gets a random LogLevels value</summary>
        /// <returns>A random value from LogLevels</returns>
        private static LogLevels RandomLogLevel()
        {
            var logLevelValues = (LogLevels[])Enum.GetValues(typeof(LogLevels));
            LogLevels level = LogLevels.None;
            while (level == LogLevels.None)
            {
                level = logLevelValues[new Random().Next(logLevelValues.Length)];
            }

            return level;
        }
    }
}
