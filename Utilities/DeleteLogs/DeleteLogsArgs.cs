// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteLogsArgs.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using ConsoleAppUtilities;

namespace Utilities.DeleteLogs
{
    /// <summary>Command-line arguments for DeleteQueues</summary>
    public class DeleteLogsArgs : CommandLineArguments
    {
        /// <summary>
        /// Gets or sets an hour threshold to delete logs older than (exclusive)
        /// </summary>
        [CommandLineArgument("-h", "Override the default hour threshold to delete logs older than (exclusive) in the app.config. Must be >= 48hrs ago. The default is 48hrs ago")]
        public int HoursAgoThresholdForDeleting { get; set; }

        /// <summary>
        /// Gets or sets a list of log container names to include
        /// </summary>
        [CommandLineArgument("-c", "Override the default list of log container names to include in the app.config. The default is \"workerlogs\" and \"weblogs\"")]
        public string LogContainers { get; set; }

        /// <summary>Gets or sets the connection string</summary>
        [CommandLineArgument("-cs", "Override the default connection string in the app.config.")]
        public string ConnectionString { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get { return true;  }
        }

        /// <summary>
        /// Gets the list of log container names
        /// </summary>
        /// <remarks>If the list is empty</remarks>
        internal string[] LogContainersList
        {
            get
            {
                var logContainer = this.LogContainers ?? string.Empty;
                return logContainer.ToLowerInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
