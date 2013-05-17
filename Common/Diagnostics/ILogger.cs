//-----------------------------------------------------------------------
// <copyright file="ILogger.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Diagnostics
{
    /// <summary>Interface for loggers</summary>
    public interface ILogger
    {
        /// <summary>Gets the log levels supported by this logger</summary>
        LogLevels LogLevels { get; }

        /// <summary>Gets a value indicating whether only alerts are supported</summary>
        bool AlertsOnly { get; }

        /// <summary>Logs a message with the specified log level</summary>
        /// <param name="level">The level of the log message</param>
        /// <param name="instance">The role instance of the log message</param>
        /// <param name="thread">The thread of the log message</param>
        /// <param name="source">The source of the log message</param>
        /// <param name="message">The content of the log message</param>
        void LogMessage(LogLevels level, string instance, string thread, string source, string message);
    }
}
