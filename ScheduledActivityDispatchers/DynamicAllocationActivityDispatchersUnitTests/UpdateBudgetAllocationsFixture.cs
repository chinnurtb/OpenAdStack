//-----------------------------------------------------------------------
// <copyright file="UpdateBudgetAllocationsFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Activities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocationActivityDispatchers;
using DynamicAllocationUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using ScheduledActivities;
using ScheduledActivityDispatchersTestHelpers;
using Utilities.Storage;
using Utilities.Storage.Testing;
using WorkItems;

namespace ScheduledDynamicAllocationActivitiesUnitTests
{
    /// <summary>Tests for the update budget allocations activity scheduler</summary>
    [TestClass]
    public class UpdateBudgetAllocationsFixture : ScheduledActivitySourceFixtureBase<UpdateBudgetAllocations>
    {
        /// <summary>Per-test method initialization</summary>
        /// <param name="context">Parameter unused</param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            System.Diagnostics.Trace.Write(context);
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateBudgetAllocationsSchedule"] = "00:00:10";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateAllocationsRequestExpiry"] = "00:02:00";
        }

        /// <summary>
        /// Test calling CreateScheduledRequests when conditions are such that
        /// no requests are expected.
        /// </summary>
        /// <remarks>Not really a valid test.</remarks>
        [TestMethod]
        public void CreateScheduledRequestsNoneToSchedule()
        {
            this.ScheduledNow = true;

            var source = this.CreateActivitySource();
            source.CreateScheduledRequests();

            this.MockQueuer.AssertWasNotCalled(f => f.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), null).Dummy));
            Assert.AreEqual(0, this.QueuedWorkItems.Count);
        }

        /// <summary>
        /// Test calling CreateScheduledRequests when conditions are such that
        /// a single request to GetBudgetAllocations is expected.
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CreateScheduledRequests()
        {
            var expectedRequest = new ActivityRequest
            {
                Task = "GetBudgetAllocations",
                Values =
                {
                    { "BudgetAllocationInputs", "TODO: serialized budget allocation inputs go here" }
                }
            };

            var source = this.CreateActivitySource();

            //// TODO: Set preconditions so that one scheduled request should be returned
            //// TODO: Probably need to setup a mock DAL and set it to return campaigns, etc
            this.ScheduledNow = true;

            source.CreateScheduledRequests();
            
            // TODO: Assert.AreEqual(1, source.SubmittedWorkItemIds.Count);
            
            // TODO: Use mock queuer to verify the correct work item was enqueued
            /*
            var scheduledRequest = this.WorkItems[source.SubmittedWorkItemIds.First()];
            Assert.IsNotNull(scheduledRequest);
            
            var request = scheduledRequest.ActivityRequest;
            Assert.IsNotNull(request);
            VerifyActivityRequest(expectedRequest, request);
             * */
        }

        /// <summary>
        /// Test calling OnActivityResult with the results of the
        /// GetBudgetAllocations activity running successfully
        /// </summary>
        public void OnBudgetAllocationsActivityResult()
        {
            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                Values =
                {
                    { EntityActivityValues.CompanyEntityId, new EntityId() },
                    { EntityActivityValues.CampaignEntityId, new EntityId() },
                    { DynamicAllocationActivityValues.AllocationStartDate, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
                    { DynamicAllocationActivityValues.IsInitialAllocation, true.ToString() },
                }
            };
            var result = new ActivityResult
            {
                Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                RequestId = request.Id,
                Succeeded = true,
                Values =
                {
                    { "BudgetAllocation", "TODO: serialized budget allocation outputs go here" }
                }
            };

            //// TODO: Probably need to setup repository stubs to return corresponding entities, etc.

            var source = this.CreateActivitySource();
            
            source.OnActivityResult(request, result);

            //// TODO: Assert that the expected repository methods were called

            //// TODO: Assert the generated requests are as expected
        }
    }
}
