// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SetAllocationParametersArgs.cs" company="Rare Crowds Inc">
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

namespace SetAllocationParameters
{
    using System;

    /// <summary>Command-line argument helpers</summary>
    public class SetAllocationParametersArgs : CommandLineArguments
    {
        /// <summary>Format for usage</summary>
        private const string UsageFormat =
@"Set custom allocation parameters.
Usage: SetAllocationParameters.exe -params ""allocations.js"" -company ""companyid"" [-target ""targetid""] [-replace ""replace or merge (default)""] [-log ""LogFileDirectory""]
{0}";
        
        /// <summary>Gets usage information</summary>
        public static string Usage
        {
            get { return UsageFormat.FormatInvariant(GetDescriptions<SetAllocationParametersArgs>()); }
        }

        /// <summary>Gets or sets the path to the input allocation parameters.</summary>
        [CommandLineArgument("-params", "Input file path.")]
        public FileInfo ParamsFile { get; set; }

        /// <summary>Gets or sets a value indicating whether to replace or merge custom allocation parameters.</summary>
        [CommandLineArgument("-replace", "Destructively replace custom allocation parameters with new. Default is merge.")]
        public bool Replace { get; set; }

        /// <summary>Gets or sets company entity id for the dry run.</summary>
        [CommandLineArgument("-company", "Company Entity Id.")]
        public string CompanyEntityId { get; set; }

        /// <summary>Gets or sets target entity id.</summary>
        [CommandLineArgument("-target", "Target Entity Id.")]
        public string TargetEntityId { get; set; }

        /// <summary>Gets or sets the log file</summary>
        [CommandLineArgument("-log", "Log file path.")]
        public FileInfo LogFile { get; set; }

        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (this.ParamsFile == null || !this.ParamsFile.Exists)
                {
                    Console.WriteLine("Missing or invalid allocation parameters file.");
                    Console.WriteLine(Usage);
                    return false;
                }

                if (!DataAccessLayer.EntityId.IsValidEntityId(this.CompanyEntityId))
                {
                    Console.WriteLine("Missing or invalid Company Id specified.");
                    Console.WriteLine(Usage);
                    return false;
                }

                if (this.TargetEntityId != null && !DataAccessLayer.EntityId.IsValidEntityId(this.TargetEntityId))
                {
                    Console.WriteLine("Invalid Target Id specified.");
                    Console.WriteLine(Usage);
                    return false;
                }

                return true;
            }
        }
    }
}
