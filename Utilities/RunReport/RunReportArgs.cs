// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RunReportArgs.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using ConsoleAppUtilities;

using DataAccessLayer;

namespace RunReport
{
    /// <summary>Command-line argument helpers for RunReports</summary>
    public class RunReportArgs : CommandLineArguments
    {
        /// <summary>Gets or sets the output directory</summary>
        [CommandLineArgument("-o", "Output directory.")]
        public FileInfo OutFile { get; set; }

        /// <summary>Gets or sets company entity id for the dry run.</summary>
        [CommandLineArgument("-company", "Company Entity Id.")]
        public string CompanyEntityId { get; set; }

        /// <summary>Gets or sets campaign entity id for the dry run.</summary>
        [CommandLineArgument("-campaign", "Campaign Entity Id.")]
        public string CampaignEntityId { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate campaign report.</summary>
        [CommandLineArgument("-cr", "Campaign report flag.")]
        public bool IsCampaignReport { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate data provider report.</summary>
        [CommandLineArgument("-dr", "Data Provider report flag.")]
        public bool IsDataProviderReport { get; set; }

        /// <summary>Gets or sets a value indicating whether to generate general report.</summary>
        [CommandLineArgument("-all", "General report flag.")]
        public bool IsGeneralReport { get; set; }

        /// <summary>Gets or sets a value indicating whether to use verbose report output.</summary>
        [CommandLineArgument("-v", "Verbose report flag.")]
        public bool IsVerbose { get; set; }

        /// <summary>Gets or sets a value indicating whether to convert campaign locally.</summary>
        [CommandLineArgument("-legacy", "Convert campaign (locally only) to newer format.")]
        public bool IsLegacy { get; set; }
        
        /// <summary>Gets or sets the log file</summary>
        [CommandLineArgument("-log", "Log file path.")]
        public FileInfo LogFile { get; set; }
        
        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                if (!EntityId.IsValidEntityId(this.CompanyEntityId))
                {
                    return false;
                }

                if (!EntityId.IsValidEntityId(this.CampaignEntityId))
                {
                    return false;
                }

                if (!(this.IsCampaignReport ^ this.IsDataProviderReport ^ this.IsGeneralReport))
                {
                    return false;
                }

                return true;
            }
        }
    }
}