// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationSimulatorArgs.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.IO;
using System.Linq;
using ConsoleAppUtilities;

using DataAccessLayer;

namespace Utilities.AllocationSimulator
{
    /// <summary>Command-line arguments for Base64Codec</summary>
    public class AllocationSimulatorArgs : CommandLineArguments
    {
        /// <summary>Gets or sets the input path</summary>
        [CommandLineArgument("-i", "Input file path.")]
        public FileInfo InFile { get; set; }

        /// <summary>Gets or sets the output path</summary>
        [CommandLineArgument("-o", "Output file path.")]
        public FileInfo OutFile { get; set; }

        /// <summary>Gets or sets the log file</summary>
        [CommandLineArgument("-log", "Log file path.")]
        public FileInfo LogFile { get; set; }

        /// <summary>Gets or sets a value indicating whether to use dry run mode</summary>
        [CommandLineArgument("-dry", "Dry run mode.")]
        public bool IsDryRun { get; set; }

        /// <summary>Gets or sets a value indicating whether to simulate with a campaign from the repository</summary>
        [CommandLineArgument("-rep", "Repository campaign sim mode.")]
        public bool IsRepCampaign { get; set; }

        /// <summary>Gets or sets the start time (as UTC) for the dry run.</summary>
        [CommandLineArgument("-start", "Start time for dry run.")]
        public string DryRunStart { get; set; }

        /// <summary>Gets or sets company entity id for the dry run.</summary>
        [CommandLineArgument("-company", "Company Entity Id.")]
        public string CompanyEntityId { get; set; }

        /// <summary>Gets or sets campaign entity id for the dry run.</summary>
        [CommandLineArgument("-campaign", "Campaign Entity Id.")]
        public string CampaignEntityId { get; set; }

        /// <summary>Gets or sets the target platform.</summary>
        [CommandLineArgument("-profile", "Target Profile (ex: Local, Production).", DefaultAppSetting = "TargetProfile")]
        public string TargetProfile { get; set; }

        /// <summary>Gets whether or not the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (this.InFile == null || !this.InFile.Exists)
                {
                    return false;
                }

                if (this.IsDryRun)
                {
                    DateTime dryRunStart;
                    if (!(string.IsNullOrEmpty(this.DryRunStart) || DateTime.TryParse(this.DryRunStart, out dryRunStart)))
                    {
                        return false;
                    }
                
                    if (!EntityId.IsValidEntityId(this.CompanyEntityId))
                    {
                        return false;
                    }

                    if (!EntityId.IsValidEntityId(this.CampaignEntityId))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
