//-----------------------------------------------------------------------
// <copyright file="LogManagerFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using Diagnostics;
using Diagnostics.Testing;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace CommonUnitTests
{
    /// <summary>Unit tests for the log manager</summary>
    [TestClass]
    public sealed class LogManagerFixture
    {
        /// <summary>
        /// Test initializing the LogManager with multiple Unity mappings for ILogger
        /// </summary>
        [TestMethod]
        public void InitializeWithMultipleLoggerUnityMappings()
        {
            using (var unityContainer = new UnityContainer())
            {
                unityContainer.RegisterType<ILogger, TestLogger>("LoggerA");
                unityContainer.RegisterType<ILogger, TestLogger>("LoggerB");

                LogManager.Initialize(unityContainer.ResolveAll<ILogger>());
                Assert.IsNotNull(LogManager.Instance);
                Assert.AreEqual(2, LogManager.Instance.Loggers.Length);
            }
        }

        /// <summary>Test initializing the LogManager with null loggers</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InitializeWithNullLoggers()
        {
            LogManager.Initialize(null);
        }

        /// <summary>Test initializing the LogManager with a test logger</summary>
        public void InitializeWithTestLogger()
        {
            var testLogger = new TestLogger();

            LogManager.Initialize(new[] { testLogger });

            // Assert the initialization information message was logged
            Assert.AreEqual(1, testLogger.Entries.Count());
            Assert.IsTrue(testLogger.HasSourcesEqualTo(LogManager.FindMessageSource()));
            Assert.IsTrue(testLogger.HasEntriesLoggedWithLevel(LogLevels.Information));
            Assert.IsTrue(testLogger.HasMessagesContaining(typeof(TestLogger).FullName));
        }

        /// <summary>Test logging a message</summary>
        [TestMethod]
        public void LogMessageWithTestLogger()
        {
            var message = Guid.NewGuid().ToString("N");
            var testLogger = new TestLogger();

            LogManager.Initialize(new[] { testLogger });
            LogManager.Log(LogLevels.Warning, message);
            
            Assert.AreEqual(2, testLogger.Entries.Count());
            Assert.AreEqual(2, testLogger.SourcesEqualTo(LogManager.FindMessageSource()).Count());

            Assert.AreEqual(1, testLogger.LoggedWithLevel(LogLevels.Warning).Count());
            Assert.AreEqual(1, testLogger.LoggedWithLevel(LogLevels.Information).Count());
            Assert.AreEqual(1, testLogger.MessagesContaining(message).Count());
        }

        /// <summary>Test logging alert vs non-alert messages</summary>
        [TestMethod]
        public void LogAlertMessage()
        {
            var messages = new List<string>();
            var mockLogger = MockRepository.GenerateMock<ILogger>();
            mockLogger.Stub(f => f.AlertsOnly).Return(true);
            mockLogger.Stub(f => f.LogLevels).Return(LogLevels.All);
            mockLogger.Stub(f =>
                f.LogMessage(
                    Arg<LogLevels>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything))
                .WhenCalled(call =>
                {
                    var message = call.Arguments[4] as string;
                    messages.Add(message);
                });

            LogManager.Initialize(new[] { mockLogger });

            var nonAlertMessage = Guid.NewGuid().ToString();
            var alertMessage = Guid.NewGuid().ToString();

            LogManager.Log(LogLevels.Information, false, nonAlertMessage);
            mockLogger.AssertWasNotCalled(f =>
                f.LogMessage(
                    Arg<LogLevels>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything));
            Assert.AreEqual(0, messages.Count);

            LogManager.Log(LogLevels.Information, true, alertMessage);
            mockLogger.AssertWasCalled(f =>
                f.LogMessage(
                    Arg<LogLevels>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything));
            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual(alertMessage, messages.Single());
        }
    }
}
