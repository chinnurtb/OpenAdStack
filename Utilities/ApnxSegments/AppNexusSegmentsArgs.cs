// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusSegmentsArgs.cs" company="Rare Crowds Inc">
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

using System.IO;
using ConsoleAppUtilities;

namespace Utilities.AppNexusSegments
{
    /// <summary>
    /// Command-line arguments for CreateUser
    /// </summary>
    public class AppNexusSegmentsArgs : CommandLineArguments
    {
        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            // All arguments are optional defaults
            get
            {
                return !(
                    string.IsNullOrWhiteSpace(this.ApiEndpoint) ||
                    string.IsNullOrWhiteSpace(this.UserName) ||
                    string.IsNullOrWhiteSpace(this.Password) ||
                    string.IsNullOrWhiteSpace(this.File) ||
                    (this.Import == this.Export));
            }
        }

        /// <summary>Gets or sets the AppNexus API endpoint URI</summary>
        [CommandLineArgument("-api", "URI of the AppNexus API endpoint (ex: http://api.appnexus.com/)")]
        public string ApiEndpoint { get; set; }

        /// <summary>Gets or sets the AppNexus user name</summary>
        [CommandLineArgument("-user", "AppNexus user name")]
        public string UserName { get; set; }

        /// <summary>Gets or sets the AppNexus password</summary>
        [CommandLineArgument("-pass", "AppNexus password")]
        public string Password { get; set; }

        /// <summary>Gets or sets the file to import/export from/to</summary>
        [CommandLineArgument("-file", "Segments file name")]
        public string File { get; set; }

        /// <summary>Gets or sets a value indicating whether the function is import</summary>
        [CommandLineArgument("-import", "Imports the segments from the file to the account")]
        public bool Import { get; set; }

        /// <summary>Gets or sets a value indicating whether the function is export</summary>
        [CommandLineArgument("-export", "Exports the segments from the account to the file")]
        public bool Export { get; set; }

        /// <summary>Gets or sets the file to log messages to</summary>
        [CommandLineArgument("-log", "(Optional) Log file")]
        public string LogFile { get; set; }

        /// <summary>Gets or sets a value indicating whether the exported segments should be saved as XML</summary>
        [CommandLineArgument("-xml", "Saves exported segments as XML (cannot be used with import)")]
        public bool OutputXml { get; set; }
    }
}
