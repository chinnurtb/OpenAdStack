// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DictionaryViewerArgs.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using ConsoleAppUtilities;
using Utilities.Storage;

namespace Utilities.DictionaryViewer
{
    /// <summary>Command-line arguments for DictionaryViewer</summary>
    public class DictionaryViewerArgs : CommandLineArguments
    {
        /// <summary>
        /// Description for Command
        /// </summary>
        private const string CommandDescription =
@"Command to run. Required. List, View, Get or Set.
            Index   Displays a list of all dictionaries
            List    Displays a list of keys in a dictionary
            View    Displays entry information and contents (requires -k)
            Get     Downloads an entry to a file (requires -k and -o)
            Set     Uploads an entry from a file (requires -k and -i)
            Remove  Removes an entry from a dictionary (requires -k)
            Delete  Deletes an entire dictionary";

        /// <summary>Gets or sets the command</summary>
        [CommandLineArgument("-c", CommandDescription)]
        public ViewerCommand Command { get; set; }

        /// <summary>Gets or sets the store name</summary>
        [CommandLineArgument("-s", "Dictionary store name. Required.")]
        public string StoreName { get; set; }

        /// <summary>Gets or sets the key</summary>
        [CommandLineArgument("-k", "Entry key. Required for View, Get and Set commands.")]
        public string Key { get; set; }

        /// <summary>Gets or sets the output path</summary>
        [CommandLineArgument("-o", "Output file path. Required for Get command.")]
        public FileInfo OutFile { get; set; }

        /// <summary>Gets or sets the input path</summary>
        [CommandLineArgument("-i", "Input file path. Required for Set command.")]
        public FileInfo InFile { get; set; }

        /// <summary>Gets or sets the dictionary type</summary>
        [CommandLineArgument("-t", "Dictionary type. Sql or Cloud.")]
        public PersistentDictionaryType DictionaryType { get; set; }

        /// <summary>Gets or sets the connection string</summary>
        [CommandLineArgument("-cs", "Connection string (requires -t)")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display only content
        /// </summary>
        [CommandLineArgument("-q", "Only display content with List and View commands.")]
        public bool Quiet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display debug information
        /// </summary>
        [CommandLineArgument("-d", "Display debug information.")]
        public bool Debug { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.StoreName) || this.Command == ViewerCommand.Unknown)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Key) &&
                   (this.Command == ViewerCommand.Get ||
                    this.Command == ViewerCommand.View ||
                    this.Command == ViewerCommand.Set ||
                    this.Command == ViewerCommand.Remove))
                {
                    return false;
                }

                if ((this.Command == ViewerCommand.Get) != (this.OutFile != null))
                {
                    return false;
                }

                if (this.Command == ViewerCommand.Set && this.InFile == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.ConnectionString) !=
                    (this.DictionaryType == PersistentDictionaryType.Unknown))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
