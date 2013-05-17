//-----------------------------------------------------------------------
// <copyright file="BudgetAllocationsTestFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using ScheduledActivities;
using SimulatedDataStore;
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
    public class BudgetAllocationsTestFixture
    {
        /// <summary>
        /// a dynamic allocation service for testing
        /// </summary>
        private DynamicAllocationEngine dynamicAllocationEngine;

        /// <summary>
        /// Mock entity repository used for tests
        /// </summary>
        private IEntityRepository repository;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private string campaignEntityId;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private string companyEntityId;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private string userId;

        /// <summary>
        /// set of test measures
        /// </summary>
        private IEnumerable<long> testMeasures;

        /// <summary>
        /// set of test measure mappings as JSON
        /// </summary>
        private string testMeasureMapJson;

        /// <summary>
        /// a test measure sets input Json string
        /// </summary>
        private string testMeasureSetsInputJson;

        /// <summary>
        /// a test node valuation set Json string
        /// </summary>
        private string testNodeValuationSetJson;
        
        /// <summary>
        /// a test budget allocation outputs
        /// </summary>
        private BudgetAllocation testBudgetAllocation;

        /// <summary>
        /// Initialize the dynamic allocation service before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ConfigurationManager.AppSettings["DynamicAllocation.CleanupCampaignsRequestExpiry"] = "23:30:00";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateAllocationsRequestExpiry"] = "23:30:00";
            ConfigurationManager.AppSettings["Delivery.ExportDACampaignRequestExpiry"] = "23:30:00";
            
            AllocationParametersDefaults.Initialize();
            
            DynamicAllocationActivitiesTestHelpers.SetupMeasureSourceFactoryStub();

            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            SimulatedPersistentDictionaryFactory.Initialize();
            Scheduler.Registries = null;

            this.userId = new EntityId().ToString();
            this.campaignEntityId = new EntityId().ToString();
            this.companyEntityId = new EntityId().ToString();
            this.repository = MockRepository.GenerateMock<IEntityRepository>();

            // create a number of measures 
            var measureCount = 10;
            this.testMeasures = Enumerable.Range(1, measureCount).Select(m => (long)m).ToList();

            var measureSetsInput = new MeasureSetsInput
            {
                IdealValuation = 5,
                MaxValuation = 10,
                Measures = this.testMeasures.Select(m => new MeasuresInput { Measure = m, Valuation = 50 })
            };

            var nodeOverrides = new Dictionary<MeasureSet, Tuple<decimal, decimal>>
            {
                { new MeasureSet { 1, 2 }, new Tuple<decimal, decimal>(10, 10) } 
            };

            this.testMeasureSetsInputJson = SerializeMeasureSetsInputJson(measureSetsInput);
            this.testNodeValuationSetJson = SerializeNodeOverridesToJson(nodeOverrides);
            var valuationInputs = new ValuationInputs(this.testMeasureSetsInputJson, this.testNodeValuationSetJson);
            var campaign = valuationInputs.CreateCampaignDefinition();

            var testMeasureMap = this.testMeasures.ToDictionary(
                m => m,
                m => (IDictionary<string, object>)new Dictionary<string, object>
                {
                    { MeasureValues.DataProvider, "Lotame" },
                    { MeasureValues.DataCost, .25 },
                });

            this.testMeasureMapJson = AppsJsonSerializer.SerializeObject(testMeasureMap);

            this.dynamicAllocationEngine = new DynamicAllocationEngine(new MeasureMap(testMeasureMap));
            var valuations = this.dynamicAllocationEngine.GetValuations(campaign);
 
            this.testBudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = valuations.ToDictionary(
                    v => v.Key, 
                    v => new PerNodeBudgetAllocationResult { Valuation = v.Value }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = new AllocationParameters(),
                TotalBudget = 1000,
                RemainingBudget = 1000,
                CampaignStart = new DateTime(2011, 12, 31, 0, 0, 0, DateTimeKind.Utc).AddDays(-3),
                CampaignEnd = new DateTime(2011, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                PeriodDuration = TimeSpan.FromHours(24),
            };
        }

        /// <summary>
        /// Tests for GetBudgetAllocationsActivity
        /// </summary>
        [TestMethod]
        public void GetBudgetAllocationsTest()
        {
            var budgetAllocation = this.testBudgetAllocation;

            // set up the allocationNodeMap to contain one value. We will make sure it gets reused.
            var testAllocationId = Guid.NewGuid().ToString("N");
            var testMeasureSet = budgetAllocation.PerNodeResults.Keys.ToList().First();
            var allocationNodeMap = new Dictionary<string, MeasureSet>
            {
                { testAllocationId, testMeasureSet }
            };

            this.repository = this.SetUpRepository(
                budgetAllocation.CampaignStart, 
                budgetAllocation.CampaignEnd, 
                budgetAllocation.RemainingBudget, 
                allocationNodeMap);

            // Create the activity
            var activity = Activity.CreateActivity(
                typeof(GetBudgetAllocationsActivity), 
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest)
                as DynamicAllocationActivity;
            Assert.IsNotNull(activity);

            var now = budgetAllocation.CampaignStart;
            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.userId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntityId },
                    { DynamicAllocationActivityValues.AllocationStartDate, now.ToString("o", CultureInfo.InvariantCulture) },
                    { DynamicAllocationActivityValues.IsInitialAllocation, true.ToString(CultureInfo.InvariantCulture) },
                    { "time", now.ToString("o", CultureInfo.InvariantCulture) }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);

            // Verify exports were scheduled
            var campaignsToExport = Scheduler.GetRegistry<Tuple<string, string, DeliveryNetworkDesignation>>(
                DeliveryNetworkSchedulerRegistries.CampaignsToExport);
            Assert.AreEqual(1, campaignsToExport[now].Count);
            Assert.AreEqual(2, campaignsToExport[now + new TimeSpan(3, 0, 0)].Count);
            Assert.AreEqual(3, campaignsToExport[now + new TimeSpan(6, 0, 0)].Count);
            Assert.AreEqual(4, campaignsToExport[now + new TimeSpan(9, 0, 0)].Count);
            Assert.AreEqual(5, campaignsToExport[now + new TimeSpan(12, 0, 0)].Count);
            Assert.AreEqual(6, campaignsToExport[now + new TimeSpan(15, 0, 0)].Count);
            Assert.AreEqual(7, campaignsToExport[now + new TimeSpan(18, 0, 0)].Count);
            Assert.AreEqual(8, campaignsToExport[now + new TimeSpan(21, 0, 0)].Count);
            Assert.AreEqual(8, campaignsToExport[now + new TimeSpan(24, 0, 0)].Count);

            var campaign = this.repository.GetEntity(null, new EntityId(this.campaignEntityId)) as CampaignEntity;
            var actualAllocationNodeMapAssociation = campaign.TryGetAssociationByName(DynamicAllocationEntityProperties.AllocationNodeMap);
            var actualAllocationNodeMapBlob = this.repository.TryGetEntity(null, actualAllocationNodeMapAssociation.TargetEntityId) as BlobEntity;
            var actualAllocationNodeMap = actualAllocationNodeMapBlob.DeserializeBlob<Dictionary<string, MeasureSet>>();

            var actualAllocationSetActiveAssociation = campaign.TryGetAssociationByName(DynamicAllocationEntityProperties.AllocationSetActive);
            var actualAllocationSetActiveBlob = this.repository.TryGetEntity(null, actualAllocationSetActiveAssociation.TargetEntityId) as BlobEntity;
            var actualAllocationSetActiveJson = actualAllocationSetActiveBlob.DeserializeBlob<string>();
            var actualAllocationSetActive = AppsJsonSerializer.DeserializeObject<BudgetAllocation>(actualAllocationSetActiveJson);

            // assert that the existing AllocationId got resused
            Assert.AreEqual(testMeasureSet, actualAllocationNodeMap[testAllocationId]);

            // assert the every measureSet got an allocationId
            Assert.IsTrue(actualAllocationSetActive.PerNodeResults.Keys.All(actualAllocationNodeMap.ContainsValue));
            
            // Assert that the period length is 3hours (since this is initial allocation and this setup has 2 sets of 4 exports)
            Assert.AreEqual(3, actualAllocationSetActive.PeriodDuration.TotalHours);

            // There should be 8 index entries (since this is initial allocation and this setup has 2 sets of 4 exports)
            var allocationHistoryIndexAssociation = campaign.TryGetAssociationByName(DynamicAllocationEntityProperties.AllocationHistoryIndex);
            var allocationHistoryIndexBlob = this.repository.TryGetEntity(null, allocationHistoryIndexAssociation.TargetEntityId) as BlobEntity;
            Assert.IsNotNull(allocationHistoryIndexBlob);
            var indexJson = allocationHistoryIndexBlob.DeserializeBlob<string>();
            var index = AppsJsonSerializer.DeserializeObject<List<HistoryElement>>(indexJson);
            Assert.AreEqual(8, index.Count);

            // In this setup, the single measure nodes should not get budgets or maxbids due to data costs
            Assert.AreEqual(10, actualAllocationSetActive.PerNodeResults.Count(pnr => pnr.Key.Count == 1));
            Assert.IsTrue(actualAllocationSetActive
                .PerNodeResults
                .Where(pnr => pnr.Key.Count == 1)
                .All(pnr => pnr.Value.PeriodTotalBudget == 0));
            Assert.IsTrue(actualAllocationSetActive
                .PerNodeResults
                .Where(pnr => pnr.Key.Count == 1)
                .All(pnr => pnr.Value.MaxBid == 0));
        }

        /// <summary>
        /// Deserialize Measures Json
        /// </summary>
        /// <param name="measureSetsInput">measureSetsInput to serialize</param>
        /// <returns>JSON string of measures</returns>
        internal static string SerializeMeasureSetsInputJson(MeasureSetsInput measureSetsInput)
        {
            const string JsonOuterFormat = @"{{""IdealValuation"": {0},""MaxValuation"": {1},""Measures"":[{2}]}}";
            const string JsonInnerFormat = @"{{""measureId"":{0},""valuation"":{1},""group"":""{2}"",""pinned"":{3}}}";

            var innerJsonStrings = new List<string>();
            foreach (var measuresInput in measureSetsInput.Measures)
            {
                innerJsonStrings.Add(
                    JsonInnerFormat.FormatInvariant(
                        measuresInput.Measure,
                        measuresInput.Valuation,
                        measuresInput.Group,
                        measuresInput.Pinned ? "true" : "false"));
            }

            return JsonOuterFormat.FormatInvariant(
                measureSetsInput.IdealValuation, measureSetsInput.MaxValuation, string.Join(",", innerJsonStrings));
        }

        /// <summary>Serialize Measures Json</summary>
        /// <param name="nodeOverrides">nodeOverrides to serialize</param>
        /// <returns>JSON string of nodeOverrides</returns>
        internal static string SerializeNodeOverridesToJson(Dictionary<MeasureSet, Tuple<decimal, decimal>> nodeOverrides)
        {
            const string JsonOuterFormat = @"[{0}]";
            const string JsonInnerFormat = @"{{""MeasureSet"":[{0}],""MaxValuation"":{1},""IdealValuation"":{2}}}";

            var innerJsonStrings = new List<string>();
            foreach (var nodeOverride in nodeOverrides)
            {
                var measureSetString = string.Join(",", nodeOverride.Key.Select(m => @"""{0}""".FormatInvariant(m)));
                innerJsonStrings.Add(JsonInnerFormat.FormatInvariant(
                        measureSetString,
                        nodeOverride.Value.Item1,
                        nodeOverride.Value.Item2));
            }

            return JsonOuterFormat.FormatInvariant(string.Join(",", innerJsonStrings));
        }

        /// <summary>
        /// Set up the mock repository
        /// </summary>
        /// <param name="campaignStart">the campaign start</param>
        /// <param name="campaignEnd">the campaign end</param>
        /// <param name="remainingBudget">the remaining budget</param>
        /// <param name="allocationNodeMap">the allocation node map</param>
        /// <returns>the mock repoository</returns>
        private IEntityRepository SetUpRepository(
            DateTime campaignStart, 
            DateTime campaignEnd, 
            decimal remainingBudget, 
            Dictionary<string, MeasureSet> allocationNodeMap)
        {
            // create Ids
            this.userId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
            this.campaignEntityId = new EntityId().ToString();
            this.companyEntityId = new EntityId().ToString();

            // create mock repository
            // this.repository = MockRepository.GenerateMock<IEntityRepository>();
            var simulatedRepository = new SimulatedEntityRepository();
            this.repository = simulatedRepository;

            // create test company
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                this.companyEntityId, "Test Company");
            companyEntity.SetDeliveryNetwork(DeliveryNetworkDesignation.AppNexus);
            simulatedRepository.SaveEntity(null, companyEntity);

            // create test campaign owner user
            var campaignOwnerEntity = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(), this.userId, "nobody@rc.dev");
            simulatedRepository.SaveUser(null, campaignOwnerEntity);

            // create allocation node map
            var allocationNodeMapEntityId = new EntityId();
            var allocationNodeMapBlob = BlobEntity.BuildBlobEntity(allocationNodeMapEntityId, allocationNodeMap) as IEntity;
            simulatedRepository.TrySaveEntity(null, allocationNodeMapBlob);
         
            this.SetUpCampaign(
                campaignStart,
                campaignEnd,
                this.testBudgetAllocation.TotalBudget,
                remainingBudget,
                allocationNodeMapEntityId);

            return simulatedRepository;
        }

        /// <summary>
        /// Adds various associations to the campaign entity
        /// </summary>
        /// <param name="campaignStart">the campaign start time</param>
        /// <param name="campaignEnd">the campaign end time</param>
        /// <param name="budget">the budget</param>
        /// <param name="remainingBudget">the remaining budget</param>
        /// <param name="allocationNodeMapEntityId">the allocationNodeMapEntityId</param>
        private void SetUpCampaign(
            DateTime campaignStart, 
            DateTime campaignEnd, 
            decimal budget, 
            decimal remainingBudget,
            EntityId allocationNodeMapEntityId)
        {
            var testCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId,
                "test",
                (long)budget,
                campaignStart,
                campaignEnd,
                "mike") as IEntity;
            testCampaign.SetOwnerId(this.userId);

            ((CampaignEntity)testCampaign).SetRemainingBudget(remainingBudget);

            testCampaign.SetPropertyByName(daName.MeasureList, this.testMeasureSetsInputJson);
            testCampaign.SetPropertyByName(daName.NodeValuationSet, this.testNodeValuationSetJson);

            // Set an approved version. This allows this test entity to act as both the current and
            // approved version (simulated repository does not distinguish).
            testCampaign.SetPropertyByName(daName.InputsApprovedVersion, 2);

            testCampaign.Associations.Add(new Association
            {
                ExternalName = DynamicAllocationEntityProperties.AllocationNodeMap,
                TargetEntityId = allocationNodeMapEntityId,
            });

            // TODO: make sure the purpose of this is still being met
            testCampaign.SetPropertyValueByName(
               daName.MeasureMap, new PropertyValue(PropertyType.String, this.testMeasureMapJson));

            this.repository.TrySaveEntity(null, testCampaign);
        }
    }
}
