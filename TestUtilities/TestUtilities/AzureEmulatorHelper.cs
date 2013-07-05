// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureEmulatorHelper.cs" company="Rare Crowds Inc">
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.WindowsAzure;

namespace TestUtilities
{
    /// <summary>
    /// Helper class to start Azure emulators for testing purposes.
    /// </summary>
    public static class AzureEmulatorHelper
    {
        /// <summary>Start the storage emulator on the local machine.</summary>
        /// <param name="emulatorRunnerPath">Path to the Azure SDK CSRun.exe tool.</param>
        public static void StartStorageEmulator(string emulatorRunnerPath)
        {
            // Don't restart if already running
            var count = Process.GetProcessesByName("DSService").Length;
            if (count != 0)
            {
                return;
            }

            StartCommandLineToolAndWaitForExit(emulatorRunnerPath, "/devstore:start");
        }

        /// <summary>Stop the storage emulator on the local machine.</summary>
        /// <param name="emulatorRunnerPath">Path to the Azure SDK CSRun.exe tool.</param>
        public static void StopStorageEmulator(string emulatorRunnerPath)
        {
            // Don't stop it if it's not running
            var count = Process.GetProcessesByName("DSService").Length;
            if (count == 0)
            {
                return;
            }

            StartCommandLineToolAndWaitForExit(emulatorRunnerPath, "/devstore:shutdown");
        }

        /// <summary>Starts the compute emulator on the local machine with the specified deployment package.</summary>
        /// <param name="emulatorRunnerPath">Path to the Azure SDK CSRun.exe tool.</param>
        /// <param name="packageFolderPath">Path to the csx directory</param>
        /// <param name="configurationPath">Path to the cscfg</param>
        public static void StartComputeEmulator(string emulatorRunnerPath, string packageFolderPath, string configurationPath)
        {
            // Shutdown the emulator if it is already running
            StopComputeEmulator(emulatorRunnerPath);

            // Start the emulator running the provided package
            StartCommandLineToolAndWaitForExit(emulatorRunnerPath, "/run:" + packageFolderPath + ";" + configurationPath);
        }

        /// <summary>Stops the compute emulator on the local machine.</summary>
        /// <param name="emulatorRunnerPath">Path to the Azure SDK CSRun.exe tool.</param>
        public static void StopComputeEmulator(string emulatorRunnerPath)
        {
            StartCommandLineToolAndWaitForExit(emulatorRunnerPath, "/devfabric:shutdown");
        }

        /// <summary>Associate the storage emulator with a given sql instance and recreate (clear) the store.</summary>
        /// <param name="storeInitializerPath">Path to the Azure SDK DSInit.exe tool.</param>
        /// <param name="sqlInstance">The sql instance to use for emulated storage.</param>
        [SuppressMessage("Microsoft.Usage", "CA1801", Justification = "Temporary making this a no-op.")]
        public static void ClearEmulatedStorage(string storeInitializerPath, string sqlInstance)
        {
            // TODO: Temporarily make this a no-op until a full-up inline initialization can be accomplished.
            // TODO: For now rely build-based init (might be adequate indefinitely).
            //// var arguments = string.Format(CultureInfo.InvariantCulture, "/sqlinstance:{0} /silent /forcecreate", sqlInstance);
            //// StartCommandLineToolAndWaitForExit(storeInitializerPath, arguments);
        }

        /// <summary>
        /// Set Azure cloud storage to use the app config file as the configuration source.
        /// Useful for testing from non-Azure projects.
        /// </summary>
        public static void SetAppConfigAsConfigurationSource()
        {
            CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
            {
                var connectionString = ConfigurationManager.AppSettings[configName];
                configSetter(connectionString);
            });            
        }

        /// <summary>Start a command-line tool with supplied arguments and wait for exit.</summary>
        /// <param name="commandLineToolPath">The command line tool path.</param>
        /// <param name="arguments">The arguments.</param>
        internal static void StartCommandLineToolAndWaitForExit(string commandLineToolPath, string arguments)
        {
            var start = new ProcessStartInfo
            {
                Arguments = arguments,
                FileName = Path.GetFullPath(commandLineToolPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (var proc = new Process { StartInfo = start })
            {
                proc.Start();

                // Wait for the command-line tool to exit.
                proc.WaitForExit();

                // Check for failures
                if (proc.ExitCode != 0)
                {
                    var stdout = proc.StandardOutput.ReadToEnd();
                    var stderr = proc.StandardError.ReadToEnd();
                    var message =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Error running command line tool ({0} {1})\nExit Code: {2}\nSTDERR:\n{3}\nSTDOUT:\n{4}",
                            commandLineToolPath,
                            arguments,
                            proc.ExitCode,
                            stderr,
                            stdout);
                    throw new InvalidOperationException(message);
                }
            }
        }
    }
}
