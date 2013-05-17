// -----------------------------------------------------------------------
// <copyright file="IncrementExportCountsFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Activities;
using ActivityTestUtilities;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using GoogleDfpUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Serialization;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Unit test fixture for GetCampaignDeliveryDataActivity</summary>
    [TestClass]
    public class IncrementExportCountsFixture
    {
        /// <summary>Stubbed entity repository for testing</summary>
        private IEntityRepository repository;

        /// <summary>AuthUserId for testing</summary>
        private string userId;

        /// <summary>Company entity for testing</summary>
        private CompanyEntity companyEntity;

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Active allocation blob for testing</summary>
        private BlobEntity activeAllocationBlobEntity;

        /// <summary>DynamicAllocationCampaign for testing</summary>
        private DynamicAllocationCampaign dynamicAllocationCampaign;

        /// <summary>Active allocation for testing</summary>
        private BudgetAllocation activeAllocation;

        /// <summary>Exported allocation ids</summary>
        private string[] exportedAllocationIds;

        /// <summary>Unexported allocation ids</summary>
        private string[] unexportedAllocationIds;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            this.exportedAllocationIds = Enumerable.Range(0, 3).Select(i => Guid.NewGuid().ToString("N")).ToArray();
            this.unexportedAllocationIds = Enumerable.Range(0, 3).Select(i => Guid.NewGuid().ToString("N")).ToArray();

            var measureId = 0L;
            this.activeAllocation = new BudgetAllocation
            {
                PerNodeResults =
                    this.exportedAllocationIds.Concat(this.unexportedAllocationIds)
                    .ToDictionary(
                    id => new MeasureSet { ++measureId, ++measureId, ++measureId },
                    id => new PerNodeBudgetAllocationResult { AllocationId = id })
            };

            // Setup entities and repository
            this.userId = Guid.NewGuid().ToString("N");
            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId(), "Bar");
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(), "Foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");

            // Setup repository with default allocation blob, campaign and company stubs
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            RepositoryStubUtilities.SetupSaveEntityStub<IEntity>(
                this.repository,
                entity => RepositoryStubUtilities.SetupGetEntityStub(
                    this.repository, entity.ExternalEntityId, entity, false),
                false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.companyEntity.ExternalEntityId, this.companyEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.campaignEntity.ExternalEntityId, this.campaignEntity, false);

            // Create and associate the starting active allocation
            this.dynamicAllocationCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntity, this.campaignEntity);
            this.activeAllocationBlobEntity = this.dynamicAllocationCampaign.CreateAndAssociateActiveAllocationBlob(this.activeAllocation);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, this.activeAllocationBlobEntity.ExternalEntityId, this.activeAllocationBlobEntity, false);

            // Setup measure source mock
            var measureMap = this.activeAllocation.PerNodeResults
                .SelectMany(pnr => pnr.Key)
                .ToDictionary(
                    id => id,
                    id => (IDictionary<string, object>)new Dictionary<string, object>());
            MeasureSourceTestHelpers.InitializeMockMeasureSource(
                this.campaignEntity.GetExporterVersion(),
                this.campaignEntity.GetDeliveryNetwork(),
                measureMap);
        }

        /// <summary>Test simplest happy path scenario for the IncrementExportCounts activity.</summary>
        [TestMethod]
        public void IncrementExportCountsActivitySmokeTest()
        {
            var originalActiveAllocationBlobEntity = this.activeAllocationBlobEntity;

            // Create the activity
            var activity = Activity.CreateActivity(
                typeof(IncrementExportCountsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest)
                as IncrementExportCountsActivity;
            Assert.IsNotNull(activity);

            // Create and run the activity request
            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.IncrementExportCounts,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.userId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntity.ExternalEntityId.ToString() },
                    { DeliveryNetworkActivityValues.ExportedAllocationIds, string.Join(",", this.exportedAllocationIds) },
                }
            };
            var result = activity.Run(request);

            // Verify the result
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Succeeded, result.Error.Message);

            // Verify the campaign has a new active allocation blob
            Assert.AreNotEqual(
                originalActiveAllocationBlobEntity.ExternalEntityId,
                this.campaignEntity.GetActiveBudgetAllocationsEntityId());

            // Verify the counts have been incremented correctly
            var incrementedAllocation = this.dynamicAllocationCampaign.RetrieveActiveAllocation();
            Assert.IsNotNull(incrementedAllocation);

            // Verify allocations with ExportCount == 1 match the exported allocation ids
            var incrementedAllocationIds = incrementedAllocation.PerNodeResults.Values
                    .Where(pnr => pnr.ExportCount == 1)
                    .Select(pnr => pnr.AllocationId)
                    .ToArray();
            Assert.AreEqual(
                this.exportedAllocationIds.Length,
                incrementedAllocationIds.Intersect(this.exportedAllocationIds).Count());

            // Verify allocations with ExportCount == 0 match the unexported allocation ids
            var unincrementedAllocationIds = incrementedAllocation.PerNodeResults.Values
                .Where(pnr => pnr.ExportCount == 0)
                .Select(pnr => pnr.AllocationId)
                .ToArray();
            Assert.AreEqual(
                this.unexportedAllocationIds.Length,
                unincrementedAllocationIds.Intersect(this.unexportedAllocationIds).Count());
        }
    }
}
