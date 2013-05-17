//-----------------------------------------------------------------------
// <copyright file="TraceLogger.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Diagnostics
{
    /// <summary>
    /// Outputs log messages as human readable trace messages.
    /// </summary>
    /// <remarks>
    /// Messages can be viewed in real-time in the compute emulator console.
    /// </remarks>
    public class TraceLogger : ILogger
    {
        /// <summary>Gets the log levels supported by this logger</summary>
        public LogLevels LogLevels
        {
            get { return LogLevels.All; }
        }

        /// <summary>Gets a value indicating whether only alerts are supported</summary>
        public bool AlertsOnly
        {
            get { return false; }
        }

        /// <summary>Logs a message with the specified log level</summary>
        /// <param name="level">The level of the log message</param>
        /// <param name="instance">The role instance of the log message</param>
        /// <param name="thread">The thread of the log message</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="message">The content of the log message</param>
        public void LogMessage(LogLevels level, string instance, string thread, string source, string message)
        {
            var logMessage = string.Format(
                CultureInfo.InvariantCulture,
                "<<{0}>> [{1}] ({2}:{3}) {4} - {5}",
                Enum.GetName(typeof(LogLevels), level),
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                instance,
                thread,
                source,
                message);
            Trace.WriteLine(logMessage);
        }
    }
}
