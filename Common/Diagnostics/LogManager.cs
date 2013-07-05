//-----------------------------------------------------------------------
// <copyright file="LogManager.cs" company="Rare Crowds Inc">
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Diagnostics
{
    /// <summary>Class for managing logging</summary>
    public class LogManager
    {
        /// <summary>Initializes a new instance of the LogManager class.</summary>
        /// <param name="loggers">Logger implementations to log messages to</param>
        private LogManager(ILogger[] loggers)
        {
            this.Loggers = loggers;
            this.LogMessage(
                LogLevels.Information,
                false,
                "LogManager initialized with {0} ILoggers: {1}",
                this.Loggers.Length,
                string.Join(", ", this.Loggers.Select(l => l.GetType().FullName)));
        }

        /// <summary>Gets or sets the singleton instance</summary>
        internal static LogManager Instance { get; set; }

        /// <summary>Gets the loggers</summary>
        internal ILogger[] Loggers { get; private set; }

        /// <summary>Initializes the log manager to use the provided loggers</summary>
        /// <param name="loggers">The ILogger implementation(s) to use</param>
        public static void Initialize(IEnumerable<ILogger> loggers)
        {
            LogManager.Instance = new LogManager(loggers.ToArray());
        }

        /// <summary>Logs the message with the default log level of Information</summary>
        /// <param name="message">message to be logged</param>
        public static void Log(string message)
        {
            LogManager.Log(LogLevels.Information, message);
        }

        /// <summary>
        /// Creates a log entry containing the specified message using the
        /// loggers which support the specified log level.
        /// </summary>
        /// <param name="level">The level of the log entry</param>
        /// <param name="message">The content of the log entry</param>
        /// <param name="args">The args for formatted messages</param>
        public static void Log(LogLevels level, string message, params object[] args)
        {
            Log(level, false, message, args);
        }

        /// <summary>
        /// Creates a log entry containing the specified message using the
        /// loggers which support the specified log level.
        /// </summary>
        /// <param name="level">The level of the log entry</param>
        /// <param name="isAlert">Whether the log entry is an alert</param>
        /// <param name="message">The content of the log entry</param>
        /// <param name="args">The args for formatted messages</param>
        public static void Log(LogLevels level, bool isAlert, string message, params object[] args)
        {
            if (LogManager.Instance == null)
            {
                Trace.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "ERROR: LogManager.Initialize must be called before LogManager.Log\n{0}",
                    new StackTrace()));
                return;
            }

            LogManager.Instance.LogMessage(level, isAlert, message, args);
        }

        /// <summary>Gets the role instance for the log message</summary>
        /// <returns>String identifying the current role instance</returns>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Only called within Azure role")]
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Must not throw exceptions")]
        internal static string GetRoleInstance()
        {
            try
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    RoleEnvironment.DeploymentId,
                    RoleEnvironment.CurrentRoleInstance.Id);
            }
            catch
            {
                return "(unavailable)";
            }
        }

        /// <summary>
        /// Finds the source for log messages from the current
        /// caller external to LogManager in the call stack.
        /// </summary>
        /// <returns>The source of the message</returns>
        internal static string FindMessageSource()
        {
            var stack = new StackTrace().ToString().Split(new[] { "\r\n   at " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var level in stack)
            {
                if (!level.Contains(typeof(LogManager).FullName))
                {
                    return level;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Creates a log entry containing the specified message using the
        /// loggers which support the specified log level.
        /// </summary>
        /// <param name="level">The level of the log entry</param>
        /// <param name="isAlert">Whether the log entry is an alert</param>
        /// <param name="message">The content of the log entry</param>
        /// <param name="args">The args for formatted messages</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Logging must not throw exceptions.")]
        private void LogMessage(LogLevels level, bool isAlert, string message, params object[] args)
        {
            var logMessage = message;

            if (args != null && args.Length > 0)
            {
                try
                {
                    logMessage = string.Format(CultureInfo.InvariantCulture, message, args ?? new object[0]);
                }
                catch (FormatException)
                {
                }
            }

            var instance = GetRoleInstance();
            var source = LogManager.FindMessageSource();
            var levelLoggers = this.Loggers
                .Where(l => (l.LogLevels & level) != LogLevels.None)
                .Where(l => !l.AlertsOnly || isAlert);
            foreach (ILogger logger in levelLoggers)
            {
                try
                {
                    logger.LogMessage(level, instance, Thread.CurrentThread.Name, source, logMessage);
                }
                catch (Exception e)
                {
                    // Logging should never throw exceptions
                    var errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "ERROR: Unable to log message using {0}: {1}\n\nLog Entry: level={2} instance={3} thread={4} source={5} message={6}",
                        logger.GetType().FullName,
                        e,
                        level,
                        instance,
                        Thread.CurrentThread.Name,
                        source,
                        message);
                    System.Diagnostics.Trace.WriteLine(errorMessage);
                }
            }
        }
    }
}
