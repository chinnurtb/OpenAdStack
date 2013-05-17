// -----------------------------------------------------------------------
// <copyright file="CanonicalDeliveryDataFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

using DynamicAllocation;

using DynamicAllocationActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// Test fixture for CanonicalDeliverData class
    /// </summary>
    [TestClass]
    public class CanonicalDeliveryDataFixture
    {
        /// <summary>Test default construction.</summary>
        [TestMethod]
        public void DefaultConstruction()
        {
            var canonicalDeliveryData = new CanonicalDeliveryData();
            Assert.AreEqual(0, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual(DeliveryNetworkDesignation.Unknown, canonicalDeliveryData.Network);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryReportDate);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryDataDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryDataDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryReportDate);
        }

        /// <summary>Test non-default construction.</summary>
        [TestMethod]
        public void NonDefaultConstruction()
        {
            var canonicalDeliveryData = new CanonicalDeliveryData(DeliveryNetworkDesignation.AppNexus);
            Assert.AreEqual(0, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual(DeliveryNetworkDesignation.AppNexus, canonicalDeliveryData.Network);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryReportDate);
            Assert.AreEqual(CanonicalDeliveryData.MinimumDeliveryDate, canonicalDeliveryData.LatestDeliveryDataDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryDataDate);
            Assert.AreEqual(CanonicalDeliveryData.MaximumDeliveryDate, canonicalDeliveryData.EarliestDeliveryReportDate);
        }

        /// <summary>Test we can parse overlapping raw delivery data into unique canonical delivery data.</summary>
        [TestMethod]
        public void AddRawDataOverlapping()
        {
            var expectedReportDate = DateTime.UtcNow;
            var canonicalDeliveryData = new CanonicalDeliveryData();
            var result = this.AddRawDataToCanonicalDeliveryData(
                    new[] { "Resources.ApnxDeliveryDataA.csv", "Resources.ApnxDeliveryDataB.csv" },
                    expectedReportDate,
                    ref canonicalDeliveryData);
            
            // There should be 9 unique records out of 12 overlapping records in the raw csv's
            Assert.IsTrue(result);
            Assert.AreEqual(9, canonicalDeliveryData.DeliveryDataForNetwork.Count);
            Assert.AreEqual("2012-03-16T23:00:00.0000000Z", canonicalDeliveryData.LatestDeliveryDataDate.ToString("o"));
            Assert.AreEqual("2012-03-15T19:00:00.0000000Z", canonicalDeliveryData.EarliestDeliveryDataDate.ToString("o"));
            Assert.AreEqual(expectedReportDate, canonicalDeliveryData.LatestDeliveryReportDate);
        }

        /// <summary>Test we return false on parse failure.</summary>
        [TestMethod]
        public void AddRawDataFailParse()
        {
            // Setup raw delivery data with a rogue header
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\nNOTANID,2012-03-16 15:00,00000000000000000000000000000001,1,1,1,1"
            };

            var expectedReportDate = DateTime.UtcNow;
            var canonicalDeliveryData = new CanonicalDeliveryData();
            var result = this.AddRawDataToCanonicalDeliveryDataCsv(
                    rawDeliveryDataList,
                    expectedReportDate,
                    ref canonicalDeliveryData);

            Assert.IsFalse(result);
        }

        /// <summary>Test we are tolerant of zero-data raw reports.</summary>
        [TestMethod]
        public void AddRawDataEmptyDeliveryData()
        {
            // Setup raw delivery data with a rogue header
            var rawDeliveryDataList = new[]
            {
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n",
                "campaign_id,hour,campaign_code,imps,ecpm,spend,clicks\r\n1234,2012-03-16 15:00,00000000000000000000000000000001,1,1,1,1"
            };

            var expectedReportDate = DateTime.UtcNow;
            var canonicalDeliveryData = new CanonicalDeliveryData();
            var result = this.AddRawDataToCanonicalDeliveryDataCsv(
                    rawDeliveryDataList,
                    expectedReportDate,
                    ref canonicalDeliveryData);

            Assert.IsTrue(result);
            Assert.AreEqual(1, canonicalDeliveryData.DeliveryDataForNetwork.Count);
        }

        /// <summary>Add raw delivery data from an array of resources to CanonicalDeliveryData.</summary>
        /// <param name="deliveryDataResources">The delivery data resources.</param>
        /// <param name="latestDeliveryReportDate">The latest delivery report date to use.</param>
        /// <param name="canonicalDeliveryData">CanonicalDeliveryData to update.</param>
        /// <returns>Result of AddRawData.</returns>
        private bool AddRawDataToCanonicalDeliveryData(
            string[] deliveryDataResources,
            DateTime latestDeliveryReportDate,
            ref CanonicalDeliveryData canonicalDeliveryData)
        {
            var rawDeliveryDataCsvs = deliveryDataResources.Select(r =>
                    EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(GetCampaignDeliveryDataFixture), r))
                    .ToArray();

            return this.AddRawDataToCanonicalDeliveryDataCsv(
                rawDeliveryDataCsvs, latestDeliveryReportDate, ref canonicalDeliveryData);
        }

        /// <summary>Add raw csv delivery data from an array of raw data to CanonicalDeliveryData.</summary>
        /// <param name="deliveryData">The raw delivery data.</param>
        /// <param name="latestDeliveryReportDate">The latest delivery report date to use.</param>
        /// <param name="canonicalDeliveryData">CanonicalDeliveryData to update.</param>
        /// <returns>Result of AddRawData.</returns>
        private bool AddRawDataToCanonicalDeliveryDataCsv(
            string[] deliveryData,
            DateTime latestDeliveryReportDate,
            ref CanonicalDeliveryData canonicalDeliveryData)
        {
            var result = true;

            foreach (var rawDeliveryData in deliveryData)
            {
                result = result && canonicalDeliveryData.AddRawData(
                    rawDeliveryData, latestDeliveryReportDate, new ApnxReportCsvParser());
            }

            return result;
        }
    }
}
