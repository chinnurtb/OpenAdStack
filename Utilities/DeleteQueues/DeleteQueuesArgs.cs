// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeleteQueuesArgs.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using ConsoleAppUtilities;

namespace Utilities.DeleteQueues
{
    /// <summary>Command-line arguments for DeleteQueues</summary>
    public class DeleteQueuesArgs : CommandLineArguments
    {
        /// <summary>
        /// Gets or sets a list of queue prefixes to include
        /// </summary>
        [CommandLineArgument("-i", "List of queue name prefixes to include.")]
        public string Includes { get; set; }

        /// <summary>
        /// Gets or sets a list of queue name substrings to exclude
        /// </summary>
        [CommandLineArgument("-x", "List of queue name substrings to exclude.")]
        public string Excludes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force deletions
        /// </summary>
        [CommandLineArgument("-f", "Force. Delete queues even if they are not empty.")]
        public bool Force { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to only preview what would be deleted
        /// </summary>
        [CommandLineArgument("-p", "Preview. Display the queues that would be deleted.")]
        public bool Preview { get; set; }

        /// <summary>Gets or sets the connection string</summary>
        [CommandLineArgument("-cs", "Override the default connection string in the app.config.")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display only content
        /// </summary>
        [CommandLineArgument("-q", "Do not display verbose output messages.")]
        public bool Quiet { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the list of queue name prefixes to include
        /// </summary>
        /// <remarks>If the list is empty</remarks>
        internal string[] IncludeList
        {
            get
            {
                var includes = this.Includes ?? string.Empty;
                return includes.ToLowerInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        /// Gets the list of queue name prefixes to exclude
        /// </summary>
        internal string[] ExcludeList
        {
            get
            {
                var excludes = this.Excludes ?? string.Empty;
                return excludes.ToLowerInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
