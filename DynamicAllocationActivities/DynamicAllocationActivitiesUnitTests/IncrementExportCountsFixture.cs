// -----------------------------------------------------------------------
// <copyright file="IncrementExportCountsFixture.cs" company="Rare Crowds Inc">
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
        private string[] allocationIds;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            var measureId = 0L;
            this.allocationIds = Enumerable.Range(0, 10).Select(i => Guid.NewGuid().ToString("N")).ToArray();
            this.activeAllocation = new BudgetAllocation
            {
                PerNodeResults = this.allocationIds.ToDictionary(
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
            var exportedAllocationIds = this.allocationIds.Take(3).ToArray();
            var unexportedAllocationIds = this.allocationIds.Except(exportedAllocationIds).ToArray();
            var request = this.CreateActivityRequest(exportedAllocationIds);
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
                exportedAllocationIds.Length,
                incrementedAllocationIds.Intersect(exportedAllocationIds).Count());

            // Verify allocations with ExportCount == 0 match the unexported allocation ids
            var unincrementedAllocationIds = incrementedAllocation.PerNodeResults.Values
                .Where(pnr => pnr.ExportCount == 0)
                .Select(pnr => pnr.AllocationId)
                .ToArray();
            Assert.AreEqual(
                unexportedAllocationIds.Length,
                unincrementedAllocationIds.Intersect(unexportedAllocationIds).Count());
        }

        /// <summary>Test incrementing counts multiple times.</summary>
        [TestMethod]
        public void MultipleIncrementExportCounts()
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
            var exportedAllocationIdsA = this.allocationIds.Take(3).ToArray();
            var exportedAllocationIdsB = this.allocationIds.Except(exportedAllocationIdsA).Take(3).ToArray();

            // Run the activity and verify the counts have been incremented correctly
            var request = this.CreateActivityRequest(exportedAllocationIdsA);
            var result = activity.Run(request);
            Assert.IsTrue(result.Succeeded);
            var incrementedAllocation = this.dynamicAllocationCampaign.RetrieveActiveAllocation();
            Assert.IsNotNull(incrementedAllocation);
            Assert.IsTrue(
                exportedAllocationIdsA.All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 1));
            Assert.IsTrue(
                this.allocationIds
                .Except(exportedAllocationIdsA)
                .All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 0));

            // Increment some other allocations and verify the export counts
            request = this.CreateActivityRequest(exportedAllocationIdsB);
            result = activity.Run(request);
            Assert.IsTrue(result.Succeeded);
            incrementedAllocation = this.dynamicAllocationCampaign.RetrieveActiveAllocation();
            Assert.IsNotNull(incrementedAllocation);
            Assert.IsTrue(
                exportedAllocationIdsA.All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 1));
            Assert.IsTrue(
                exportedAllocationIdsB.All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 1));
            Assert.IsTrue(
                this.allocationIds
                .Except(exportedAllocationIdsA.Concat(exportedAllocationIdsB))
                .All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 0));

            // Increment the first allocations again and verify the export counts
            request = this.CreateActivityRequest(exportedAllocationIdsA);
            result = activity.Run(request);
            Assert.IsTrue(result.Succeeded);
            incrementedAllocation = this.dynamicAllocationCampaign.RetrieveActiveAllocation();
            Assert.IsNotNull(incrementedAllocation);
            Assert.IsTrue(
                exportedAllocationIdsA.All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 2));
            Assert.IsTrue(
                exportedAllocationIdsB.All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 1));
            Assert.IsTrue(
                this.allocationIds
                .Except(exportedAllocationIdsA.Concat(exportedAllocationIdsB))
                .All(id =>
                    incrementedAllocation.PerNodeResults.Values
                    .Single(pnr => pnr.AllocationId == id)
                    .ExportCount == 0));
        }

        /// <summary>
        /// Creates an activity request with the specified allocation ids
        /// </summary>
        /// <param name="exportedAllocationIds">Exported allocation ids to increment</param>
        /// <returns>The IncrementExportCounts activity request</returns>
        private ActivityRequest CreateActivityRequest(string[] exportedAllocationIds)
        {
            return new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.IncrementExportCounts,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.userId },
                    { EntityActivityValues.CompanyEntityId, this.companyEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.CampaignEntityId, this.campaignEntity.ExternalEntityId.ToString() },
                    { DeliveryNetworkActivityValues.ExportedAllocationIds, string.Join(",", exportedAllocationIds) },
                }
            };
        }
    }
}
