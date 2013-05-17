//-----------------------------------------------------------------------
// <copyright file="ExportDynamicAllocationCampaignActivityFixture.cs" company="Rare Crowds Inc">
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
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using GoogleDfpActivities;
using GoogleDfpActivities.Exporters;
using GoogleDfpClient;
using GoogleDfpUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using TestUtilities;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Tests for ExportDynamicAllocationCampaignActivity</summary>
    [TestClass]
    public class ExportDynamicAllocationCampaignActivityFixture : DfpActivityFixtureBase<ExportDynamicAllocationCampaignActivity>
    {
        /// <summary>Random number generator for testing</summary>
        private static readonly Random Random = new Random();

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity testCampaign;

        /// <summary>Allocations blob entity for testing</summary>
        private BlobEntity testAllocationsBlob;

        /// <summary>EntityId for the test campaign</summary>
        private EntityId testCampaignEntityId;

        /// <summary>EntityId for the test allocations blob</summary>
        private EntityId testAllocationsBlobEntityId;

        /// <summary>Test initial budget allocations</summary>
        private BudgetAllocation testInitialAllocations;

        /// <summary>Test updated budget allocations</summary>
        private BudgetAllocation testUpdatedAllocations;

        /// <summary>A string representation of a list of Allocation IDs of campaigns to export</summary>
        private string[] testInitialExportAllocationIds;

        /// <summary>A string representation of a list of Allocation IDs of campaigns to export</summary>
        private string[] testUpdatedExportAllocationIds;

        /// <summary>Created order id (used to delete on test cleanup)</summary>
        private long orderId;

        /// <summary>Gets the bytes of a 300x250 test GIF</summary>
        private byte[] TestImageBytes
        {
            get { return EmbeddedResourceHelper.GetEmbeddedResourceAsByteArray(this.GetType(), "Resources.test.gif"); }
        }

        /// <summary>Cleanup per-test-run objects</summary>
        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                if (this.orderId > 0)
                {
                    new GoogleDfpWrapper().DeleteOrder(this.orderId);
                }
            }
            catch
            {
            }
        }

        /// <summary>Initialize per-test object(s)/settings</summary>
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            this.CreateTestAllocations();
            this.CreateTestEntities();
            this.orderId = -1;
        }

        /// <summary>Test exporting a "typical" DA campaign</summary>
        [TestMethod]
        public void ExportDynamicAllocationCampaign()
        {
            var now = DateTime.UtcNow;
            var request = new ActivityRequest
            {
                Task = GoogleDfpActivityTasks.ExportDACampaign,
                Values =
                {
                    { EntityActivityValues.AuthUserId, Guid.NewGuid().ToString("N") },
                    { EntityActivityValues.CompanyEntityId, TestNetwork.AdvertiserCompanyEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.testCampaignEntityId },
                    { DynamicAllocationActivityValues.ExportAllocationsEntityId, this.testAllocationsBlobEntityId }
                }
            };

            var activity = this.CreateActivity();
            var result = activity.Run(request);
            ActivityTestHelpers.AssertValidSuccessResult(result);
            ActivityTestHelpers.AssertResultHasValues(
                result,
                EntityActivityValues.CampaignEntityId,
                GoogleDfpActivityValues.OrderId);

            Assert.IsTrue(long.TryParse(result.Values[GoogleDfpActivityValues.OrderId], out this.orderId));

            //// TODO: Verify order and line-items were created correctly in DFP
        }

        /// <summary>Create test allocations</summary>
        private void CreateTestAllocations()
        {
            // Initial allocations with some arbitrary values for exporting.
            // The last allocation is budget-less and should not be created.
            this.testInitialAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(5500, 55, 70, 50, 3200, 4.25m),
                    this.CreateTestAllocation(1250, 23, 30, 20, 1650, 1.50m),
                    this.CreateTestAllocation(1350, 18, 22, 16, 1400, 5.25m),
                    this.CreateTestAllocation(1175, 10, 13, 8, 1234, 3.75m),
                    this.CreateTestAllocation(0, 0, 0, 0, 600, 3.25m),
                }
                .ToDictionary()
            };

            // Updated allocations have the same measure sets with value changes.
            // Value changes include no export budget for the first allocation to
            // test deactivation.
            var initialMeasureSets = this.testInitialAllocations
                .PerNodeResults
                .Keys
                .ToArray();
            var initialAllocationIds = this.testInitialAllocations
                .PerNodeResults
                .Values
                .Select(allocation => allocation.AllocationId)
                .ToArray();

            // create an intial string list of allocation IDs to export
            // the nodes to export should not be defined as those with export budget 
            // - it will be a subset of those in general
            this.testInitialExportAllocationIds =
                this.testInitialAllocations.PerNodeResults
                .Where(allocation => allocation.Value.ExportBudget > 0)
                .Select(allocation => allocation.Value.AllocationId)
                .ToArray();

            this.testUpdatedAllocations = new BudgetAllocation
            {
                AnticipatedSpendForDay = 10000.00m,
                PerNodeResults = new[]
                {
                    this.CreateTestAllocation(initialMeasureSets[0], initialAllocationIds[0], 0, 0, 0, 0, 3500, 3.75m),
                    this.CreateTestAllocation(initialMeasureSets[1], initialAllocationIds[1], 2250, 33, 36, 30, 1825, 4.25m),
                    this.CreateTestAllocation(initialMeasureSets[2], initialAllocationIds[2], 1250, 20, 24, 18, 1500, 5.25m),
                    this.CreateTestAllocation(initialMeasureSets[3], initialAllocationIds[3], 1350, 18, 21, 16, 2345, 3.75m),
                    this.CreateTestAllocation(initialMeasureSets[4], initialAllocationIds[4], 2500, 26, 29, 23, 600, 4.25m),
                }
                .ToDictionary()
            };

            // create an updated string list of allocation IDs to export
            // the nodes to export should not be defined as those with export budget 
            // - it will be a subset of those in general
            this.testUpdatedExportAllocationIds =
                this.testUpdatedAllocations.PerNodeResults
                .Where(allocation => allocation.Value.ExportBudget > 0)
                .Select(allocation => allocation.Value.AllocationId)
                .ToArray();
        }

        /// <summary>Creates a test allocation from the provided values</summary>
        /// <param name="periodImpressionCap">The period impression cap</param>
        /// <param name="periodMediaBudget">The period media budget</param>
        /// <param name="periodTotalBudget">The period total budget</param>
        /// <param name="exportBudget">The export budget</param>
        /// <param name="lifetimeMediaSpend">The lifetime media spend</param>
        /// <param name="maxBid">The max bid</param>
        /// <returns>The allocation</returns>
        private KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> CreateTestAllocation(
            long periodImpressionCap,
            decimal periodMediaBudget,
            decimal periodTotalBudget,
            decimal exportBudget,
            decimal lifetimeMediaSpend,
            decimal maxBid)
        {
            var measureSet = new MeasureSet();

            measureSet.Add(new long[]
            {
                TestNetwork.LocationMeasures.Random(),
                TestNetwork.AdUnitMeasures.Random(),
                TestNetwork.PlacementMeasures.Random(),
                TestNetwork.TechnologyMeasures.Random()
            });

            return this.CreateTestAllocation(
                measureSet,
                Guid.NewGuid().ToString("N"),
                periodImpressionCap,
                periodMediaBudget,
                periodTotalBudget,
                exportBudget,
                lifetimeMediaSpend,
                maxBid);
        }

        /// <summary>Creates a test allocation from the provided values</summary>
        /// <param name="measureSet">The measure set</param>
        /// <param name="allocationId">The allocation id</param>
        /// <param name="periodImpressionCap">The period impression cap</param>
        /// <param name="periodMediaBudget">The period media budget</param>
        /// <param name="periodTotalBudget">The period total budget</param>
        /// <param name="exportBudget">The export budget</param>
        /// <param name="lifetimeMediaSpend">The lifetime media spend</param>
        /// <param name="maxBid">The max bid</param>
        /// <returns>The allocation</returns>
        private KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult> CreateTestAllocation(
            MeasureSet measureSet,
            string allocationId,
            long periodImpressionCap,
            decimal periodMediaBudget,
            decimal periodTotalBudget,
            decimal exportBudget,
            decimal lifetimeMediaSpend,
            decimal maxBid)
        {
            return new KeyValuePair<MeasureSet, PerNodeBudgetAllocationResult>(
                measureSet,
                new PerNodeBudgetAllocationResult
                {
                    AllocationId = allocationId,
                    PeriodImpressionCap = periodImpressionCap,
                    PeriodMediaBudget = periodMediaBudget,
                    PeriodTotalBudget = periodTotalBudget,
                    ExportBudget = exportBudget,
                    LifetimeMediaSpend = lifetimeMediaSpend,
                    MaxBid = maxBid
                });
        }

        /// <summary>Create the test entities</summary>
        private void CreateTestEntities()
        {
            this.testCampaign = EntityTestHelpers.CreateTestCampaignEntity(
                (this.testCampaignEntityId = new EntityId()).ToString(),
                "Test Campaign - " + this.UniqueId,
                10000,
                DateTime.UtcNow,
                DateTime.UtcNow + new TimeSpan(7, 0, 0, 0),
                "Test Persona - " + this.UniqueId);

            this.testAllocationsBlob = BlobEntity.BuildBlobEntity<string>(
                this.testAllocationsBlobEntityId = new EntityId(),
                JsonConvert.SerializeObject(this.testInitialAllocations));

            var creativeEntity = EntityTestHelpers.CreateTestImageAdCreativeEntity(
                new EntityId(),
                "Test Creative - " + this.UniqueId,
                300,
                250,
                "http://www.rarecrowds.com/",
                this.TestImageBytes);
            this.testCampaign.AssociateEntities(
                "Creative",
                "image creative",
                new HashSet<IEntity>(new[] { creativeEntity }),
                AssociationType.Relationship,
                true);

            this.AddEntitiesToMockRepository(
                TestNetwork.AdvertiserCompanyEntity,
                this.testCampaign,
                this.testAllocationsBlob,
                creativeEntity);

            var creativeId = new DfpCreativeExporter(
                TestNetwork.AdvertiserCompanyEntity,
                creativeEntity)
                .CreateCreative();
            creativeEntity.SetDfpCreativeId(creativeId);
        }
    }
}
