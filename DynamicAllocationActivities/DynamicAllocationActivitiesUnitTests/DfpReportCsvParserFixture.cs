// -----------------------------------------------------------------------
// <copyright file="DfpReportCsvParserFixture.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System.Linq;
using Diagnostics;
using DynamicAllocationActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Test fixture for DfpReportCsvParser</summary>
    [TestClass]
    public class DfpReportCsvParserFixture
    {
        /// <summary>
        /// Per test init
        /// </summary>
        [TestInitialize]
        public void InitializeTests()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
        }

        /// <summary>Happy path parse raw delivery data</summary>
        [TestMethod]
        public void ParseRawDeliveryData()
        {
            var rawDeliveryData = new[]
            {
                "   dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "1,2012-03-16,15   ,00000000000000000000000000000001,123,1.30,304.32,27,namenotused   ",
                "1,2012-03-16,16,00000000000000000000000000000001,123,1.30,304.32,27,namenotused"
            };

            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);

            Assert.AreEqual(2, deliveryData.Count);
            Assert.AreEqual(1, (long)deliveryData[0]["CampaignId"]);
            Assert.AreEqual("2012-03-16T15:00:00.0000000Z", deliveryData[0]["Hour"].ToString());
            Assert.AreEqual("00000000000000000000000000000001", (string)deliveryData[0]["AllocationId"]);
            Assert.AreEqual(123, (long)deliveryData[0]["Impressions"]);
            Assert.AreEqual(1.30m, (decimal)deliveryData[0]["Ecpm"]);
            Assert.AreEqual(304.32m, (decimal)deliveryData[0]["Spend"]);
            Assert.AreEqual(27, (long)deliveryData[0]["Clicks"]);
        }

        /// <summary>Parse raw delivery data header case-insensitive</summary>
        [TestMethod]
        public void ParseRawDeliveryDataHeaderCaseInsensitive()
        {
            var rawDeliveryData = new[]
            {
                "Dimension.LINE_ITEM_ID,Dimension.DATE,Dimension.HOUR,DimensionAttribute.LINE_ITEM_EXTERNAL_ID,Column.AD_SERVER_IMPRESSIONS,Column.AD_SERVER_AVERAGE_ECPM,Column.AD_SERVER_CPM_AND_CPC_REVENUE,Column.AD_SERVER_CLICKS,Dimension.LINE_ITEM_NAME",
                "1,2012-03-16,15,00000000000000000000000000000001,123,1.30,304.32,27,namenotused",
                "1,2012-03-16,16,00000000000000000000000000000001,123,1.30,304.32,27,namenotused"
            };

            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);

            // Should include the canonical keys
            Assert.IsTrue(deliveryData[0].Keys.All(k => DfpReportCsvParser.CanonicalKeys.Contains(k)));
        }

        /// <summary>Parse raw delivery data redundant headers</summary>
        [TestMethod]
        public void ParseRawDeliveryDataIgnoreMultipleHeaders()
        {
            var rawDeliveryData = new[]
            {
                "Dimension.LINE_ITEM_ID,Dimension.DATE,Dimension.HOUR,DimensionAttribute.LINE_ITEM_EXTERNAL_ID,Column.AD_SERVER_IMPRESSIONS,Column.AD_SERVER_AVERAGE_ECPM,Column.AD_SERVER_CPM_AND_CPC_REVENUE,Column.AD_SERVER_CLICKS,Dimension.LINE_ITEM_NAME",
                "1,2012-03-16,15,00000000000000000000000000000001,123,1.30,304.32,27,namenotused",
                "Dimension.LINE_ITEM_ID,Dimension.DATE,Dimension.HOUR,DimensionAttribute.LINE_ITEM_EXTERNAL_ID,Column.AD_SERVER_IMPRESSIONS,Column.AD_SERVER_AVERAGE_ECPM,Column.AD_SERVER_CPM_AND_CPC_REVENUE,Column.AD_SERVER_CLICKS,Dimension.LINE_ITEM_NAME",
                "1,2012-03-16,16,00000000000000000000000000000001,123,1.30,304.32,27,namenotused"
            };

            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);

            // Extra header should be ignored
            Assert.AreEqual(2, deliveryData.Count);
        }

        /// <summary>Test we parse raw delivery data sample correctly</summary>
        //// TODO: Need sample data
        [TestMethod]
        [Ignore]
        public void ParseRawDeliveryDataSampleTest()
        {
        }

        /// <summary>Test we ignore if we don't recognize a header value.</summary>
        [TestMethod]
        public void ParseRawDeliveryDataHeaderNotRecognized()
        {
            var rawDeliveryData = new[]
            {
                "rogue,dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "Rogue,1,2012-03-16,15   ,00000000000000000000000000000001,123,1.30,304.32,27,namenotused   ",
                "1,2012-03-16,16,00000000000000000000000000000001,123,1.30,304.32,27,namenotused"
            };

            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);

            // Should include only the canonical keys
            Assert.IsTrue(deliveryData[0].Keys.All(k => DfpReportCsvParser.CanonicalKeys.Contains(k)));
        }

        /// <summary>Test we return null if a value is not compatible with expected type.</summary>
        [TestMethod]
        public void ParseRawDeliveryDataValueIncompatible()
        {
            var rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "NotACampaignId,2012-03-16,15   ,00000000000000000000000000000001,123,1.30,304.32,27,namenotused   ",
                "1,2012-03-16,16,00000000000000000000000000000001,123,1.30,304.32,27,namenotused"
            };

            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.IsNull(deliveryData);
        }
        
        /// <summary>Test we return defaults for empty values that have reasonable defaults.</summary>
        [TestMethod]
        public void ParseRawDeliveryDataValueEmpty()
        {
            var rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "1,2012-03-16,15,00000000000000000000000000000001,,,,,"
            };

            // imps, ecpm, spend, media cost have reasonable defaults of zero
            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.AreEqual(1, (long)deliveryData[0]["CampaignId"]);
            Assert.AreEqual("2012-03-16T15:00:00.0000000Z", deliveryData[0]["Hour"].ToString());
            Assert.AreEqual("00000000000000000000000000000001", (string)deliveryData[0]["AllocationId"]);
            Assert.AreEqual(0, (long)deliveryData[0]["Impressions"]);
            Assert.AreEqual(0m, (decimal)deliveryData[0]["Ecpm"]);
            Assert.AreEqual(0m, (decimal)deliveryData[0]["Spend"]);
            Assert.AreEqual(0, (long)deliveryData[0]["Clicks"]);

            // line item Id, date, hour, external id are required
            rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                ",2012-03-16,15,00000000000000000000000000000001,,,,,"
            };
            Assert.IsNull(new DfpReportCsvParser().ParseRawRecords(rawDeliveryData));
            rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "1,,15,00000000000000000000000000000001,,,,,"
            };
            Assert.IsNull(new DfpReportCsvParser().ParseRawRecords(rawDeliveryData));
            rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "1,2012-03-16,,00000000000000000000000000000001,,,,,"
            };
            Assert.IsNull(new DfpReportCsvParser().ParseRawRecords(rawDeliveryData));
            rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,  dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name",
                "1,2012-03-16,15,,,,,,"
            };
            Assert.IsNull(new DfpReportCsvParser().ParseRawRecords(rawDeliveryData));
        }

        /// <summary>Test we return an empty collection if there is no delivery data.</summary>
        [TestMethod]
        public void ParseRawDeliveryDataEmpty()
        {
            var rawDeliveryData = new[]
            {
                "dimension.line_item_id,dimension.date,dimension.hour,dimensionattribute.line_item_external_id,column.ad_server_impressions,column.ad_server_average_ecpm,column.ad_server_cpm_and_cpc_revenue,column.ad_server_clicks,dimension.line_item_name"
            };

            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.AreEqual(0, deliveryData.Count);
        }

        /// <summary>Test we return an empty collection if the header is not present.</summary>
        [TestMethod]
        public void ParseRawDeliveryHeaderEmptyString()
        {
            var rawDeliveryData = new string[0];
            var deliveryData = new DfpReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.AreEqual(0, deliveryData.Count);
        }
    }
}
