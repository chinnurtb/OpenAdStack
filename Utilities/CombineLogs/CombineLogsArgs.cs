// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CombineLogsArgs.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using ConsoleAppUtilities;

namespace Utilities.CombineLogs
{
    /// <summary>Command-line arguments for DeleteQueues</summary>
    public class CombineLogsArgs : CommandLineArguments
    {
        /// <summary>
        /// Gets or sets a date for logs to time interleave
        /// </summary>
        [CommandLineArgument("-start", "The start date of the logs you wish to interleave eg: 2013-02-11T00:00")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets a date for logs to time interleave
        /// </summary>
        [CommandLineArgument("-end", "The end date of the logs you wish to interleave eg: 2013-02-11T00:00. Default is the present.")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets a list of log container names to include
        /// </summary>
        [CommandLineArgument("-c", "Override the default list of log container names to include in the app.config. The default is \"workerlogs\" and \"weblogs\"")]
        public string LogContainers { get; set; }

        /// <summary>Gets or sets the connection string</summary>
        [CommandLineArgument("-cs", "Override the default connection string in the app.config.")]
        public string ConnectionString { get; set; }

        /// <summary>Gets or sets the output file directory</summary>
        [CommandLineArgument("-f", "Specify the output file. Default is a date named file in the app directory.")]
        public FileInfo OutputFile { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get { return this.StartDate.Date != new DateTime().Date; }
        }

        /// <summary>
        /// Gets the list of queue name prefixes to include
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
