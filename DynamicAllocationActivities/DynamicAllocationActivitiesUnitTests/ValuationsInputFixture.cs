//-----------------------------------------------------------------------
// <copyright file="ValuationsInputFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using DynamicAllocation;
using DynamicAllocationActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;
using Utilities.Serialization;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// Fixture for testing the ValuationsInput class
    /// </summary>
    [TestClass]
    public class ValuationsInputFixture
    {
        /// <summary>Measures for testing.</summary>
        private static readonly long[] TestMeasures = new long[] { 1, 2 };

        /// <summary>MeasureSet for testing.</summary>
        private static readonly MeasureSet TestMeasureSet1 = new MeasureSet(new long[] { 1 });

        /// <summary>MeasureSet for testing.</summary>
        private static readonly MeasureSet TestMeasureSet2 = new MeasureSet(new long[] { 2 });

        /// <summary>MeasureSet for testing.</summary>
        private static readonly MeasureSet TestMeasureSet12 = new MeasureSet(TestMeasures);

        /// <summary>Measures Json for testing.</summary>
        private static string measureListSerializedJson;

        /// <summary>NodeValuationsSet Json for testing.</summary>
        private static string nodeValuationSetSerializedJson;

        /// <summary>Class level initialization of tests.</summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            InitializeTestData();
        }

        /// <summary>Test construction of valuation inputs object.</summary>
        [TestMethod]
        public void TestConstructor()
        {
            var measureSetsInput = new MeasureSetsInput();
            var nodeOverrides = new Dictionary<MeasureSet, decimal>();
            var valuationInputs = new ValuationInputs(measureSetsInput, nodeOverrides);
            Assert.AreSame(measureSetsInput, valuationInputs.MeasureSetsInput);
            Assert.AreSame(nodeOverrides, valuationInputs.NodeOverrides);
            Assert.AreEqual("legacyfingerprint", valuationInputs.ValuationInputsFingerprint);

            valuationInputs = new ValuationInputs(measureSetsInput, null);
            Assert.IsNull(valuationInputs.NodeOverrides);
        }

        /// <summary>Test construction of valuation inputs object from json.</summary>
        [TestMethod]
        public void TestJsonConstructor()
        {
            var valuationInputs = new ValuationInputs(measureListSerializedJson, nodeValuationSetSerializedJson);
            Assert.IsNotNull(valuationInputs.MeasureSetsInput);
            Assert.IsNotNull(valuationInputs.NodeOverrides);
            Assert.IsNotNull(valuationInputs.ValuationInputsFingerprint);

            valuationInputs = new ValuationInputs(measureListSerializedJson, null);
            Assert.IsNull(valuationInputs.NodeOverrides);
        }

        /// <summary>
        /// Verify we can create a campaign definition from the valuation inputs
        /// when not groups or pinning are specified and no node overrides are present.
        /// </summary>
        [TestMethod]
        public void TestCreateCampaignDefinitionDefaults()
        {
            var maxValuation = 10;
            var dontcare = 50;
            var valuationInputs = BuildValuationsInputs(maxValuation, dontcare);
            var campaignDefinition = valuationInputs.CreateCampaignDefinition();
            
            // Default explicit valuations are the measures
            Assert.AreEqual(2, campaignDefinition.ExplicitValuations.Count);
            Assert.IsTrue(campaignDefinition.ExplicitValuations.ContainsKey(TestMeasureSet1));
            Assert.IsTrue(campaignDefinition.ExplicitValuations.ContainsKey(TestMeasureSet2));

            // MaxPersonaValuation comes from MeasureSetsInput MaxValuation
            Assert.AreEqual(maxValuation, campaignDefinition.MaxPersonaValuation);
            
            // Groups and pinned measures should be empty
            Assert.AreEqual(0, campaignDefinition.MeasureGroupings.Count);
            Assert.AreEqual(0, campaignDefinition.PinnedMeasures.Count);
        }

        /// <summary>
        /// Verify explicit valuations are correctly calculated
        /// </summary>
        [TestMethod]
        public void TestCreateCampaignDefinitionExplicitValuations()
        {
            var dontcare = 10;
            var valuationInputs = BuildValuationsInputs(dontcare, 50);
            var campaignDefinition = valuationInputs.CreateCampaignDefinition();

            // explicit valuation is a function of base valuation input IntegerPart((base + 1) / 2) / 100
            Assert.AreEqual(.25m, campaignDefinition.ExplicitValuations[TestMeasureSet1]);

            // Bump the base valuation by one (valuation increases)
            valuationInputs = BuildValuationsInputs(10, 51);
            campaignDefinition = valuationInputs.CreateCampaignDefinition();
            Assert.AreEqual(.26m, campaignDefinition.ExplicitValuations[TestMeasureSet1]);

            // Drop the base valuation by one (same result as 50)
            valuationInputs = BuildValuationsInputs(10, 49);
            campaignDefinition = valuationInputs.CreateCampaignDefinition();
            Assert.AreEqual(.25m, campaignDefinition.ExplicitValuations[TestMeasureSet1]);
        }
        
        /// <summary>
        /// Verify we can create a campaign definition from the valuation inputs
        /// with overrides.
        /// </summary>
        [TestMethod]
        public void TestCreateCampaignDefinitionWithOverrides()
        {
            var overrideValuation = .1m;
            var dontcare = 10;
            var valuationInputs = BuildValuationsInputs(dontcare, dontcare, true, overrideValuation);
            var campaignDefinition = valuationInputs.CreateCampaignDefinition();

            // Override valuation is the value specified in the override
            Assert.AreEqual(3, campaignDefinition.ExplicitValuations.Count);
            Assert.AreEqual(overrideValuation, campaignDefinition.ExplicitValuations[TestMeasureSet12]);
            Assert.IsTrue(campaignDefinition.ExplicitValuations.ContainsKey(TestMeasureSet1));
            Assert.IsTrue(campaignDefinition.ExplicitValuations.ContainsKey(TestMeasureSet2));
        }

        /// <summary>
        /// Verify we can create a campaign definition from the valuation inputs
        /// with groups and pinned measures.
        /// </summary>
        [TestMethod]
        public void TestCreateCampaignDefinitionWithGroupsAndPins()
        {
            var dontcare = 10;
            var valuationInputs = BuildValuationsInputs(dontcare, dontcare);
            var measures = valuationInputs.MeasureSetsInput.Measures.ToList();
            measures.Add(new MeasuresInput { Measure = 3, Group = "g1", Pinned = false });
            measures.Add(new MeasuresInput { Measure = 4, Group = "g1", Pinned = false });
            measures.Add(new MeasuresInput { Measure = 5, Group = "g2", Pinned = true });
            valuationInputs.MeasureSetsInput.Measures = measures;

            var campaignDefinition = valuationInputs.CreateCampaignDefinition();

            // Groups and pinned measures should be empty
            Assert.AreEqual(3, campaignDefinition.MeasureGroupings.Count);
            Assert.AreEqual("g1", campaignDefinition.MeasureGroupings[3]);
            Assert.AreEqual("g1", campaignDefinition.MeasureGroupings[4]);
            Assert.AreEqual("g2", campaignDefinition.MeasureGroupings[5]);
            Assert.AreEqual(1, campaignDefinition.PinnedMeasures.Count);
            Assert.AreEqual(5, campaignDefinition.PinnedMeasures.First());
        }

        /// <summary>Test for DeserializeMeasuresJson success</summary>
        [TestMethod]
        public void TestDeserializeMeasuresJson()
        {
            var measuresInputs = ValuationInputs.DeserializeMeasuresJson(measureListSerializedJson);
            Assert.IsNotNull(measuresInputs);
            Assert.AreEqual(5, measuresInputs.IdealValuation);
            Assert.AreEqual(10, measuresInputs.MaxValuation);
            Assert.AreEqual(3, measuresInputs.Measures.Count());
            Assert.IsTrue(measuresInputs.Measures.All(m => !m.Pinned));
            Assert.IsTrue(measuresInputs.Measures.All(m => m.Group == string.Empty));
            Assert.IsTrue(measuresInputs.Measures.Single(m => m.Measure == 1).Valuation == 10);
            Assert.IsTrue(measuresInputs.Measures.Single(m => m.Measure == 2).Valuation == 50);
            Assert.IsTrue(measuresInputs.Measures.Single(m => m.Measure == 3).Valuation == 75);
        }

        /// <summary>Test for DeserializeMeasuresJson empty json.</summary>
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public void TestDeserializeMeasuresJsonEmpty()
        {
            ValuationInputs.DeserializeMeasuresJson(string.Empty);
        }

        /// <summary>
        /// Test for DeserializeNodeValuationsSetJson
        /// </summary>
        [TestMethod]
        public void TestDeserializeNodeValuationsSetJson()
        {
            var nodeValuations = ValuationInputs.DeserializeNodeOverridesJson(nodeValuationSetSerializedJson);
            Assert.IsNotNull(nodeValuations);
            Assert.AreEqual(2, nodeValuations.Count);
            Assert.AreEqual(60, nodeValuations[new MeasureSet(new long[] { 1, 2 })]);
        }

        /// <summary>Test for DeserializeNodeValuationsSetJson empty json returns null.</summary>
        [TestMethod]
        public void TestDeserializeNodeValuationsSetJsonEmpty()
        {
            var overrides = ValuationInputs.DeserializeNodeOverridesJson(string.Empty);
            Assert.IsNull(overrides);
        }

        /// <summary>Test for DeserializeNodeValuationsSetJson empty json collection.</summary>
        [TestMethod]
        public void TestDeserializeNodeValuationsSetJsonEmptyCollection()
        {
            var overrides = ValuationInputs.DeserializeNodeOverridesJson("[]");
            Assert.AreEqual(0, overrides.Count);
        }

        /// <summary>Verify we can serialize valuations to json.</summary>
        [TestMethod]
        public void TestSerializeValuationsToJson()
        {
            var m1 = new MeasureSet(new long[] { 1, 2 });
            var m2 = new MeasureSet(new long[] { 1, 3 });
            var valuations = new Dictionary<MeasureSet, decimal>
                {
                    { m1, 1.2m },
                    { m2, 1.3m },
                };

            var serializedTypeDef = new { NodeValuationSet = new[] { new { MeasureSet = new MeasureSet(), MaxValuation = 0m } } };
            var nodeValuationsJson = ValuationInputs.SerializeValuationsToJson(valuations);
            var nodeValuationsRoundTrip = AppsJsonSerializer.DeserializeAnonymousType(nodeValuationsJson, serializedTypeDef);
            Assert.IsNotNull(nodeValuationsRoundTrip);
            Assert.AreEqual(1.2m, nodeValuationsRoundTrip.NodeValuationSet[0].MaxValuation);
            Assert.AreEqual(m1, nodeValuationsRoundTrip.NodeValuationSet[0].MeasureSet);
            Assert.AreEqual(1.3m, nodeValuationsRoundTrip.NodeValuationSet[1].MaxValuation);
            Assert.AreEqual(m2, nodeValuationsRoundTrip.NodeValuationSet[1].MeasureSet);
        }

        /// <summary>
        /// Reads test data from a csv
        /// </summary>
        private static void InitializeTestData()
        {
            // Set the measure list on the campaign
            measureListSerializedJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ValuationsTestFixture), "Resources.ValuationInputs_MeasuresSerialized.js");

            // Set the node valuation set on the campaign
            nodeValuationSetSerializedJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ValuationsTestFixture), "Resources.ValuationInputs_NodeValuationsSerialized.js");
        }

        /// <summary>Build valuation inputs for testing.</summary>
        /// <param name="maxValuation">The max Valuation.</param>
        /// <param name="baseValuation">The base Valuation to use on all measures.</param>
        /// <param name="includeOverrides">true to include overrides.</param>
        /// <param name="overrideValution">override valuation.</param>
        /// <returns>Valuation inputs object.</returns>
        private static ValuationInputs BuildValuationsInputs(
            decimal maxValuation, int baseValuation, bool includeOverrides = false, decimal overrideValution = 0)
        {
            var measures = TestMeasures.Select(m => new MeasuresInput { Measure = m, Valuation = baseValuation }).ToList();
            var measureSetsInput = new MeasureSetsInput
                {
                    IdealValuation = 5,
                    MaxValuation = maxValuation, 
                    Measures = measures
                };

            var nodeOverrides = includeOverrides ? 
                new Dictionary<MeasureSet, decimal>
                {
                    { TestMeasureSet12, overrideValution } 
                } 
                : null;

            return new ValuationInputs(measureSetsInput, nodeOverrides);
        }
    }
}
