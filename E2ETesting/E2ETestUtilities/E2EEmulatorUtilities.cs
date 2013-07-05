//-----------------------------------------------------------------------
// <copyright file="E2EEmulatorUtilities.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace E2ETestUtilities
{
    /// <summary>Utilities for End-to-End tests</summary>
    public static class E2EEmulatorUtilities
    {
        /// <summary>
        /// How many seconds to wait for the emulator to be responsive
        /// </summary>
        private const int EmulatorStartupTimeoutSeconds = 45;

        /// <summary>Gets the emulator runner path</summary>
        private static string EmulatorRunnerPath
        {
            get { return ConfigurationManager.AppSettings["AzureEmulatorExe"]; }
        }

        /// <summary>Gets the package (csx) folder path</summary>
        private static string PackageFolderPath
        {
            get { return ConfigurationManager.AppSettings["CsxPath"]; }
        }

        /// <summary>Gets the configuration (cscfg) file path</summary>
        private static string PackageConfigurationPath
        {
            get { return ConfigurationManager.AppSettings["CscfgPath"]; }
        }

        /// <summary>Starts the compute emulator</summary>
        public static void StartEmulators()
        {
            AzureEmulatorHelper.StartStorageEmulator(EmulatorRunnerPath);
            AzureEmulatorHelper.StartComputeEmulator(EmulatorRunnerPath, PackageFolderPath, PackageConfigurationPath);

            // Wait until the web role responds with something other than 404
            var testClient = new RestTestClient("https://localhost/");
            var emulatorStartupTimeout = DateTime.UtcNow.AddSeconds(EmulatorStartupTimeoutSeconds);
            while (true)
            {
                var getCompaniesResponse = testClient.SendRequest(Microsoft.Http.HttpMethod.GET, string.Empty);
                if (getCompaniesResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    break;
                }

                Assert.IsTrue(
                    DateTime.UtcNow < emulatorStartupTimeout,
                    "Emulator failed to be responsive after {0} seconds",
                    EmulatorStartupTimeoutSeconds);
                Thread.Sleep(500);
            }
        }

        /// <summary>Stops the compute emulator</summary>
        public static void StopEmulators()
        {
            AzureEmulatorHelper.StopComputeEmulator(EmulatorRunnerPath);
            AzureEmulatorHelper.StopStorageEmulator(EmulatorRunnerPath);
        }
    }
}
