// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateCampaignReportHandlerFactoryFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationActivities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportingActivities;
using ReportingTools;
using ReportingUtilities;
using Rhino.Mocks;

namespace ReportingActivitiesUnitTests
{
    /// <summary>
    /// Unit test fixture for CreateCampaignReportHandlerFactory
    /// </summary>
    [TestClass]
    public class CreateCampaignReportHandlerFactoryFixture
    {
        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>campaign id for testing</summary>
        private EntityId campaignEntityId;

        /// <summary>company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>Activity request for testing</summary>
        private ActivityRequest activityRequest;

        /// <summary>Activity context for testing</summary>
        private Dictionary<Type, object> activityContext;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.repository = MockRepository.GenerateStub<IEntityRepository>();

            // load all fields
            this.activityRequest = new ActivityRequest();
            this.activityRequest.Values.Add(EntityActivityValues.CompanyEntityId, this.companyEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.CampaignEntityId, this.campaignEntityId);
            this.activityRequest.Values.Add(ReportingActivityValues.VerboseReport, null);
            this.activityRequest.Values.Add(ReportingActivityValues.SaveLegacyConversion, null);
            this.activityRequest.Values.Add(ReportingActivityValues.ReportType, "SomeReport");

            this.activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository }
                };
        }

        /// <summary>Default constructor test.</summary>
        [TestMethod]
        public void DefaultConstructor()
        {
            var factory = new CreateCampaignReportHandlerFactory();
            Assert.IsInstanceOfType(factory.CampaignFactory, typeof(DynamicAllocationCampaignFactory));
        }

        /// <summary>Happy path create</summary>
        [TestMethod]
        public void CreateActivityHandlerSuccess()
        {
            // Setup the DynamicAllocationCampaign stub and factory
            var dynamicAllocationCampaign = MockRepository.GenerateStub<IDynamicAllocationCampaign>();
            dynamicAllocationCampaign.Stub(f => f.DeliveryNetwork).Return(DeliveryNetworkDesignation.AppNexus);
            
            // setup campaign factory stub so it only returns or dynamicAllocationCampaign stub if the entity id's
            // and SaveLegacyConversion flag match what is base in the activity request.
            var campaignFactory = MockRepository.GenerateStub<IDynamicAllocationCampaignFactory>();
            campaignFactory.Stub(f => f.MigrateDynamicAllocationCampaign(this.companyEntityId, this.campaignEntityId))
                .Return(dynamicAllocationCampaign);

            var factory = new CreateCampaignReportHandlerFactory(campaignFactory);
            var handler = factory.CreateActivityHandler(this.activityRequest, this.activityContext) as CampaignReportHandler;
            Assert.IsNotNull(handler);
            Assert.AreSame(this.repository, handler.Repository);
            Assert.AreEqual(this.campaignEntityId, handler.CampaignEntityId);
            Assert.AreEqual(this.companyEntityId, handler.CompanyEntityId);
            Assert.AreEqual(true, handler.BuildVerbose);
            Assert.IsInstanceOfType(handler.ReportGenerators[DeliveryNetworkDesignation.AppNexus], typeof(AppNexusBillingReport));
            Assert.AreEqual("SomeReport", handler.ReportType);
        }

        /// <summary>No matching generator test.</summary>
        [TestMethod]
        public void CreateActivityHandlerNoMatchingReportGenerator()
        {
            // Setup the DynamicAllocationCampaign stub and factory
            var dynamicAllocationCampaign = MockRepository.GenerateStub<IDynamicAllocationCampaign>();
            
            // Unsupported network
            dynamicAllocationCampaign.Stub(f => f.DeliveryNetwork).Return(DeliveryNetworkDesignation.GoogleDfp);

            // setup campaign factory stub so it only returns or dynamicAllocationCampaign stub if the entity id's
            // and SaveLegacyConversion flag match what is base in the activity request.
            var campaignFactory = MockRepository.GenerateStub<IDynamicAllocationCampaignFactory>();
            campaignFactory.Stub(f => f.MigrateDynamicAllocationCampaign(this.companyEntityId, this.campaignEntityId))
                .Return(dynamicAllocationCampaign);

            var factory = new CreateCampaignReportHandlerFactory(campaignFactory);
            var handler = factory.CreateActivityHandler(this.activityRequest, this.activityContext) as CampaignReportHandler;
            Assert.IsNotNull(handler);
            Assert.AreEqual(0, handler.ReportGenerators.Count);
        }
    }
}
