// -----------------------------------------------------------------------
// <copyright file="AppNexusReportCsvParserFixture.cs" company="Emerging Media Group">
//  Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Diagnostics;
using DynamicAllocationActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Test fixture for ApnxReportCsvParser</summary>
    [TestClass]
    public class AppNexusReportCsvParserFixture
    {
        /// <summary>
        /// Per test init
        /// </summary>
        [TestInitialize]
        public void InitializeTests()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
        }

        /// <summary>Test we load raw delivery data</summary>
        [TestMethod]
        public void LoadRawDeliveryData()
        {
            var rawDeliveryData = new[]
            {
                "   campaign_id,hour,campaign_code,imps,  ecpm,spend,clicks",
                "1,2012-03-16 15:00   ,00000000000000000000000000000001,123,1.30,304.32,27   ",
                "1,2012-03-16 16:00,00000000000000000000000000000001,123,1.30,304.32,27     "
            };

            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);

            Assert.AreEqual(2, deliveryData.Count);
            Assert.AreEqual(1, (long)deliveryData[0]["CampaignId"]);
            Assert.AreEqual("2012-03-16T15:00:00.0000000Z", deliveryData[0]["Hour"].ToString());
            Assert.AreEqual("00000000000000000000000000000001", (string)deliveryData[0]["AllocationId"]);
            Assert.AreEqual(123, (long)deliveryData[0]["Impressions"]);
            Assert.AreEqual(1.30m, (decimal)deliveryData[0]["Ecpm"]);
            Assert.AreEqual(304.32m, (decimal)deliveryData[0]["Spend"]);
            Assert.AreEqual(27, (long)deliveryData[0]["Clicks"]);
        }

        /// <summary>Test we load raw delivery data sample correctly</summary>
        [TestMethod]
        public void LoadRawDeliveryDataSampleTest()
        {
            var rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                "313824,2012-03-24 02:00,466b2cb6c4084294bf3338b9feff884f,42,1.484809523809523809524000,.062362,62357",
                "313802,2012-03-24 01:00,e12ac5d04c5b46ab91fe8ff85c58622f,1,.210000000000000000000000,.000210,210"
            };

            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);

            Assert.AreEqual(2, deliveryData.Count);
            Assert.AreEqual(313824, (long)deliveryData[0]["CampaignId"]);
            Assert.AreEqual("2012-03-24T02:00:00.0000000Z", deliveryData[0]["Hour"].ToString());
            Assert.AreEqual("466b2cb6c4084294bf3338b9feff884f", (string)deliveryData[0]["AllocationId"]);
            Assert.AreEqual(42, (long)deliveryData[0]["Impressions"]);
            Assert.AreEqual(Math.Round(1.484809523809523809524000m, 14), Math.Round((decimal)deliveryData[0]["Ecpm"], 14));
            Assert.AreEqual(.062362m, (decimal)deliveryData[0]["Spend"]);
            Assert.AreEqual(62357, (long)deliveryData[0]["Clicks"]);
        }

        /// <summary>Test we load raw delivery data sample correctly that contains embellished campaign codes</summary>
        [TestMethod]
        public void LoadRawDeliveryDataSampleTestWithDeletedCampaigns()
        {
            // campaign code has _DLTD...we will include it...but need to parse it correctly
            var rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                "312828,2012-03-23 01:00,127660c476c44f4a9ad94ad177079e5b_DLTD_1332465452,1,3.103000000000000000000000,.003103,3103"
            };

            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);

            Assert.AreEqual("127660c476c44f4a9ad94ad177079e5b", (string)deliveryData[0]["AllocationId"]);
        }

        /// <summary>Test we ignore if we don't recognize a header value.</summary>
        [TestMethod]
        public void LoadRawDeliveryDataHeaderNotRecognized()
        {
            var rawDeliveryData = new[]
            {
                "rogue,campaign_id,hour,campaign_code,imps,  ecpm,spend,clicks",
                "Rogue,1,2012-03-16 15:00   ,00000000000000000000000000000001,123,1.30,304.32,27   ",
                "1,2012-03-16 16,00000000000000000000000000000001,123,1.30,304.32,27\r\n     "
            };

            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);

            // Should include only the ones we recognize
            Assert.AreEqual(7, deliveryData[0].Count);
        }

        /// <summary>Test we return null if a value is not compatible with expected type.</summary>
        [TestMethod]
        public void LoadRawDeliveryDataValueIncompatible()
        {
            var rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                "NotACampaignId,2012-03-16 15:00   ,00000000000000000000000000000001,123,1.30,304.32,27   ",
                "1,2012-03-16 16,00000000000000000000000000000001,123,1.30,304.32,27     "
            };

            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.IsNull(deliveryData);
        }
        
        /// <summary>Test we return defaults for empty values that have reasonable defaults.</summary>
        [TestMethod]
        public void LoadRawDeliveryDataValueEmpty()
        {
            var rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                "1,2012-03-16 15:00,00000000000000000000000000000001,,,,"
            };

            // imps, ecpm, spend, media cost have reasonable defaults of zero
            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.AreEqual(1, (long)deliveryData[0]["CampaignId"]);
            Assert.AreEqual("2012-03-16T15:00:00.0000000Z", deliveryData[0]["Hour"].ToString());
            Assert.AreEqual("00000000000000000000000000000001", (string)deliveryData[0]["AllocationId"]);
            Assert.AreEqual(0, (long)deliveryData[0]["Impressions"]);
            Assert.AreEqual(0m, (decimal)deliveryData[0]["Ecpm"]);
            Assert.AreEqual(0m, (decimal)deliveryData[0]["Spend"]);
            Assert.AreEqual(0, (long)deliveryData[0]["Clicks"]);

            // campaign Id, hour, campaign_code are required
            rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                ",2012-03-16 15:00,00000000000000000000000000000001,,,,"
            };
            Assert.IsNull(new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData));
            rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                "1,,00000000000000000000000000000001,,,,"
            };
            Assert.IsNull(new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData));
            rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks",
                "1,2012-03-16 15:00,,,,,"
            };
            Assert.IsNull(new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData));
        }

        /// <summary>Test we return an empty collection if there is no delivery data.</summary>
        [TestMethod]
        public void LoadRawDeliveryDataEmpty()
        {
            var rawDeliveryData = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks"
            };

            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.AreEqual(0, deliveryData.Count);
        }

        /// <summary>Test we return an empty collection if the header is not present.</summary>
        [TestMethod]
        public void LoadRawDeliveryHeaderEmptyString()
        {
            var rawDeliveryData = new string[0];
            var deliveryData = new ApnxReportCsvParser().ParseRawRecords(rawDeliveryData);
            Assert.AreEqual(0, deliveryData.Count);
        }
    }
}
