//-----------------------------------------------------------------------
// <copyright file="BudgetReallocationsTestFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
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
    public class BudgetReallocationsTestFixture
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
        private EntityId campaignEntityId;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private EntityId companyEntityId;

        /// <summary>
        /// Campaign Id used in the tests
        /// </summary>
        private string userId;

        /// <summary>
        /// set of test measures
        /// </summary>
        private List<long> testMeasures;

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
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "00:00:00|00:04:00";
            ConfigurationManager.AppSettings["DynamicAllocation.CleanupCampaignsRequestExpiry"] = "23:30:00";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateAllocationsRequestExpiry"] = "23:30:00";
            ConfigurationManager.AppSettings["AppNexus.ExportDACampaignRequestExpiry"] = "23:30:00";

            AllocationParametersDefaults.Initialize();

            DynamicAllocationActivitiesTestHelpers.SetupMeasureSourceFactoryStub();
            
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            SimulatedPersistentDictionaryFactory.Initialize();
            Scheduler.Registries = null;

            this.userId = new EntityId().ToString();
            this.campaignEntityId = new EntityId();
            this.companyEntityId = new EntityId();
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

            this.testMeasureSetsInputJson = BudgetAllocationsTestFixture.SerializeMeasureSetsInputJson(measureSetsInput);
            this.testNodeValuationSetJson = BudgetAllocationsTestFixture.SerializeNodeOverridesToJson(nodeOverrides);
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
                    v => new PerNodeBudgetAllocationResult
                    {
                        Valuation = v.Value,
                    }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                AllocationParameters = new AllocationParameters(),
                TotalBudget = 1000,
                RemainingBudget = 1000,
                CampaignStart = new DateTime(2011, 12, 31, 0, 0, 0, DateTimeKind.Utc).AddDays(-3),
                CampaignEnd = new DateTime(2011, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                PeriodStart = new DateTime(2011, 12, 31).AddDays(-2),
                PeriodDuration = TimeSpan.FromHours(12),
            };

            // Make sure at least one node that will not be filtered has been exported
            this.testBudgetAllocation.PerNodeResults.First(pnr => pnr.Key.Count > 1).Value.ExportCount = 1;
        }

        /// <summary>
        /// Tests for GetBudgetAllocationsActivity
        /// </summary>
        [TestMethod]
        public void GetBudgetReallocationsTest()
        {
            var budgetAllocationOutputs = this.testBudgetAllocation;

            // set up the allocationNodeMap to contain one value. We will make sure it gets reused.
            var testAllocationId = Guid.NewGuid().ToString("N");
            var testMeasureSet = budgetAllocationOutputs.PerNodeResults.Keys.ToList().First();
            var allocationNodeMap = new Dictionary<string, MeasureSet>
            {
                { testAllocationId, testMeasureSet }
            };

            // set up entities (company, campaign, blobs)
            var allocationNodeMapEntityId = new EntityId();
            var activeAllocationEntityId = new EntityId();

            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                this.companyEntityId,
                "Test Company");

            var allocationNodeMapBlob = BlobEntity.BuildBlobEntity(allocationNodeMapEntityId, allocationNodeMap);
            var activeAllocationBlob = BlobEntity.BuildBlobEntity(
                activeAllocationEntityId,
                AppsJsonSerializer.SerializeObject(this.testBudgetAllocation));
           
            var oldOutputsEntityId = new EntityId();
            var index = new List<HistoryElement> 
            { 
                new HistoryElement 
                { 
                    AllocationStartTime = budgetAllocationOutputs.PeriodStart.ToString("o"), 
                    AllocationOutputsId = oldOutputsEntityId.ToString()
                } 
            };
            var indexJson = AppsJsonSerializer.SerializeObject(index);
            var indexEntityId = new EntityId();
            var indexBlob = BlobEntity.BuildBlobEntity(indexEntityId, indexJson);
           
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId,
                "test",
                (long)budgetAllocationOutputs.TotalBudget,
                budgetAllocationOutputs.CampaignStart,
                budgetAllocationOutputs.CampaignEnd,
                "mike");
            campaignEntity.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.DeliveryNetwork,
                DeliveryNetworkDesignation.AppNexus.ToString());
            campaignEntity.SetPropertyByName(daName.MeasureList, this.testMeasureSetsInputJson);
            campaignEntity.SetPropertyByName(daName.NodeValuationSet, this.testNodeValuationSetJson);

            // Set an approved version. This allows this test entity to act as both the current and
            // approved version (simulated repository does not distinguish).
            campaignEntity.SetPropertyByName(daName.InputsApprovedVersion, 2);

            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = DynamicAllocationEntityProperties.AllocationNodeMap,
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = allocationNodeMapEntityId,
                TargetExternalType = "???"
            });
            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = DynamicAllocationEntityProperties.AllocationHistoryIndex,
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = indexEntityId,
                TargetExternalType = "???"
            });
            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = DynamicAllocationEntityProperties.AllocationSetActive,
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = activeAllocationEntityId,
                TargetExternalType = "???"
            }); 
        
            campaignEntity.SetRemainingBudget(campaignEntity.Budget);

            campaignEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.MeasureMap,
                new PropertyValue(PropertyType.String, this.testMeasureMapJson));

            // set up the repository mocks
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, companyEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignEntityId, campaignEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, allocationNodeMapEntityId, allocationNodeMapBlob, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, indexEntityId, indexBlob, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, activeAllocationEntityId, activeAllocationBlob, false);

            // TODO: this behavior should be fleshed out a bit 
            IEntity actualAllocationNodeMapBlob = null;
            IEntity newIndexBlob = null;
            Action<IEntity> saveSideEffect = e =>
            {
                if ((string)e.ExternalName == DynamicAllocationEntityProperties.AllocationNodeMap)
                {
                    actualAllocationNodeMapBlob = e;
                }

                if ((string)e.ExternalName == DynamicAllocationEntityProperties.AllocationHistoryIndex)
                {
                    newIndexBlob = e;
                }
            };

            RepositoryStubUtilities.SetupSaveEntityStub(this.repository, saveSideEffect, false);

            // create an allocationsBlob with a LastModifiedDate to return in the ref call of SaveBlob
            // TODO: contrive this to be the same as the one created in the activity (the EntityId at least will be different currently)
            var newBudgetAllocation = this.dynamicAllocationEngine.GetBudgetAllocations(budgetAllocationOutputs);

            // Mocks adding a LastModifiedDate when a blob is saved
            this.repository.Stub(f => f.SaveEntity(
                Arg<RequestContext>.Is.Anything, 
                Arg<BlobEntity>.Is.Anything))
                .WhenCalled(a => ((IEntity)a.Arguments[1]).LastModifiedDate = DateTime.Now);

            // Create the activity
            var activity = Activity.CreateActivity(
                typeof(GetBudgetAllocationsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest)
                as DynamicAllocationActivity;
            Assert.IsNotNull(activity);
      
            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.userId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntityId },
                    { DynamicAllocationActivityValues.AllocationStartDate, this.testBudgetAllocation.PeriodStart.ToString("o") },
                    { DynamicAllocationActivityValues.IsInitialAllocation, "false" }
                }
            };

            var result = activity.Run(request);

            // Verify the result
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);

            // Verify export was scheduled
            var campaignsToExport = Scheduler.GetRegistry<Tuple<string, string, DeliveryNetworkDesignation>>(
                DeliveryNetworkSchedulerRegistries.CampaignsToExport);
            Assert.AreEqual(1, campaignsToExport[DateTime.UtcNow].Count);

            var actualAllocationNodeMap = ((BlobEntity)actualAllocationNodeMapBlob).DeserializeBlob<Dictionary<string, MeasureSet>>();
            var newIndexJson = ((BlobEntity)newIndexBlob).DeserializeBlob<string>();
            var newIndex = AppsJsonSerializer.DeserializeObject<List<HistoryElement>>(newIndexJson);

            // assert that the existing AllocationId got resused
            Assert.AreEqual(testMeasureSet, actualAllocationNodeMap[testAllocationId]);

            // assert the every measureSet got an allocationId
            Assert.IsTrue(newBudgetAllocation.PerNodeResults.Keys.All(actualAllocationNodeMap.ContainsValue));
            Assert.IsTrue(newBudgetAllocation.Phase > 0.0000001);

            // Assert that the period length is 12hours
            Assert.AreEqual(12, budgetAllocationOutputs.PeriodDuration.TotalHours);

            Assert.AreEqual(2, newIndex.Count);
            Assert.AreEqual(oldOutputsEntityId, new EntityId(newIndex[1].AllocationOutputsId));
            Assert.AreEqual(
                request.Values[DynamicAllocationActivityValues.AllocationStartDate],
                newIndex[0].AllocationStartTime);
        }
    }
}
