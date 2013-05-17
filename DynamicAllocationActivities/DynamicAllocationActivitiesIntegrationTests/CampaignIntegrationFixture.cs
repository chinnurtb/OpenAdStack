// -----------------------------------------------------------------------
// <copyright file="CampaignIntegrationFixture.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Configuration;
using System.Linq;
using System.Text;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationTestUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using SimulatedDataStore;

namespace DynamicAllocationActivitiesIntegrationTests
{
    /// <summary>
    /// Integration test fixture for DynamicAllocationCampaign
    /// </summary>
    [TestClass]
    public class CampaignIntegrationFixture
    {
        /// <summary>DA campaign stub for testing.</summary>
        private static readonly DynamicAllocationCampaignTestStub campaignStub = new DynamicAllocationCampaignTestStub();

        /// <summary>Global config value for margin.</summary>
        private const decimal DefaultMarginConfigValue = 1m;

        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>campaign id for testing.</summary>
        private EntityId campaignEntityId;

        /// <summary>campaign owner user id for testing</summary>
        private string campaignOwnerId;

        /// <summary>One time class initialization</summary>
        /// <param name="context">The context.</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });

            MeasureSourceFactory.Initialize(new IMeasureSourceProvider[]
            {
                new AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider(),
                new AppNexusActivities.Measures.AppNexusMeasureSourceProvider()
            });
        }

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // Make sure global config value for Margin is set to the correct default.
            ConfigurationManager.AppSettings["DynamicAllocation.Margin"] = "{0}".FormatInvariant(DefaultMarginConfigValue);

            // Set up an in-memory simulated repository. There are too many interior saves
            // for Rhino to be effective.
            this.repository = new SimulatedEntityRepository();

            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.campaignOwnerId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N")));
        }

        /// <summary>Test AllocationParameters can come from global config.</summary>
        [TestMethod]
        public void BuildAllocationParametersWithGlobalConfig()
        {
            var companyEntity = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, "company");
            var campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId, "Foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");
            EntityTestHelpers.CreateTestUserEntity(new EntityId(), this.campaignOwnerId, "nobody@rc.dev");

            // Set global config value for Margin to make sure we pull from it.
            var marginConfigValue = .9999m;
            ConfigurationManager.AppSettings["DynamicAllocation.Margin"] = "{0}".FormatInvariant(marginConfigValue);

            // Set up company with all configs except the one we initialized on global config
            AllocationParametersTestHelpers.Initialize(companyEntity);
            var companyConfigSettings = companyEntity.GetConfigSettings();
            companyConfigSettings.Remove("DynamicAllocation.Margin");
            companyEntity.SetConfigSettings(companyConfigSettings);

            // Get the allocation parameters
            var dac = new DynamicAllocationCampaign(
                MockRepository.GenerateStub<IEntityRepository>(), companyEntity, campaignEntity);
            var allocationParameters = dac.AllocationParameters;

            // This should still be in present in the allocation parameters
            // because of global config even though we've removed it from company configs
            Assert.AreEqual(allocationParameters.Margin, marginConfigValue);
        }

        /// <summary>Test to build a campaign without conversion.</summary>
        [TestMethod]
        public void BuildCampaignWithoutConversion()
        {
            campaignStub.SetupCampaign(this.repository, this.companyEntityId, this.campaignEntityId, this.campaignOwnerId);
            var factory = new DynamicAllocationCampaignFactory();
            factory.BindRuntime(this.repository);
            var dac = factory.BuildDynamicAllocationCampaign(this.companyEntityId, this.campaignEntityId, false);
            Assert.AreEqual(1m, dac.AllocationParameters.Margin);
            Assert.AreEqual(.06m, dac.AllocationParameters.PerMilleFees);
            Assert.AreEqual(DeliveryNetworkDesignation.AppNexus, dac.DeliveryNetwork);
            Assert.IsNotNull(dac.RetrieveMeasureMap());
            Assert.IsNotNull(dac.RetrieveAllocationNodeMap());
            var rawIndex = dac.RawDeliveryData.RetrieveRawDeliveryDataIndexItems();
            var rawDataId = rawIndex.Single().RawDeliveryDataEntityIds.First();
            Assert.IsNotNull(dac.RawDeliveryData.RetrieveRawDeliveryDataItem(rawDataId));
            var valuationInputs = ValuationsCache.BuildValuationInputs(dac.CampaignEntity);
            Assert.IsNotNull(valuationInputs);
        }
    }
}
