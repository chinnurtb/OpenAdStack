//Copyright 2012-2013 Rare Crowds, Inc.
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace ActivityUtilitiesUnitTests
{
    /// <summary>Tests for the DynamicAllocation Activity Utilities</summary>
    [TestClass]
    public class DynamicAllocationUtilitiesFixture
    {
        /// <summary>Test making and parsing an export unit name</summary>
        [TestMethod]
        public void RoundtripExportUnitName()
        {
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId().ToString(),
                "Test -- {0}".FormatInvariant(Guid.NewGuid().ToString("N")),
                10000,
                DateTime.UtcNow.Date,
                DateTime.UtcNow.Date.AddDays(7),
                "???");
            var allocation = new PerNodeBudgetAllocationResult
            {
                AllocationId = Guid.NewGuid().ToString("N"),
                PeriodMediaBudget = 9.74m,
                ExportBudget = 8m,
                LifetimeMediaSpend = 123m,
            };
            var measures = new MeasureSet { 123, 456, 789 };
            var lifetimeBudget = allocation.LifetimeMediaSpend + allocation.ExportBudget;

            var campaignName = DynamicAllocationActivityUtilities.MakeExportUnitNameForAllocation(
                allocation,
                campaignEntity,
                measures,
                lifetimeBudget);

            Assert.IsNotNull(campaignName);
            Assert.AreEqual(6, campaignName.Split(new[] { "--" }, StringSplitOptions.None).Count());
            Assert.IsTrue(campaignName.Contains(((string)campaignEntity.ExternalName).Replace("--", "_")));
            Assert.IsTrue(campaignName.Contains(allocation.AllocationId));
            Assert.IsTrue(campaignName.Contains(measures.Count.ToString(CultureInfo.InvariantCulture)));
            Assert.IsTrue(campaignName.Contains(lifetimeBudget.ToString(CultureInfo.InvariantCulture)));
            Assert.IsTrue(campaignName.Contains(allocation.PeriodMediaBudget.ToString(CultureInfo.InvariantCulture)));

            var allocationId = DynamicAllocationActivityUtilities.ParseAllocationIdFromExportUnitName(campaignName);
            Assert.AreEqual(allocation.AllocationId, allocationId);
        }

        /// <summary>Test getting an EntityMeasureSource from an entity</summary>
        [TestMethod]
        public void GetEntityMeasureSource()
        {
            // Setup a company entity with a custom measure map
            var testMeasureMap = new Dictionary<long, IDictionary<string, object>>
            {
                { 1, new Dictionary<string, object> { { "displayName", "foo" }, { "provider", -1 } } },
                { 2, new Dictionary<string, object> { { "displayName", "bar" }, { "provider", -1 } } },
            };
            var testMeasureMapJson = JsonConvert.SerializeObject(testMeasureMap);
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId().ToString(),
                Guid.NewGuid().ToString("N"));
            companyEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.MeasureMap,
                new PropertyValue(PropertyType.String, testMeasureMapJson));

            // Get the measure source using the extension
            var measureSource = companyEntity.GetMeasureSource();
            Assert.IsInstanceOfType(measureSource, typeof(EntityMeasureSource));

            // Verify the entity measures match the test measures
            var measures = measureSource.Measures;
            Assert.IsNotNull(measures);
            Assert.AreEqual(testMeasureMap.Count, measures.Count);
            Assert.IsTrue(testMeasureMap.All(testMeasure =>
                measures.ContainsKey(testMeasure.Key)));
            Assert.IsTrue(testMeasureMap.Values.All(testMeasure =>
                measures.Values.Any(measure =>
                    (string)measure["displayName"] == (string)testMeasure["displayName"] ||
                    (long)measure["provider"] == (long)(int)testMeasure["provider"])));
        }
    }
}
