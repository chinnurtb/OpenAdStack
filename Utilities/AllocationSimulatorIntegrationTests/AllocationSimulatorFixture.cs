// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationSimulatorFixture.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Configuration;
using System.IO;
using AllocationSimulator;
using DataAccessLayer;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.AllocationSimulator;

namespace AllocationSimulatorIntegrationTests
{
    /// <summary>Integration tests for the AllocationSimulator</summary>
    [TestClass]
    public class AllocationSimulatorFixture
    {
        /// <summary>One time class initialization</summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            // Force Azure emulated storage to start. DSService can still be running
            // but the emulated storage not available. The most reliable way to make sure
            // it's running and available is to stop it then start again.
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            AzureEmulatorHelper.StopStorageEmulator(emulatorRunnerPath);
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);
        }

        /// <summary>
        /// Test that running the allocation simulator does not crash or take longer than expected
        /// </summary>
        [TestMethod]
        [DeploymentItem("testcampaign.js")]
        public void FileBasedCampaignSmokeTest()
        {
            var fileHandler = new TestFileHandler();
            var arguments = new AllocationSimulatorArgs
                {
                    InFile = new FileInfo("testcampaign.js"),
                    TargetProfile = "Local"
                };

            new Simulator(arguments, fileHandler).Run();
            
            Assert.IsTrue(fileHandler.OutputFiles["valuations.js"].Contains("107000000000018024"));
            Assert.IsTrue(fileHandler.OutputFiles["measuremap.js"].Contains("107000000000018024"));
            Assert.IsTrue(fileHandler.OutputFiles["simrun0.js"].Contains("\"PeriodDuration\":\"03:00:00\""));
            Assert.IsTrue(fileHandler.OutputFiles["simrun7.js"].Contains("\"PeriodDuration\":\"03:00:00\""));
            Assert.IsTrue(fileHandler.OutputFiles["simrun8.js"].Contains("\"PeriodDuration\":\"12:00:00\""));
            Assert.IsTrue(fileHandler.OutputFiles["simrun25.js"].Contains("\"PeriodDuration\":\"12:00:00\""));
            Assert.IsTrue(fileHandler.OutputFiles["simrun26.js"].Contains("\"PeriodDuration\":\"00:01:00\""));

            // The last one is a simulator-only (to get an update allocation with final delivery) and will have zero duration
            Assert.IsTrue(fileHandler.OutputFiles["simrun27.js"].Contains("\"PeriodDuration\":\"00:00:00\""));
            Assert.IsFalse(fileHandler.OutputFiles.ContainsKey("simrun28.js"));
        }

        /// <summary>
        /// Test that running the allocation simulator does not crash or take longer than expected
        /// </summary>
        [TestMethod]
        [Ignore]
        [DeploymentItem("testcampaign.js")]
        public void DryRunRepositorySmokeTest()
        {
            var fileHandler = new TestFileHandler();
            var arguments = new AllocationSimulatorArgs
            {
                InFile = new FileInfo("testcampaign.js"),
                TargetProfile = "Local",
                IsRepCampaign = true,
                IsDryRun = true,
                CampaignEntityId = new EntityId(),
                CompanyEntityId = new EntityId(),
                DryRunStart = "2012-10-16T17:00:00.0000000Z"
            };

            var sim = new Simulator(arguments, fileHandler);

            // now get it to save the campaign described in the input file in
            // the repository
            sim.SetupFileBasedCampaign();

            sim.Run();
            Assert.IsTrue(fileHandler.OutputFiles["valuations.js"].Contains("107000000000018024"));
            Assert.IsTrue(fileHandler.OutputFiles["measuremap.js"].Contains("107000000000018024"));
            Assert.IsTrue(fileHandler.OutputFiles["simrun0.js"].Contains("\"PeriodDuration\":\"12:00:00\""));
            Assert.IsFalse(fileHandler.OutputFiles.ContainsKey("simrun1.js"));
        }

        /// <summary>
        /// IFileHandler for testing
        /// </summary>
        private class TestFileHandler : IFileHandler
        {
            /// <summary>Initializes a new instance of the <see cref="TestFileHandler"/> class.</summary>
            public TestFileHandler()
            {
                this.OutputFiles = new Dictionary<string, string>();
            }

            /// <summary>
            /// Gets the OutputFiles dictionary
            /// </summary>
            public IDictionary<string, string> OutputFiles { get; private set; }

            /// <summary>Write a string to a text file.</summary>
            /// <param name="fileName">File path.</param>
            /// <param name="textContent">String to write.</param>
            public void WriteFile(string fileName, string textContent)
            {
                var name = Path.GetFileName(fileName);
                if (!string.IsNullOrEmpty(name))
                {
                    this.OutputFiles[name] = textContent;
                }
            }

            /// <summary>Write a string to a text file.</summary>
            /// <param name="directoryName">Directory to create.</param>
            /// <returns>The fully qualified path that was created.</returns>
            public string CreateDirectory(string directoryName)
            {
                return directoryName;
            }
        }
    }
}
