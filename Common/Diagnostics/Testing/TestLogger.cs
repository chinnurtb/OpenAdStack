//-----------------------------------------------------------------------
// <copyright file="TestLogger.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Diagnostics.Testing
{
    /// <summary>
    /// Outputs log messages as human readable trace messages.
    /// </summary>
    /// <remarks>
    /// Messages can be viewed in real-time in the compute emulator console.
    /// </remarks>
    public class TestLogger : ILogger
    {
        /// <summary>List containing the test log entries</summary>
        private IList<TestLogEntry> entries = new List<TestLogEntry>();

        /// <summary>Initializes a new instance of the TestLogger class</summary>
        public TestLogger()
        {
            this.AlertsOnly = false;
        }

        /// <summary>Gets the log levels supported by this logger</summary>
        public LogLevels LogLevels
        {
            get { return LogLevels.All; }
        }

        /// <summary>Gets or sets a value indicating whether only alerts are supported</summary>
        public bool AlertsOnly { get; set; }

        /// <summary>
        /// Gets all of the log entries
        /// </summary>
        public IEnumerable<TestLogEntry> Entries
        {
            get { return this.entries.Where(e => e != null); }
        }

        /// <summary>
        /// Gets the log entries with LogLevel.Error
        /// </summary>
        public IEnumerable<TestLogEntry> InfoEntries
        {
            get { return this.Entries.Where(e => e.LogLevel == LogLevels.Information); }
        }

        /// <summary>
        /// Gets the log entries with LogLevel.Error
        /// </summary>
        public IEnumerable<TestLogEntry> WarningEntries
        {
            get { return this.Entries.Where(e => e.LogLevel == LogLevels.Warning); }
        }

        /// <summary>
        /// Gets the log entries with LogLevel.Error
        /// </summary>
        public IEnumerable<TestLogEntry> ErrorEntries
        {
            get { return this.Entries.Where(e => e.LogLevel == LogLevels.Error); }
        }

        /// <summary>
        /// Checks if one or more entries were logged with the specified level.
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasEntriesLoggedWithLevel(LogLevels level)
        {
            return 0 < this.LoggedWithLevel(level).Count();
        }

        /// <summary>
        /// Checks if one or more entries were logged at or before the specified time.
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasEntriesLoggedAtOrBefore(DateTime time)
        {
            return 0 < this.LoggedAtOrBefore(time).Count();
        }

        /// <summary>
        /// Checks if one or more entries were logged at or after the specified time.
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasEntriesLoggedAtOrAfter(DateTime time)
        {
            return 0 < this.LoggedAtOrAfter(time).Count();
        }

        /// <summary>
        /// Checks if one or more entries have messages equal to the specified text.
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasMessagesEqualTo(string text)
        {
            return 0 < this.MessagesEqualTo(text).Count();
        }

        /// <summary>
        /// Checks if one or more entries have messages containing the specified text.
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasMessagesContaining(string text)
        {
            return 0 < this.MessagesContaining(text).Count();
        }

        /// <summary>
        /// Checks if one or more entries have messages matching the specified regex pattern.
        /// </summary>
        /// <param name="pattern">The regex pattern</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasMessagesMatching(string pattern)
        {
            return 0 < this.MessagesMatching(pattern).Count();
        }

        /// <summary>
        /// Checks if one or more entries have sources equal to the specified text.
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasSourcesEqualTo(string text)
        {
            return 0 < this.SourcesEqualTo(text).Count();
        }

        /// <summary>
        /// Checks if one or more entries have sources containing the specified text.
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasSourcesContaining(string text)
        {
            return 0 < this.SourcesContaining(text).Count();
        }

        /// <summary>
        /// Checks if one or more entries have sources matching the specified regex pattern.
        /// </summary>
        /// <param name="pattern">The regex pattern</param>
        /// <returns>
        /// True if one or more matching entries were found; Otherwise, false.
        /// </returns>
        public bool HasSourcesMatching(string pattern)
        {
            return 0 < this.SourcesMatching(pattern).Count();
        }

        /// <summary>
        /// Gets the entries logged with the specified log level
        /// </summary>
        /// <param name="level">The log level</param>
        /// <returns>The count of log entries</returns>
        public IEnumerable<TestLogEntry> LoggedWithLevel(LogLevels level)
        {
            return this.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Source))
                .Where(entry => (entry.LogLevel & level) != LogLevels.None);
        }

        /// <summary>
        /// Gets the entries logged at or before the specified time
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> LoggedAtOrBefore(DateTime time)
        {
            return this.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Source))
                .Where(entry => entry.Timestamp <= time);
        }

        /// <summary>
        /// Gets the entries logged at or after the specified time
        /// </summary>
        /// <param name="time">The time</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> LoggedAtOrAfter(DateTime time)
        {
            return this.Entries
                .Where(entry => entry.Timestamp >= time);
        }

        /// <summary>
        /// Gets the entries with messages equal to the specified text
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> MessagesEqualTo(string text)
        {
            return this.Entries
                .Where(entry => entry.Message.Equals(text));
        }

        /// <summary>
        /// Gets the entries with messages containing the specified text
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> MessagesContaining(string text)
        {
            return this.Entries
                .Where(entry => entry.Message.Contains(text));
        }

        /// <summary>
        /// Gets the entries with messages matching the specified regex pattern
        /// </summary>
        /// <param name="pattern">The regex pattern</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> MessagesMatching(string pattern)
        {
            return this.Entries
                .Where(entry => new Regex(pattern).IsMatch(entry.Message));
        }

        /// <summary>
        /// Gets the entries with sources equal to the specified text
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> SourcesEqualTo(string text)
        {
            return this.Entries
                .Where(entry => entry.Source.Equals(text));
        }

        /// <summary>
        /// Gets the entries with sources containing the specified text
        /// </summary>
        /// <param name="text">The text</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> SourcesContaining(string text)
        {
            return this.Entries
                .Where(entry => entry.Source.Contains(text));
        }

        /// <summary>
        /// Gets the entries with sources matching the specified regex pattern
        /// </summary>
        /// <param name="pattern">The regex pattern</param>
        /// <returns>The entries</returns>
        public IEnumerable<TestLogEntry> SourcesMatching(string pattern)
        {
            return this.Entries
                .Where(entry => new Regex(pattern).IsMatch(entry.Source));
        }

        /// <summary>Logs a message with the specified log level</summary>
        /// <param name="level">The level of the log message</param>
        /// <param name="instance">The role instance of the log message</param>
        /// <param name="thread">The thread of the log message</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="message">The content of the log message</param>
        public void LogMessage(LogLevels level, string instance, string thread, string source, string message)
        {
            this.entries.Add(new TestLogEntry(level, instance, thread, source, message));
        }
    }
}
