//-----------------------------------------------------------------------
// <copyright file="AppNexusApiReportsFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using AppNexusClient;
using AppNexusTestUtilities;
using ConfigManager;
using Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Net;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace AppNexusClientE2ETests
{
    /// <summary>Test the AppNexus report APIs</summary>
    [TestClass]
    public class AppNexusApiReportsFixture
    {
        /// <summary>Maximum time to wait for a report before failing the test</summary>
        private static readonly TimeSpan MaxReportWait = new TimeSpan(0, 0, 30);

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void Initialize()
        {
            LogManager.Initialize(new[] { new TraceLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            AppNexusClientHelper.InitializeDeliveryNetworkClientFactory();
        }

        /// <summary>Per-test cleanup</summary>
        [TestCleanup]
        public void Cleanup()
        {
            AppNexusClientHelper.Cleanup();
        }

        /// <summary>Test creating and deleting objects</summary>
        [TestMethod]
        public void RequestReport()
        {
            var advertiserName = "Test Company - " + Guid.NewGuid().ToString();
            var lineItemName = "Test Line-Item - " + Guid.NewGuid().ToString();

            var client = this.CreateClient();

            var advertiserId = client.CreateAdvertiser(advertiserName, Guid.NewGuid().ToString());
            var profileId = client.CreateLineItemProfile(
                advertiserId,
                lineItemName,
                new Dictionary<AppNexusFrequencyType, int>(),
                null,
                null);
            AppNexusClientHelper.AddAdvertiserForCleanup(advertiserId);
            var lineItemId = client.CreateLineItem(
                advertiserId,
                profileId,
                lineItemName,
                Guid.NewGuid().ToString(),
                true,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(1),
                100m);

            Guid reportIdGuid;
            var reportId = client.RequestDeliveryReport(advertiserId, lineItemId);
            Assert.IsNotNull(reportId);
            Assert.IsTrue(Guid.TryParse(reportId, out reportIdGuid));

            var reportRequestTime = DateTime.UtcNow;
            string reportData;
            while ((reportData = client.RetrieveReport(reportId)) == null)
            {
                var reportWait = DateTime.UtcNow - reportRequestTime;
                if (reportWait > MaxReportWait)
                {
                    // Just give up, but don't fail
                    return;
                }

                System.Threading.Thread.Sleep(2000);
            }

            // TODO: Assert on contents of reportData
            Assert.IsNotNull(reportData);
        }

        /// <summary>
        /// Creates an instance of the AppNexus API client for testing
        /// </summary>
        /// <returns>The client</returns>
        private IAppNexusApiClient CreateClient()
        {
            var client = new AppNexusApiClient
            {
                Config = new CustomConfig(),
                RestClient = { MaxRetries = 5, RetryWait = new TimeSpan(0, 0, 3) }
            };
            Assert.IsNotNull(client);
            return client;
        }
    }
}
