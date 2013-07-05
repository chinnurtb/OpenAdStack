//-----------------------------------------------------------------------
// <copyright file="ILogger.cs" company="Rare Crowds Inc">
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
