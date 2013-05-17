//-----------------------------------------------------------------------
// <copyright file="TestLogEntry.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Globalization;

namespace Diagnostics.Testing
{
    /// <summary>
    /// Represents an entry in the test log
    /// </summary>
    public class TestLogEntry
    {
        /// <summary>
        /// Initializes a new instance of the TestLogEntry class
        /// </summary>
        /// <param name="level">The log level</param>
        /// <param name="instance">The role instance</param>
        /// <param name="thread">The thread</param>
        /// <param name="source">The source</param>
        /// <param name="message">The message</param>
        public TestLogEntry(LogLevels level, string instance, string thread, string source, string message)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            this.Timestamp = DateTime.UtcNow;
            this.LogLevel = level;
            this.Instance = instance;
            this.Thread = thread ?? "<unknown>";
            this.Source = source;
            this.Message = message;
        }

        /// <summary>Gets the entry timestamp</summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>Gets the entry log level</summary>
        public LogLevels LogLevel { get; private set; }

        /// <summary>Gets the role instance</summary>
        public string Instance { get; private set; }

        /// <summary>Gets the entry thread</summary>
        public string Thread { get; private set; }

        /// <summary>Gets the entry source</summary>
        public string Source { get; private set; }

        /// <summary>Gets the entry message</summary>
        public string Message { get; private set; }

        /// <summary>Gets a string representation of the test log entry</summary>
        /// <returns>The test log entry as a string</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1}",
                this.LogLevel,
                this.Message);
        }
    }
}
