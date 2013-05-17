//-----------------------------------------------------------------------
// <copyright file="CleanupCampaignsFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Linq;
using Activities;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkActivityDispatchers;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
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
    /// <summary>Tests for the campaign cleanup activity scheduler</summary>
    [TestClass]
    public class CleanupCampaignsFixture : ScheduledActivitySourceFixtureBase<CleanupCampaigns>
    {
        /// <summary>Per-test method initialization</summary>
        [TestInitialize]
        public override void TestInitialize()
        {
            base.TestInitialize();
            
            ConfigurationManager.AppSettings["Delivery.CleanupCampaignsRequestExpiry"] = "00:10:00";
            ConfigurationManager.AppSettings["Delivery.CleanupCampaignsSchedule"] = "01:00:00";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateAllocationsRequestExpiry"] = "00:10:00";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateBudgetAllocationsSchedule"] = "01:00:00";
            ConfigurationManager.AppSettings["System.AuthUserId"] = Guid.NewGuid().ToString("n");
            Scheduler.Registries = null;
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
        public void CreateScheduledRequests()
        {
            var companyEntityId = new EntityId().ToString();
            var campaignEntityId = new EntityId().ToString();
            var expectedRequest = new ActivityRequest
            {
                Task = AppNexusActivityTasks.DeleteLineItem,
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId },
                    { EntityActivityValues.CompanyEntityId, companyEntityId },
                    { EntityActivityValues.CampaignEntityId, campaignEntityId }
                }
            };

            Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CampaignsToCleanup,
                DateTime.UtcNow,
                campaignEntityId,
                companyEntityId,
                DeliveryNetworkDesignation.AppNexus);
            this.ScheduledNow = true;

            var source = this.CreateActivitySource();
            source.CreateScheduledRequests();

            this.MockQueuer.AssertWasCalled(f => f.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), null).Dummy));
            
            var workItem = this.QueuedWorkItems.SingleOrDefault();
            Assert.IsNotNull(workItem);
            var request = ActivityRequest.DeserializeFromXml(workItem.Content);
            VerifyActivityRequest(expectedRequest, request);
        }
    }
}
