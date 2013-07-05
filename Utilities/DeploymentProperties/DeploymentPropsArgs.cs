// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DeploymentPropsArgs.cs" company="Rare Crowds Inc">
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

namespace Utilities.DeploymentProps
{
    /// <summary>Command-line arguments for DictionaryViewer</summary>
    public class DeploymentPropsArgs : CommandLineArguments
    {
        /// <summary>
        /// Description for Command
        /// </summary>
        private const string CommandDescription =
@"Command to run. Required. List, View, Get or Set.
            Index      List deployments/role instances
            Instances  List role instances only
            Get        Gets a deployment property (requires -pn)
            Set        Sets a deployment property (requires -d, -pn and -pv)
            List       Lists properties
            Remove     Removes a property (requires -d and -pn)";

        /// <summary>Gets or sets the command</summary>
        [CommandLineArgument("-c", CommandDescription)]
        public PropsCommand Command { get; set; }

        /// <summary>Gets or sets the deployment ID</summary>
        [CommandLineArgument("-d", "Deployment ID.")]
        public string DeploymentId { get; set; }

        /// <summary>Gets or sets the role instance ID</summary>
        [CommandLineArgument("-i", "Role Instance ID.")]
        public string InstanceId { get; set; }

        /// <summary>Gets or sets the property name</summary>
        [CommandLineArgument("-pn", "Property name.")]
        public string PropertyName { get; set; }

        /// <summary>Gets or sets the property value</summary>
        [CommandLineArgument("-pv", "Property value.")]
        public string PropertyValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include role instance properties
        /// when only a Deployment ID is specified
        /// </summary>
        [CommandLineArgument("-r", "Recursive. Include role instances when only Deployment ID is specified.")]
        public bool Recursive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to prompt when removing properties
        /// </summary>
        [CommandLineArgument("-y", "Do not prompt to remove properties.")]
        public bool Confirmed { get; set; }

        /// <summary>Gets or sets the connection string</summary>
        [CommandLineArgument("-cs", "Overrides the SQL Connection String in DProps.exe.config.")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display debug information
        /// </summary>
        [CommandLineArgument("-dbg", "Display verbose/debugging information.")]
        public bool Verbose { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (this.Command == PropsCommand.Unknown)
                {
                    return false;
                }

                switch (this.Command)
                {
                    case PropsCommand.Get:
                        if (string.IsNullOrWhiteSpace(this.PropertyName))
                        {
                            return false;
                        }

                        break;
                    case PropsCommand.Set:
                        if (string.IsNullOrWhiteSpace(this.PropertyValue) ||
                            string.IsNullOrWhiteSpace(this.PropertyName) ||
                            string.IsNullOrEmpty(this.DeploymentId))
                        {
                            return false;
                        }

                        break;
                }

                return true;
            }
        }
    }
}
