//-----------------------------------------------------------------------
// <copyright file="AzureStorageTestHelper.cs" company="Rare Crowds Inc">
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

using System.Configuration;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using TestUtilities;

namespace AzureQueueIntegrationTests
{
    /// <summary>Helper for tests using Azure Storage</summary>
    [TestClass]
    public class AzureStorageTestHelper
    {
        /// <summary>Gets the storage emulator sql instance</summary>
        private static string EmulatorRunnerPath
        {
            get { return ConfigurationManager.AppSettings["AzureEmulatorExe"]; }
        }

        /// <summary>Gets the storage emulator sql instance</summary>
        private static string StorageInitializerPath
        {
            get { return ConfigurationManager.AppSettings["AzureStorageInitExe"]; }
        }

        /// <summary>Gets the storage emulator sql instance</summary>
        private static string StorageEmulatorSqlInstance
        {
            get { return ConfigurationManager.AppSettings["AzureStorageEmulatorSqlInstance"]; }
        }

        /// <summary>
        /// Before tests in this assembly are run, ensure DSService is running
        /// and setup the configuration settings publisher to use AppSettings
        /// </summary>
        /// <param name="context">Test Context (unused)</param>
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            // Force Azure emulated storage to reinitialize (clears data)
            AzureEmulatorHelper.StopStorageEmulator(EmulatorRunnerPath);
            AzureEmulatorHelper.ClearEmulatedStorage(StorageInitializerPath, StorageEmulatorSqlInstance);
            AzureEmulatorHelper.StartStorageEmulator(EmulatorRunnerPath);
        }

        /// <summary>
        /// After tests in this assembly are run, clear storage and shutdown the storage emulator
        /// </summary>
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            AzureEmulatorHelper.ClearEmulatedStorage(StorageInitializerPath, StorageEmulatorSqlInstance);
            AzureEmulatorHelper.StopStorageEmulator(EmulatorRunnerPath);
        }
    }
}
