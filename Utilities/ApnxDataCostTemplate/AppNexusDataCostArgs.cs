// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusDataCostArgs.cs" company="Rare Crowds">
//   Copyright Rare Crowds. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using ConsoleAppUtilities;

namespace Utilities.AppNexus.DataCostTemplate
{
    /// <summary>
    /// Command-line arguments for ApnxDataCosts
    /// </summary>
    public class AppNexusDataCostArgs : CommandLineArguments
    {
        /// <summary>Gets a value indicating whether the arguments are valid</summary>
        public override bool ArgumentsValid
        {
            get
            {
                return !(
                    string.IsNullOrWhiteSpace(this.ApiEndpoint) ||
                    string.IsNullOrWhiteSpace(this.UserName) ||
                    string.IsNullOrWhiteSpace(this.Password) ||
                    (this.OutFile.Exists && this.OutFile.IsReadOnly));
            }
        }

        /// <summary>Gets or sets the file to import/export from/to</summary>
        [CommandLineArgument("-out", "Output file name (Required)")]
        public FileInfo OutFile { get; set; }

        /// <summary>Gets or sets the AppNexus API endpoint URI</summary>
        [CommandLineArgument("-api", "URI of the AppNexus API endpoint (Optional)", DefaultAppSetting = "AppNexus.Endpoint")]
        public string ApiEndpoint { get; set; }

        /// <summary>Gets or sets the AppNexus user name</summary>
        [CommandLineArgument("-user", "AppNexus user name (Optional)", DefaultAppSetting = "AppNexus.Username")]
        public string UserName { get; set; }

        /// <summary>Gets or sets the AppNexus password</summary>
        [CommandLineArgument("-pass", "AppNexus password (Optional)", DefaultAppSetting = "AppNexus.Password")]
        public string Password { get; set; }
    }
}
