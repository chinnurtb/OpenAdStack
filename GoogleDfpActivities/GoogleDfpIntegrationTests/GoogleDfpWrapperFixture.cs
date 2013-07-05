//-----------------------------------------------------------------------
// <copyright file="GoogleDfpWrapperFixture.cs" company="Rare Crowds Inc">
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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ConfigManager;
using Google.Api.Ads.Common.Util;
using Google.Api.Ads.Dfp.Lib;
using GoogleDfpClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using Dfp = Google.Api.Ads.Dfp.v201206;
using DfpUtils = Google.Api.Ads.Dfp.Util.v201206;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Tests for the GoogleDfpWrapper class</summary>
    [TestClass]
    public class GoogleDfpWrapperFixture
    {
        /// <summary>Test ad-unit width</summary>
        private const int TestAdUnitWidth = 300;

        /// <summary>Test ad-unit height</summary>
        private const int TestAdUnitHeight = 250;

        /// <summary>Reusable advertiser for cases not focused on testing advertisers</summary>
        private static long testAdvertiserId;

        /// <summary>Reusable order for cases not focused on testing orders</summary>
        private static long testOrderId;

        /// <summary>Reusable ad-unit for cases not focused on testing ad-units</summary>
        private static string testAdUnitId;

        /// <summary>Reusable placement for cases not focused on testing placements</summary>
        private static long testPlacementId;

        /// <summary>Reusable start date</summary>
        private static DateTime testStartDate;

        /// <summary>Reusable end date</summary>
        private static DateTime testEndDate;

        /// <summary>Test IGoogleDfpClient instance</summary>
        private IGoogleDfpClient dfpClient;

        /// <summary>Guid for unique test object names</summary>
        private string guid;

        /// <summary>Gets the test client instance as a GoogleDfpWrapper</summary>
        private GoogleDfpWrapper Wrapper
        {
            get { return this.dfpClient as GoogleDfpWrapper; }
        }

        /// <summary>Initialize per-test-run objects</summary>
        /// <param name="context">Parameter unused.</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            try
            {
                var testRunGuid = Guid.NewGuid().ToString();
                var client = new GoogleDfpWrapper();
                testAdvertiserId = client.CreateAdvertiser("Test Run Advertiser - " + testRunGuid, testRunGuid);
                testOrderId = client.CreateOrder(testAdvertiserId, "Test Run Order - " + testRunGuid, DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
                testAdUnitId = GoogleDfpTestHelpers.CreateAdUnit("Test_Run_AdUnit_" + testRunGuid, TestAdUnitWidth, TestAdUnitHeight);
                testPlacementId = GoogleDfpTestHelpers.CreatePlacement("Test Run Placement - " + testRunGuid, testAdUnitId);
                testStartDate = DateTime.UtcNow;
                testEndDate = testStartDate.AddDays(7);
            }
            catch
            {
            }
        }

        /// <summary>Cleanup per-test-run objects</summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            try
            {
                if (testOrderId > 0)
                {
                    new GoogleDfpWrapper().DeleteOrder(testOrderId);
                }
            }
            catch
            {
            }
        }

        /// <summary>Initialize per-test object(s)/settings</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["GoogleDfp.NetworkTimezone"] = "Pacific Standard Time";
            this.dfpClient = new GoogleDfpWrapper();
            this.guid = Guid.NewGuid().ToString("N");
        }

        /// <summary>Test initializing the DFP API wrapper with default config</summary>
        [TestMethod]
        public void CreateDfpApiUserFromDefaultConfig()
        {
            var user = this.Wrapper.DfpUser;
            Assert.IsNotNull(user);
            var userConfig = user.Config as DfpAppConfig;
            Assert.IsNotNull(userConfig);

            // Verify a few values from Config settings
            Assert.AreEqual(Config.GetValue("GoogleDfp.ApplicationName"), userConfig.ApplicationName);
            Assert.AreEqual(Config.GetValue("GoogleDfp.NetworkId"), userConfig.NetworkCode);
            Assert.AreEqual(Config.GetValue("GoogleDfp.Username"), userConfig.Email);
            Assert.AreEqual(Config.GetValue("GoogleDfp.Password"), userConfig.Password);
        }

        /// <summary>Test initializing the DFP API User (session) from an IConfig</summary>
        [TestMethod]
        public void CreateDfpApiUserFromCustomConfig()
        {
            var rand = new Random();
            var config = new Dictionary<string, string>
                {
                    { "GoogleDfp.ApplicationName", Guid.NewGuid().ToString().Left(12) },
                    { "GoogleDfp.NetworkId", "{0}".FormatInvariant(rand.Next()) },
                    { "GoogleDfp.Username", "dfp.dev{0}@rarecrowds.com".FormatInvariant(rand.Next()) },
                    { "GoogleDfp.Password", Guid.NewGuid().ToString() },
                };
            this.dfpClient = new GoogleDfpWrapper(new CustomConfig(config));
            var user = this.Wrapper.DfpUser;
            Assert.IsNotNull(user);
            var userConfig = user.Config as DfpAppConfig;
            Assert.IsNotNull(userConfig);

            Assert.AreEqual(config["GoogleDfp.ApplicationName"], userConfig.ApplicationName);
            Assert.AreEqual(config["GoogleDfp.NetworkId"], userConfig.NetworkCode);
            Assert.AreEqual(config["GoogleDfp.Username"], userConfig.Email);
            Assert.AreEqual(config["GoogleDfp.Password"], userConfig.Password);
        }

        /// <summary>Test creating an agency</summary>
        [TestMethod]
        public void CreateAgency()
        {
            var companyName = "Test Agency - " + this.guid;
            var externalId = this.guid;

            var companyId = this.dfpClient.CreateAgency(companyName, externalId);
            Assert.AreNotEqual(0, companyId);

            var company = this.Wrapper.CompanyService.getCompany(companyId);
            Assert.IsNotNull(company);
            Assert.AreEqual(companyId, company.id);
            Assert.AreEqual(companyName, company.name);
            Assert.AreEqual(externalId, company.externalId);
        }

        /// <summary>Test creating a house agency</summary>
        [TestMethod]
        public void CreateHouseAgency()
        {
            var companyName = "Test House Agency - " + this.guid;
            var externalId = this.guid;

            var companyId = this.dfpClient.CreateHouseAgency(companyName, externalId);

            var company = this.Wrapper.CompanyService.getCompany(companyId);
            Assert.IsNotNull(company);
            Assert.AreEqual(companyId, company.id);
            Assert.AreEqual(companyName, company.name);
            Assert.AreEqual(externalId, company.externalId);
        }

        /// <summary>Test creating an advertiser</summary>
        [TestMethod]
        public void CreateAdvertiser()
        {
            var companyName = "Test Advertiser - " + this.guid;
            var externalId = this.guid;

            var companyId = this.dfpClient.CreateAdvertiser(companyName, externalId);

            var company = this.Wrapper.CompanyService.getCompany(companyId);
            Assert.IsNotNull(company);
            Assert.AreEqual(companyId, company.id);
            Assert.AreEqual(companyName, company.name);
            Assert.AreEqual(externalId, company.externalId);
        }

        /// <summary>Test creating a house advertiser</summary>
        [TestMethod]
        public void CreateHouseAdvertiser()
        {
            var companyName = "Test House Advertiser - " + this.guid;
            var externalId = this.guid;

            var companyId = this.dfpClient.CreateHouseAdvertiser(companyName, externalId);

            var company = this.Wrapper.CompanyService.getCompany(companyId);
            Assert.IsNotNull(company);
            Assert.AreEqual(companyId, company.id);
            Assert.AreEqual(companyName, company.name);
            Assert.AreEqual(externalId, company.externalId);
        }

        /// <summary>Test getting a company</summary>
        [TestMethod]
        public void GetCompany()
        {
            var companyName = "Test Company - " + this.guid;
            var externalId = this.guid;
            var company = this.Wrapper.CompanyService.createCompany(
                new Dfp.Company
                {
                    name = companyName,
                    externalId = externalId,
                    type = Dfp.CompanyType.AGENCY
                });
            var companyId = company.id;

            var result = this.dfpClient.GetCompany(companyId);
            Assert.IsNotNull(result);
            Assert.AreEqual(companyId, result.id);
            Assert.AreEqual(companyName, result.name);
            Assert.AreEqual(externalId, result.externalId);
        }

        /// <summary>Test getting the test advertiser company</summary>
        [TestMethod]
        public void GetTestAdvertiserCompany()
        {
            var statement = new DfpUtils.StatementBuilder(
                "WHERE name = :name LIMIT 500")
                .AddValue("name", TestNetwork.AdvertiserName)
                .ToStatement();
            var companyPage = this.Wrapper.CompanyService.Invoke(svc =>
                svc.getCompaniesByStatement(statement));
            var advertiser = companyPage.results.FirstOrDefault();
            Assert.IsNotNull(advertiser);
            Assert.AreEqual(TestNetwork.AdvertiserName, advertiser.name);
            Assert.AreEqual(TestNetwork.AdvertiserId, advertiser.id);
        }

        /// <summary>Test getting the test placements</summary>
        [TestMethod]
        public void GetTestPlacements()
        {
            var statement = new DfpUtils.StatementBuilder(
                "WHERE status = :status LIMIT 500")
                .AddValue("status", "ACTIVE")
                .ToStatement();
            var placementPage = this.Wrapper.PlacementService.Invoke(svc =>
                svc.getPlacementsByStatement(statement));
            var placements = placementPage.results
                .ToDictionary(p => p.id, p => p.name);
            Assert.IsTrue(
                TestNetwork.Placements.All(testplacement =>
                    placements.Any(placement =>
                        testplacement.Key == placement.Key &&
                        testplacement.Value == placement.Value)));
        }

        /// <summary>Test getting a non-existent company</summary>
        [TestMethod]
        [ExpectedException(typeof(GoogleDfpClientException))]
        public void GetNonexistentCompany()
        {
            var invalidCompanyId = (long)Math.Floor(long.MaxValue * new Random().NextDouble());
            this.dfpClient.GetCompany(invalidCompanyId);
        }

        /// <summary>Test creating an order</summary>
        [TestMethod]
        public void CreateOrder()
        {
            long orderId = 0;
            try
            {
                var orderName = "Test Order - " + this.guid;
                var startDate = DateTime.UtcNow;
                var endDate = startDate.AddDays(7);

                orderId = this.dfpClient.CreateOrder(testAdvertiserId, orderName, startDate, endDate);

                var order = this.Wrapper.OrderService.getOrder(orderId);
                Assert.IsNotNull(order);
                Assert.AreEqual(orderName, order.name);

                // Assumes local time is the API user/network time
                // TODO: Use ToSystemDateTime overload to specify the timezone of the network/API
                var actualStartDate = order.startDateTime.ToSystemDateTime(this.Wrapper.NetworkTimezone);
                var actualEndDate = order.endDateTime.ToSystemDateTime(this.Wrapper.NetworkTimezone);

                // There seems to be some minutes of lag in the server's time.
                // Tollerate up to 10 minutes difference.
                Assert.IsTrue(Math.Abs((startDate - actualStartDate).TotalMinutes) < 10);
                Assert.IsTrue(Math.Abs((endDate - actualEndDate).TotalMinutes) < 10);
            }
            finally
            {
                if (orderId != 0)
                {
                    try
                    {
                        this.dfpClient.DeleteOrder(orderId);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>Test creating a line-item</summary>
        [TestMethod]
        public void CreateLineItem()
        {
            long orderId = 0;
            try
            {
                orderId = this.dfpClient.CreateOrder(
                    testAdvertiserId,
                    "Test Order - " + this.guid,
                    testStartDate,
                    testEndDate);

                var lineItemName = "Test Line Item - " + this.guid;
                var lineItemExternalId = this.guid;
                var cpm = 2.00m / 1000m;
                var impressionGoal = 3210;
                var includeAdUnits = new[] { testAdUnitId };
                var placementIds = new[] { testPlacementId };
                var locationIds = new long[] { 2010 };
                var technologyTargeting = new Dfp.TechnologyTargeting
                {
                    bandwidthGroupTargeting = new Dfp.BandwidthGroupTargeting
                    {
                        bandwidthGroups = new[]
                        {
                            new Dfp.Technology { id = 4 },
                            new Dfp.Technology { id = 3 }
                        }
                    },
                    browserLanguageTargeting = new Dfp.BrowserLanguageTargeting
                    {
                        browserLanguages = new[]
                        {
                            new Dfp.Technology { id = 504038 }
                        }
                    }
                };
                var creatives = new[]
                {
                    new Dfp.ImageCreative
                    {
                         size = new Dfp.Size
                         {
                             width = 320,
                             height = 200,
                             isAspectRatio = false
                         }
                    }
                };

                var lineItemId = this.dfpClient.CreateLineItem(
                    orderId,
                    lineItemName,
                    lineItemExternalId,
                    cpm,
                    impressionGoal,
                    testStartDate,
                    testEndDate,
                    includeAdUnits,
                    true,
                    placementIds,
                    locationIds,
                    technologyTargeting,
                    creatives);
                Assert.AreNotEqual(0, lineItemId);

                var lineItem = this.Wrapper.LineItemService.getLineItem(lineItemId);
                Assert.IsNotNull(lineItem);
                Assert.AreEqual(lineItemName, lineItem.name);
                Assert.AreEqual(lineItemExternalId, lineItem.externalId);
                Assert.AreEqual(orderId, lineItem.orderId);
                Assert.AreEqual(Dfp.CostType.CPM, lineItem.costType);
                Assert.AreEqual(Dfp.LineItemType.STANDARD, lineItem.lineItemType);
                var cpmMicroAmount = (long)(cpm * GoogleDfpWrapper.DollarsToDfpMoneyMicrosMultiplier);
                Assert.AreEqual(cpmMicroAmount, lineItem.costPerUnit.microAmount);

                var lineItems = this.dfpClient.GetLineItemsForOrder(orderId);
                Assert.IsNotNull(lineItems);
                Assert.AreEqual(1, lineItems.Length);
                Assert.AreEqual(lineItem.id, lineItems[0].id);
                Assert.AreEqual(lineItem.name, lineItems[0].name);
                Assert.AreEqual(lineItem.externalId, lineItems[0].externalId);
                Assert.AreEqual(lineItem.costType, lineItems[0].costType);
                Assert.AreEqual(lineItem.lineItemType, lineItems[0].lineItemType);
                Assert.AreEqual(lineItem.costPerUnit.microAmount, lineItems[0].costPerUnit.microAmount);
            }
            finally
            {
                if (orderId > 0)
                {
                    try
                    {
                        this.dfpClient.DeleteOrder(orderId);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>Test creating a line-item</summary>
        [TestMethod]
        public void CreateImageCreativeFromBytes()
        {
            var creativeName = "Test Creative - " + this.guid;
            var destinationUrl = "http://www.rarecrowds.com/";
            var imageName = this.guid + ".gif";
            var imageBytes = this.GetResourceBytes(@"Resources.test.gif");

            var creativeId = this.dfpClient.CreateImageCreative(
                testAdvertiserId,
                creativeName,
                destinationUrl,
                TestAdUnitWidth,
                TestAdUnitHeight,
                false,
                imageName,
                imageBytes);

            var creative = this.Wrapper.CreativeService.getCreative(creativeId) as Dfp.ImageCreative;
            Assert.IsNotNull(creativeId);
            Assert.AreEqual(testAdvertiserId, creative.advertiserId);
            Assert.AreEqual(creativeName, creative.name);
            Assert.AreEqual(destinationUrl, creative.destinationUrl);
            Assert.AreEqual(TestAdUnitWidth, creative.size.width);
            Assert.AreEqual(TestAdUnitHeight, creative.size.height);
            Assert.IsFalse(creative.size.isAspectRatio);
            Assert.AreEqual(imageName, creative.imageName);

            // Image will be uploaded and round-tripped creative will have the URL
            Assert.IsNull(creative.imageByteArray);
            Assert.IsNotNull(creative.imageUrl);
            var uploadedBytes = MediaUtilities.GetAssetDataFromUrl(new Uri(creative.imageUrl));
            Assert.AreEqual(imageBytes.Length, uploadedBytes.Length);
            foreach (var bytes in imageBytes.Zip(uploadedBytes))
            {
                Assert.AreEqual(bytes.Item1, bytes.Item2);
            }
        }

        /// <summary>Tests adding and removing a creative from a line-item</summary>
        [TestMethod]
        public void AddAndRemoveCreativeFromLineItem()
        {
            var creativeName = "Test Creative - " + this.guid;
            var creativeDestinationUrl = "http://www.rarecrowds.com/";
            int creativeWidth = 300, creativeHeight = 250;
            var creativeImageName = this.guid + ".gif";
            var creativeImageBytes = this.GetResourceBytes(@"Resources.test.gif");

            var creativeId = this.dfpClient.CreateImageCreative(
                testAdvertiserId,
                creativeName,
                creativeDestinationUrl,
                creativeWidth,
                creativeHeight,
                false,
                creativeImageName,
                creativeImageBytes);

            var lineItemName = "Test Line Item - " + this.guid;
            var lineItemExternalId = this.guid;
            var cpm = 2.00m / 1000m;
            var impressionGoal = 3210;
            var includeAdUnits = new[] { testAdUnitId };
            var placementIds = new[] { testPlacementId };
            var locationIds = new long[] { 2010 };
            var technologyTargeting = new Dfp.TechnologyTargeting();
            var creatives = new[]
            {
                new Dfp.ImageCreative
                {
                    size = new Dfp.Size
                    {
                        width = 300,
                        height = 250,
                        isAspectRatio = false
                    }
                }
            };

            var lineItemId = this.dfpClient.CreateLineItem(
                testOrderId,
                lineItemName,
                lineItemExternalId,
                cpm,
                impressionGoal,
                testStartDate,
                testEndDate,
                includeAdUnits,
                true,
                placementIds,
                locationIds,
                technologyTargeting,
                creatives);
            Assert.AreNotEqual(0, lineItemId);

            // Add the creative to the line item
            Assert.IsTrue(this.dfpClient.AddCreativeToLineItem(lineItemId, creativeId));

            //// TODO: How to verify?
            var lineItem = this.Wrapper.LineItemService.getLineItem(lineItemId);
            Assert.IsNotNull(lineItemId);

            // Remove the creative from the line item
            Assert.IsTrue(this.dfpClient.RemoveCreativeFromLineItem(lineItemId, creativeId));
        }

        /// <summary>Test getting the creatives for a line-item</summary>
        [TestMethod]
        [Ignore]
        public void GetCreativesForLineItem()
        {
        }

        /// <summary>Test getting the creatives for a line-item that doesn't have any creatives</summary>
        [TestMethod]
        [Ignore]
        public void GetCreativesForLineItemWithoutCreatives()
        {
        }

        /// <summary>Test getting creatives by their ids</summary>
        [TestMethod]
        [Ignore]
        public void GetCreativesById()
        {
        }

        /// <summary>Test getting nonexistent creatives by their ids</summary>
        [TestMethod]
        [Ignore]
        public void GetNonexistentCreativesById()
        {
        }

        /// <summary>Test approving an order</summary>
        [TestMethod]
        [Ignore]
        public void ApproveOrder()
        {
        }

        /// <summary>Test approving an order that does not meet the requirements for approval</summary>
        [TestMethod]
        [Ignore]
        public void ApproveUnreadyOrder()
        {
        }

        /// <summary>Test deleting an order</summary>
        [TestMethod]
        [Ignore]
        public void DeleteOrder()
        {
        }

        /// <summary>Test activating line-items</summary>
        [TestMethod]
        [Ignore]
        public void ActivateLineItems()
        {
        }

        /// <summary>Test activating line-items that have already been activated</summary>
        [TestMethod]
        [Ignore]
        public void ActivateLineItemsAlreadyActivated()
        {
        }

        /// <summary>Test pausing active line-items</summary>
        [TestMethod]
        [Ignore]
        public void PauseActiveLineItems()
        {
        }

        /// <summary>Test pausing line-items that have not yet been activated</summary>
        [TestMethod]
        [Ignore]
        public void PauseLineItemsNotActivated()
        {
        }

        /// <summary>Test pausing line-items that have already been paused</summary>
        [TestMethod]
        [Ignore]
        public void PausePausedLineItems()
        {
        }

        /// <summary>Test resuming line-items that are paused</summary>
        [TestMethod]
        [Ignore]
        public void ResumePausedLineItems()
        {
        }

        /// <summary>Test resuming line-items that are active</summary>
        [TestMethod]
        [Ignore]
        public void ResumeActiveLineItems()
        {
        }

        /// <summary>Test deleting line-items</summary>
        [TestMethod]
        [Ignore]
        public void DeleteLineItems()
        {
        }

        /// <summary>Test requesting and retreiving a report</summary>
        [TestMethod]
        public void RequestAndRetrieveReport()
        {
            const int RequestTimeoutMinutes = 2;
            var reportJobId = this.dfpClient.RequestDeliveryReport(testOrderId, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);
            var requestTimeout = DateTime.UtcNow.AddMinutes(RequestTimeoutMinutes);
            while (true)
            {
                if (DateTime.UtcNow > requestTimeout)
                {
                    Assert.Fail("Unable to retrieve report within {0} minutes", RequestTimeoutMinutes);
                }

                var status = this.dfpClient.CheckReportStatus(reportJobId);
                Assert.AreNotEqual(Dfp.ReportJobStatus.FAILED, status);
                if (status == Dfp.ReportJobStatus.COMPLETED)
                {
                    break;
                }
            }

            var reportCsv = this.dfpClient.RetrieveReport(reportJobId);
            Assert.IsNotNull(reportCsv);
            Assert.IsFalse(string.IsNullOrWhiteSpace(reportCsv));
            Assert.IsTrue(reportCsv.Contains("Dimension.LINE_ITEM_ID"));
            Assert.IsTrue(reportCsv.Contains("Dimension.LINE_ITEM_NAME"));
            Assert.IsTrue(reportCsv.Contains("Dimension.DATE"));
            Assert.IsTrue(reportCsv.Contains("Dimension.HOUR"));
            Assert.IsTrue(reportCsv.Contains("Column.AD_SERVER_IMPRESSIONS"));
            Assert.IsTrue(reportCsv.Contains("Column.AD_SERVER_CLICKS"));
            Assert.IsTrue(reportCsv.Contains("Column.AD_SERVER_AVERAGE_ECPM"));
            Assert.IsTrue(reportCsv.Contains("Column.AD_SERVER_CPM_AND_CPC_REVENUE"));
        }

        /// <summary>Test getting all AdUnits</summary>
        [TestMethod]
        public void GetAllAdUnits()
        {
            var adUnits = this.dfpClient.GetAllAdUnits();
            Assert.IsTrue(TestNetwork.AdUnitCodes.All(code => adUnits.Any(adunit => adunit.adUnitCode == code)));
        }

        /// <summary>Gets the bytes of an embedded resource from this assembly</summary>
        /// <param name="resourceName">The name of the resource</param>
        /// <returns>The embedded resource bytes</returns>
        public byte[] GetResourceBytes(string resourceName)
        {
            return EmbeddedResourceHelper.GetEmbeddedResourceAsByteArray(this.GetType(), resourceName);
        }
    }
}
