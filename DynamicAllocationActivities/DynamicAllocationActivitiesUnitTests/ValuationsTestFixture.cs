//-----------------------------------------------------------------------
// <copyright file="ValuationsTestFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using Diagnostics;
using Diagnostics.Testing;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using ScheduledActivities;
using TestUtilities;
using Utilities.Serialization;
using Utilities.Storage.Testing;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// Tests for valuations activities
    /// </summary>
    [TestClass]
    public class ValuationsTestFixture
    {
        /// <summary>Expected cached valuations.</summary>
        private static readonly Dictionary<MeasureSet, decimal> ExpectedCachedValuations = new Dictionary<MeasureSet, decimal>
            {
                { new MeasureSet(new long[] { 1, 2 }), 60.0m },
                { new MeasureSet(new long[] { 1, 3 }), 6.23m },
                { new MeasureSet(new long[] { 1, 2, 3 }), 10.00m },
                { new MeasureSet(new long[] { 2, 3 }), 60.0m },
                { new MeasureSet(new long[] { 1 }), 0.5m },
                { new MeasureSet(new long[] { 2 }), 0.25m },
                { new MeasureSet(new long[] { 3 }), 0.38m },
            };

        /// <summary>Test entity repository stub</summary>
        private IEntityRepository repositoryStub;

        /// <summary>Test company</summary>
        private CompanyEntity companyEntity;

        /// <summary>Current version test campaign</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Approved test campaign</summary>
        private CampaignEntity campaignEntityApproved;

        /// <summary>Test campaign owner</summary>
        private UserEntity campaignOwnerEntity;

        /// <summary>One time initialization of fixture</summary>
        /// <param name="notUsed">The test context.</param>
        [ClassInitialize]
        public static void FixtureInitialize(TestContext notUsed)
        {
            AllocationParametersDefaults.Initialize();
            Scheduler.Registries = null;
            LogManager.Initialize(new[] { new TestLogger() });
            SimulatedPersistentDictionaryFactory.Initialize();
            DynamicAllocationActivitiesTestHelpers.SetupMeasureSourceFactoryStub();
        }

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            // Setup test company/campaign/owner
            this.CreateTestEntities();

            // Setup entity repository stub
            this.SetupRepositoryStub();
        }

        /// <summary>
        /// Tests for ApproveValuationsInputs activity
        /// </summary>
        [TestMethod]
        public void ApproveValuationsInputsTest()
        {
            CampaignEntity calledCampaign = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CampaignEntity>(this.repositoryStub, e => calledCampaign = e, false);
            
            var submitActivityRequestCalled = false;
            SubmitActivityRequestHandler onSubmit = (r, s) => { submitActivityRequestCalled = true; return true; };

            // Create the activity, include the SubmitActivityRequest handler
            var activity = Activity.CreateActivity(
                typeof(ApproveValuationInputsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repositoryStub } },
                onSubmit)
                as DynamicAllocationActivity;
            Assert.IsNotNull(activity);

            var request = new ActivityRequest
            {
                Task = "DAApproveValuationsInputs",
                Values =
                {
                    { "CompanyEntityId", this.companyEntity.ExternalEntityId.ToString() },
                    { "CampaignEntityId", this.campaignEntity.ExternalEntityId.ToString() },
                    { "AuthUserId", new EntityId() }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded);

            // verify approved version matches the version at the time the approve activity is called
            var expectedVersion = this.campaignEntity.GetPropertyByName<int>(daName.InputsApprovedVersion);
            var actualVersion = calledCampaign.GetPropertyByName<int>(daName.InputsApprovedVersion);
            Assert.AreEqual(expectedVersion, actualVersion);

            // Verify the SubmitRequest was NOT called
            Assert.IsFalse(submitActivityRequestCalled);

            // Verify an entry was created in the reallocation time-slotted registry for some time in the next 24 hours
            // TODO: be more precise as to when the entry is for
            var campaignsToReallocate = Scheduler.GetRegistry<Tuple<string, DateTime, bool>>(
                DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate);
            var reallocationEntries = campaignsToReallocate[DateTime.UtcNow.AddDays(1)];
            Assert.AreEqual(1, reallocationEntries.Count);
        }

        /// <summary>
        /// Happy path test for GetValuationsActivity
        /// </summary>
        [TestMethod]
        public void GetValuationsActivitySuccess()
        {
            // Remove the node overrides
            this.RemoveValuationEntry(daName.NodeValuationSet);

            // Get valuations for the approved version
            var request = new ActivityRequest
            {
                Task = "DAGetValuations",
                Values =
                {
                    { "Approved", "true" },
                    { "EntityId", this.campaignEntity.ExternalEntityId.ToString() },
                    { "ParentEntityId", this.companyEntity.ExternalEntityId.ToString() },
                    { "AuthUserId", new EntityId() }
                }
            };
            
            var activity = this.CreateTestActivity();
            Assert.IsNotNull(activity);
            var result = activity.Run(request);

            // Verify the result
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);

            // Verify the allocation output
            Assert.IsTrue(result.Values.ContainsKey("Valuations"));
            Assert.IsNotNull(result.Values["Valuations"]);

            // reinflate valuations from the activity result (the response valuations still correspond
            // to the DeserializeNodeOverridesJsonDeprecated format. This method can be moved into the
            // test fixture when it is no longer used elsewhere
            var actualValuations = DeserializeNodeOverridesJson(result.Values["Valuations"]);

            // Assert against expected valuations (for the approved version)
            var expectedValuationsOutputJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(ValuationsTestFixture), "Resources.ValuationInputs_GetValuationsOutput.js");
            var expectedValuations = DeserializeNodeOverridesJson(expectedValuationsOutputJson);
            Assert.IsTrue(expectedValuations.OrderBy(kvp => kvp.Key)
                .SequenceEqual(actualValuations.OrderBy(kvp => kvp.Key)));

            // Now get the valuations for the current version and assert they are not the same
            request.Values["Approved"] = "false";
            result = activity.Run(request);
            var actualCurrentValuations = DeserializeNodeOverridesJson(result.Values["Valuations"]);

            Assert.IsFalse(actualCurrentValuations.SequenceEqual(actualValuations));
        }

        /// <summary>
        /// Failure test for GetValuationsActivity
        /// </summary>
        [TestMethod]
        public void GetValuationsActivityFail()
        {
            // Clear the campaign properties so there are no valuation inputs (fail)
            this.campaignEntity.Properties.Clear();

            // Get current version valuations
            var request = new ActivityRequest
            {
                Task = "DAGetValuations",
                Values =
                {
                    { "EntityId", this.campaignEntity.ExternalEntityId.ToString() },
                    { "ParentEntityId", this.companyEntity.ExternalEntityId.ToString() },
                    { "AuthUserId", new EntityId() }
                }
            };

            var activity = this.CreateTestActivity();
            Assert.IsNotNull(activity);

            var result = activity.Run(request);

            // Verify error result
            Assert.IsNotNull(result.Error);
        }

        /// <summary>Contruction test of ValuationsCache class.</summary>
        [TestMethod]
        public void ValuationsCacheConstructor()
        {
            var cache = new ValuationsCache(this.repositoryStub);
            Assert.AreSame(this.repositoryStub, cache.Repository);
        }

        /// <summary>Happy path BuildValuationInputs</summary>
        [TestMethod]
        public void BuildValuationInputsSuccess()
        {
            // Build the valuation inputs
            var actualValuationInputs = ValuationsCache.BuildValuationInputs(
                this.campaignEntity);

            Assert.IsNotNull(actualValuationInputs);
            Assert.IsNotNull(actualValuationInputs.MeasureSetsInput);
            Assert.IsNotNull(actualValuationInputs.NodeOverrides);
            Assert.IsNotNull(actualValuationInputs.ValuationInputsFingerprint);
        }

        /// <summary>Fail BuildValuationInputs - no MeasureList</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void BuildValuationInputsMeasureListNotPresent()
        {
            // Remove the measure list
            this.RemoveValuationEntry(daName.MeasureList);

            // Build the valuation inputs
            ValuationsCache.BuildValuationInputs(this.campaignEntity);
        }

        /// <summary>BuildValuationInputs - no NodeValuationSet ok</summary>
        [TestMethod]
        public void BuildValuationInputsMissingNodeValuationSetOk()
        {
            // Remove the base valuation set
            this.RemoveValuationEntry(daName.NodeValuationSet);

            // Build the valuation inputs
            var actualValuationInputs = ValuationsCache.BuildValuationInputs(
                this.campaignEntity);

            Assert.IsNotNull(actualValuationInputs);
            Assert.IsNull(actualValuationInputs.NodeOverrides);
            Assert.IsNotNull(actualValuationInputs.ValuationInputsFingerprint);
        }

        /// <summary>Fail BuildValuationInputs - fail deserialization.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void BuildValuationInputsFailDeserialization()
        {
            this.campaignEntity.SetPropertyValueByName(daName.MeasureList, new PropertyValue("SomeBogusJson"));

            // Build the valuation inputs
            ValuationsCache.BuildValuationInputs(this.campaignEntity);
        }

        /// <summary>Fail TryBuildValuationInputs - valuations not found (properties or associations).</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void BuildValuationInputsValuationsNotFound()
        {
            // Remove the property valuations
            this.RemoveValuationEntry(daName.MeasureList);
            this.RemoveValuationEntry(daName.NodeValuationSet);

            // Build the valuation inputs
            ValuationsCache.BuildValuationInputs(this.campaignEntity);
        }

        /// <summary>Fail the call to DA that gets valuations with bad inputs.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void CreateValuationsFromInputsFailGetValuations()
        {
            // Set the measure list on the campaign
            var measureListSerializedJsonBad = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ValuationsTestFixture), "Resources.ValuationInputs_MeasuresSerialized_Bad.js");
            this.campaignEntity.SetPropertyValueByName(
                daName.MeasureList, new PropertyValue(PropertyType.String, measureListSerializedJsonBad));

            var dynamicAllocation = this.GetDynamicAllocationEngine();

            // Build the valuation inputs
            var valuationInputs = ValuationsCache.BuildValuationInputs(this.campaignEntity);

            // Try to create the valuations from bad inputs
            ValuationsCache.CreateValuationsFromInputs(
                dynamicAllocation, valuationInputs, this.campaignEntity);
        }

        /// <summary>Happy path test of getting cached valuations when they are present and not stale.</summary>
        [TestMethod] 
        public void GetValuationsUpToDate()
        {
            // Set up cached valuations different from default from CreateTestEntities to make sure we're really getting them
            var cachedValuations = new Dictionary<MeasureSet, decimal> { { new MeasureSet(new long[] { 1, 2, 3 }), 1.1m } };
            
            this.SetupCachedValuations(cachedValuations, true);
            var expectedFingerprint = this.campaignEntity.TryGetPropertyByName<string>(
                daName.ValuationInputsFingerprint, null);
            
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // We don't expect save to be called
            IList<EntityProperty> savedProperties = null;
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), null);

            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac);

            Assert.IsNull(savedProperties);
            Assert.AreEqual(expectedFingerprint, this.campaignEntity.TryGetPropertyByName<string>(daName.ValuationInputsFingerprint, null));
            AssertCachedValuations(cachedValuations, actualValuations, true);
        }

        /// <summary>Happy path test of getting cached valuations when they are present and stale.</summary>
        [TestMethod]
        public void GetValuationsStale()
        {
            // Set up cached valuations different from default from CreateTestEntities
            var cachedValuations = new Dictionary<MeasureSet, decimal> { { new MeasureSet(new long[] { 1, 2, 3 }), 1.1m } };
            this.SetupCachedValuations(cachedValuations, false);
            var originalFingerprint = this.campaignEntity.TryGetPropertyByName<string>(daName.ValuationInputsFingerprint, null);
            var expectedFingerprint = this.GetValuationInputsFingerprintFromInputs();

            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Capture saved properties
            IList<EntityProperty> savedProperties = null;
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), expectedEntityFilter);

            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac);

            Assert.AreNotEqual(originalFingerprint, (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.AreEqual(expectedFingerprint, (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.IsNotNull((string)savedProperties.Single(p => p.Name == daName.CachedValuations));
            AssertCachedValuations(cachedValuations, actualValuations, false);
            AssertCachedValuations(ExpectedCachedValuations, actualValuations, true);
        }

        /// <summary>Get valuations with suppressed save to cache when even if stale.</summary>
        [TestMethod]
        public void GetValuationsSuppressedCacheSave()
        {
            // Set up cached valuations different from default from CreateTestEntities
            var cachedValuations = new Dictionary<MeasureSet, decimal> { { new MeasureSet(new long[] { 1, 2, 3 }), 1.1m } };
            this.SetupCachedValuations(cachedValuations, false);
            
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac, true);

            this.repositoryStub.AssertWasNotCalled(f => f.TryUpdateEntity(
                Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Anything, Arg<IEnumerable<EntityProperty>>.Is.Anything));
            AssertCachedValuations(cachedValuations, actualValuations, false);
        }

        /// <summary>Get valuations when cached valuations not yet present.</summary>
        [TestMethod]
        public void GetValuationsCacheNotPresent()
        {
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Capture saved campaign
            IList<EntityProperty> savedProperties = null;
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), expectedEntityFilter);

            // Cached values shouldn't be there to begin with
            Assert.IsNull(this.campaignEntity.TryGetPropertyByName<string>(daName.ValuationInputsFingerprint, null));
            Assert.IsNull(this.campaignEntity.TryGetPropertyByName<string>(daName.CachedValuations, null));

            var valuationInputsFingerprint = this.GetValuationInputsFingerprintFromInputs();
            Assert.IsNotNull(valuationInputsFingerprint);

            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac);

            Assert.AreEqual(valuationInputsFingerprint, (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.IsNotNull((string)savedProperties.Single(p => p.Name == daName.CachedValuations));
            AssertCachedValuations(ExpectedCachedValuations, actualValuations, true);
        }

        /// <summary>Get valuations when fingerprint is correct but cached valuations not found.</summary>
        [TestMethod]
        public void GetValuationsFlaggedPresentButNotFound()
        {
            // Set up cached valutions flag
            this.SetupCachedValuations((Dictionary<MeasureSet, decimal>)null, true);

            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Capture saved campaign
            IList<EntityProperty> savedProperties = null;
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), expectedEntityFilter);

            // Cached values shouldn't be there to begin with but flag set
            var valuationInputsFingerprint = this.GetValuationInputsFingerprintFromInputs();
            Assert.AreEqual(valuationInputsFingerprint, this.campaignEntity.TryGetPropertyByName<string>(daName.ValuationInputsFingerprint, null));
            Assert.IsNull(this.campaignEntity.TryGetPropertyByName<string>(daName.CachedValuations, null));

            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac);

            Assert.AreEqual(valuationInputsFingerprint, (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.IsNotNull((string)savedProperties.Single(p => p.Name == daName.CachedValuations));
            AssertCachedValuations(ExpectedCachedValuations, actualValuations, true);
        }

        /// <summary>Error result if valuation inputs not found.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetValuationsInputsNotFound()
        {
            // Clear the campaign properties
            this.campaignEntity.Properties.Clear();

            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            var cache = new ValuationsCache(this.repositoryStub);
            cache.GetValuations(dac);
        }

        /// <summary>Get valuations when cache deserialization fails - this should cause them to be regenereted.</summary>
        [TestMethod]
        public void GetValuationsCachePresentButDeserializeFails()
        {
            // Set up cached valutions different from default from CreateTestEntities to make sure we're really getting them
            var bogusCachedValuationsJson = "Somethingbogusthiswaycomes";
            this.SetupCachedValuations(bogusCachedValuationsJson, true);

            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Capture saved campaign
            IList<EntityProperty> savedProperties = null;
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), expectedEntityFilter);

            var valuationInputsFingerprint = this.GetValuationInputsFingerprintFromInputs();
            Assert.IsNotNull(valuationInputsFingerprint);
            
            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac);

            Assert.AreEqual(valuationInputsFingerprint, (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.IsNotNull((string)savedProperties.Single(p => p.Name == daName.CachedValuations));
            Assert.AreNotEqual(bogusCachedValuationsJson, (string)savedProperties.Single(p => p.Name == daName.CachedValuations));
            AssertCachedValuations(ExpectedCachedValuations, actualValuations, true);
        }

        /// <summary>Get valuations are calculated but save cache fails.</summary>
        [TestMethod]
        public void GetValuationsSaveCacheFails()
        {
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Set up save stub to fail
            IList<EntityProperty> savedProperties = null;
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), expectedEntityFilter, false);

            var valuationInputsFingerprint = this.GetValuationInputsFingerprintFromInputs();
            Assert.IsNotNull(valuationInputsFingerprint);
            
            var cache = new ValuationsCache(this.repositoryStub);
            var actualValuations = cache.GetValuations(dac);

            // Benign failure - should not throw but valuations should be calculated.
            AssertCachedValuations(ExpectedCachedValuations, actualValuations, true);

            // Save should have been called with the cached valuations even if it failed
            Assert.AreEqual(valuationInputsFingerprint, (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.IsNotNull((string)savedProperties.Single(p => p.Name == daName.CachedValuations));
        }

        /// <summary>Successful save of cached valuations.</summary>
        [TestMethod]
        public void SaveCachedValuationsSaveSucceeds()
        {
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Capture saved campaign
            IList<EntityProperty> savedProperties = null;
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => savedProperties = c.ToList(), expectedEntityFilter);

            var cache = new ValuationsCache(this.repositoryStub);
            Assert.IsTrue(cache.SaveCachedValuations(
                dac, "ValuationsJson", "valuationInputsFingerprint"));

            Assert.AreEqual("valuationInputsFingerprint", (string)savedProperties.Single(p => p.Name == daName.ValuationInputsFingerprint));
            Assert.AreEqual("ValuationsJson", (string)savedProperties.Single(p => p.Name == daName.CachedValuations));
        }

        /// <summary>Failed save of cached valuations.</summary>
        [TestMethod]
        public void SaveCachedValuationsSaveFails()
        {
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);

            // Set up save stub to fail
            var expectedEntityFilter = new RepositoryEntityFilter(true, true, true, true);
            this.SetupSaveCampaignStub(c => { }, expectedEntityFilter, false);

            var cache = new ValuationsCache(this.repositoryStub);
            Assert.IsFalse(cache.SaveCachedValuations(
                dac, "ValuationsJson", "valuationInputsFingerprint"));
        }

        /// <summary>Assert that cached valuations match expected values</summary>
        /// <param name="expectedValuations">expected valuations</param>
        /// <param name="actualValuations">actual valuations</param>
        /// <param name="expected">True if we expect them to be equal.</param>
        private static void AssertCachedValuations(
            IDictionary<MeasureSet, decimal> expectedValuations, IDictionary<MeasureSet, decimal> actualValuations, bool expected)
        {
            Assert.AreEqual(
                expected,
                expectedValuations.OrderByDescending(p => p.Key).SequenceEqual(actualValuations.OrderByDescending(p => p.Key)));
        }

        /// <summary>
        /// Deserialize Node Valuation Set Json
        /// </summary>
        /// <param name="nodeValuationSetJson">node Valuation Set Json</param>
        /// <returns>Dictionary from MeasureSet to decimal</returns>
        private static IDictionary<MeasureSet, decimal> DeserializeNodeOverridesJson(string nodeValuationSetJson)
        {
            var jsonNodeValuationSetList = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(nodeValuationSetJson);
            var nodeValuationSetList = (jsonNodeValuationSetList["NodeValuationSet"] as ArrayList).ToArray();

            var nodeValuationSet = new Dictionary<MeasureSet, decimal>();
            foreach (IDictionary<string, object> nodeValuationDictionary in nodeValuationSetList)
            {
                var nodeValuationSetArrayList = (nodeValuationDictionary["MeasureSet"] as ArrayList).ToArray();
                var measureSet = nodeValuationSetArrayList.Select(measureId => Convert.ToInt64(measureId, CultureInfo.InvariantCulture));
                var value = Convert.ToDecimal(nodeValuationDictionary["MaxValuation"], CultureInfo.InvariantCulture);

                nodeValuationSet.Add(new MeasureSet(measureSet), value);
            }

            return nodeValuationSet;
        }

        /// <summary>Remove the given valuation entry from the campaign.</summary>
        /// <param name="entryName">The valuation entry name.</param>
        private void RemoveValuationEntry(string entryName)
        {
            // Remove the valuation entry on the campaign
            var valuationEntry = this.campaignEntity.TryGetEntityPropertyByName(entryName, null);
            this.campaignEntity.Properties.Remove(valuationEntry);

            // Remove the valuation entry on the campaign
            valuationEntry = this.campaignEntityApproved.TryGetEntityPropertyByName(entryName, null);
            this.campaignEntityApproved.Properties.Remove(valuationEntry);
        }

        /// <summary>Create the test company and campaign</summary>
        private void CreateTestEntities()
        {
            // Setup test company
            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId(),
                "Test Company");

            // Setup test campaign owner
            this.campaignOwnerEntity = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N"))),
                "nobody@rc.dev");

            // Setup a 'approved version' test campaign
            this.campaignEntityApproved = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(),
                "Test Campaign",
                10,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(2, 0, 0, 0),
                "mike");
            this.campaignEntityApproved.SetOwnerId(this.campaignOwnerEntity.UserId);
            this.campaignEntityApproved.LocalVersion = 1;

            // Set the measure list and node overrides
            var measureListSerializedJsonMod = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ValuationsTestFixture), "Resources.ValuationInputs_MeasuresSerialized.js");
            this.campaignEntityApproved.SetPropertyValueByName(
                daName.MeasureList, new PropertyValue(PropertyType.String, measureListSerializedJsonMod));

            var nodeValuationSetSerializedJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ValuationsTestFixture), "Resources.ValuationInputs_NodeValuationsSerialized.js");
            this.campaignEntityApproved.SetPropertyValueByName(
                daName.NodeValuationSet, new PropertyValue(PropertyType.String, nodeValuationSetSerializedJson));
            
            // Setup 'current version' test campaign
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                (EntityId)this.campaignEntityApproved.ExternalEntityId,
                this.campaignEntityApproved.ExternalName,
                (long)(decimal)this.campaignEntityApproved.Budget,
                this.campaignEntityApproved.StartDate,
                this.campaignEntityApproved.EndDate,
                "mike");
            this.campaignEntity.SetOwnerId(this.campaignOwnerEntity.UserId);
            this.campaignEntity.LocalVersion = 2;
            
            // Set the approved version property
            this.campaignEntity.SetPropertyByName(daName.InputsApprovedVersion, 1, PropertyFilter.Extended);

            // Set the measure list on the campaign
            var measureListSerializedJsonModified = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ValuationsTestFixture), "Resources.ValuationInputs_MeasuresSerializedModified.js");
            this.campaignEntity.SetPropertyValueByName(
                daName.MeasureList, new PropertyValue(PropertyType.String, measureListSerializedJsonModified));

            // Set the node valuation set on the campaign
            this.campaignEntity.SetPropertyValueByName(
                daName.NodeValuationSet, new PropertyValue(PropertyType.String, nodeValuationSetSerializedJson));
        }

        /// <summary>Create the GetValuationsActivity for testing</summary>
        /// <returns>The GetValuationsActivity</returns>
        private DynamicAllocationActivity CreateTestActivity()
        {
            return Activity.CreateActivity(
                typeof(GetValuationsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repositoryStub } },
                ActivityTestHelpers.SubmitActivityRequest)
                as DynamicAllocationActivity;
        }

        /// <summary>Setup campaign and company repository stubs.</summary>
        private void SetupRepositoryStub()
        {
            this.repositoryStub = MockRepository.GenerateStub<IEntityRepository>();

            // Setup two versions of the campaign
            var entityFilter = new RepositoryEntityFilter();
            entityFilter.AddVersionToEntityFilter(1);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repositoryStub, entityFilter, this.campaignEntity.ExternalEntityId, this.campaignEntityApproved, false);

            var entityFilterCurrent = new RepositoryEntityFilter();
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repositoryStub, entityFilterCurrent, this.campaignEntity.ExternalEntityId, this.campaignEntity, false);

            // Setup company, owner
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repositoryStub, this.companyEntity.ExternalEntityId, this.companyEntity, false);
            RepositoryStubUtilities.SetupGetUserStub(
                this.repositoryStub, this.campaignOwnerEntity.UserId, this.campaignOwnerEntity, false);
        }

        /// <summary>Setup a save campaign stub that captures the saved campaign.</summary>
        /// <param name="captureCampaign">delegate to capture campaign being saved.</param>
        /// <param name="expectedEntityFilter">The entity filter for the call.</param>
        /// <param name="saveSucceeds">True if the save should return a success result.</param>
        private void SetupSaveCampaignStub(
            Action<IEnumerable<EntityProperty>> captureCampaign,
            RepositoryEntityFilter expectedEntityFilter,
            bool saveSucceeds = true)
        {
            RepositoryStubUtilities.SetupTryUpdateEntityStub(
                this.repositoryStub, expectedEntityFilter, this.campaignEntity.ExternalEntityId, captureCampaign, !saveSucceeds);
        }

        /// <summary>Set up cached valuations.</summary>
        /// <param name="cachedValuations">The valuations to be cached.</param>
        /// <param name="isUpToDate">Value of cache flag.</param>
        private void SetupCachedValuations(Dictionary<MeasureSet, decimal> cachedValuations, bool isUpToDate)
        {
            string cachedValuationsJson = null;
            if (cachedValuations != null)
            {
                cachedValuationsJson = AppsJsonSerializer.SerializeObject(cachedValuations);
            }

            this.SetupCachedValuations(cachedValuationsJson, isUpToDate);
        }

        /// <summary>Set up cached valuations.</summary>
        /// <param name="cachedValuationsJson">The valuations json to be cached.</param>
        /// <param name="isUpToDate">True if valuations are up to date.</param>
        private void SetupCachedValuations(string cachedValuationsJson, bool isUpToDate)
        {
            if (cachedValuationsJson != null)
            {
                var cachedValuationsExtProperty = new EntityProperty(
                    daName.CachedValuations, cachedValuationsJson, PropertyFilter.Extended);
                this.campaignEntity.SetEntityProperty(cachedValuationsExtProperty);
            }

            // If we want stale valuations, set a bogus fingerprint, otherwise use a real one
            var valuationInputsFingerprint = "stalefingerprint";
            if (isUpToDate)
            {
                valuationInputsFingerprint = this.GetValuationInputsFingerprintFromInputs();
            }

            this.campaignEntity.SetPropertyByName(daName.ValuationInputsFingerprint, valuationInputsFingerprint);
        }

        /// <summary>Return the valuation inputs fingerprint for the inputs on a campaign entity.</summary>
        /// <returns>the fingerprint</returns>
        private string GetValuationInputsFingerprintFromInputs()
        {
            return new ValuationInputs(
                    this.campaignEntity.TryGetPropertyByName<string>(daName.MeasureList, null),
                    this.campaignEntity.TryGetPropertyByName<string>(daName.NodeValuationSet, null))
                    .ValuationInputsFingerprint;
        }

        /// <summary>Test method that mimics the behavior of the static call to build a DynamicAllocationEngine.</summary>
        /// <returns>A dynamic allocation engine.</returns>
        private IDynamicAllocationEngine GetDynamicAllocationEngine()
        {
            var dac = new DynamicAllocationCampaign(this.repositoryStub, this.companyEntity, this.campaignEntity);
            return dac.CreateDynamicAllocationEngine();
        }
    }
}
