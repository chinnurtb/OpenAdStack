// -----------------------------------------------------------------------
// <copyright file="DynamicAllocationCampaignFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using Activities;
using ConfigManager;
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

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Unit test fixture for DynamicAllocationCampaign</summary>
    [TestClass]
    public class DynamicAllocationCampaignFixture
    {
        /// <summary>Campaign version for testing.</summary>
        private readonly int? campaignVersion = 2;

        /// <summary>IEntity repository stub for testing.</summary>
        private IEntityRepository repository;

        /// <summary>Campaign EntityId for testing.</summary>
        private EntityId campaignEntityId;

        /// <summary>Company EntityId for testing.</summary>
        private EntityId companyEntityId;

        /// <summary>CampaignEntity for testing.</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Versioned CampaignEntity for testing.</summary>
        private CampaignEntity campaignEntityVersioned;
        
        /// <summary>CompanyEntity for testing.</summary>
        private CompanyEntity companyEntity;

        /// <summary>Node map for testing.</summary>
        private Dictionary<string, MeasureSet> expecteNodeMap;

        /// <summary>Node map blob for testing.</summary>
        private BlobEntity nodeMapBlob;

        /// <summary>A DynamicAllocationCampaign for testing.</summary>
        private DynamicAllocationCampaign dynAllCampaign;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateStub<ILogger>() });

            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, "company");
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId, "Foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");

            // Set the approved version property on the default campaign (which does not itself have a meaningful version)
            // in the tests
            this.campaignEntity.SetPropertyByName(daName.InputsApprovedVersion, this.campaignVersion.Value);

            // Set up a versioned campaign that corresponds
            this.campaignEntityVersioned = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId, "Foo", 1000, DateTime.UtcNow, DateTime.UtcNow, "persona");
            this.campaignEntityVersioned.LocalVersion = this.campaignVersion.Value;

            // Setup allocation params on campaign
            AllocationParametersTestHelpers.Initialize(this.campaignEntity);

            // Setup default campaign and company stubs
            var entityFilter = new RepositoryEntityFilter(true, true, true, true);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, entityFilter, this.companyEntityId, this.companyEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, entityFilter, this.campaignEntityId, this.campaignEntity, false);
            entityFilter = new RepositoryEntityFilter(true, true, true, true);
            entityFilter.AddVersionToEntityFilter(2);
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, entityFilter, this.campaignEntityId, this.campaignEntityVersioned, false);
            
            // Setup a default DynamicAllocationCampaign
            this.dynAllCampaign = new DynamicAllocationCampaign(
                this.repository, this.companyEntityId, this.campaignEntityId);

            this.expecteNodeMap = new Dictionary<string, MeasureSet>
                {
                    { "allocationid", new MeasureSet(new long[] { 1, 2, 3 }) } 
                };

            this.nodeMapBlob = BlobEntity.BuildBlobEntity(new EntityId(), this.expecteNodeMap);
        }

        /// <summary>Happy path constructor success.</summary>
        [TestMethod]
        public void DynamicAllocationCampaignConstructorSuccess()
        {
            var dac = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);
            Assert.AreSame(this.repository, dac.Repository);
            Assert.AreSame(this.campaignEntity.WrappedEntity, dac.CampaignEntity.WrappedEntity);
            Assert.AreSame(this.companyEntity.WrappedEntity, dac.CompanyEntity.WrappedEntity);
            Assert.IsNotNull(dac.CampaignConfig);
            Assert.IsNotNull(dac.AllocationParameters);
            Assert.IsNotNull(dac.BudgetAllocationHistory);
            Assert.IsNotNull(dac.RawDeliveryData);
            Assert.IsNotNull(dac.DeliveryNetwork);
        }

        /// <summary>Test constructor with version.</summary>
        [TestMethod]
        public void DynamicAllocationCampaignConstructorAtVersionSuccess()
        {
            var dac = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId, this.campaignVersion);
            Assert.AreSame(this.repository, dac.Repository);
            Assert.AreSame(this.campaignEntityVersioned.WrappedEntity, dac.CampaignEntity.WrappedEntity);
        }

        /// <summary>Happy path injection constructor success.</summary>
        [TestMethod]
        public void DynamicAllocationCampaignInjectionConstructorSuccess()
        {
            var dac = new DynamicAllocationCampaign(this.repository, this.companyEntity, this.campaignEntity);
            Assert.AreSame(this.repository, dac.Repository);
            Assert.AreSame(this.campaignEntity, dac.CampaignEntity);
            Assert.AreSame(this.companyEntity, dac.CompanyEntity);
            Assert.IsNotNull(dac.CampaignConfig);
            Assert.IsNotNull(dac.AllocationParameters);
            Assert.IsNotNull(dac.BudgetAllocationHistory);
            Assert.IsNotNull(dac.RawDeliveryData);
            Assert.IsNotNull(dac.DeliveryNetwork);
        }

        /// <summary>Test the factory can bind repository at runtime.</summary>
        [TestMethod]
        public void CampaignFactoryRuntimeBinding()
        {
            var converter = MockRepository.GenerateStub<IEntityConverter>();

            Action<IEntity> convert = entity =>
                {
                    var config = entity.GetConfigSettings();
                    config["DynamicAllocation.Margin"] = "{0}".FormatInvariant(1.17m);
                    entity.SetConfigSettings(config);
                };

            converter.Stub(f => f.ConvertEntity(this.campaignEntityId, this.companyEntityId))
                .WhenCalled(call => convert(this.campaignEntity));

            var factory = new DynamicAllocationCampaignFactory();
            factory.BindRuntime(this.repository, converter);

            Assert.AreSame(this.repository, factory.Repository);
            Assert.AreSame(converter, factory.Converter);

            var dynamicAllocationCampaign = factory.BuildDynamicAllocationCampaign(
                this.companyEntityId, this.campaignEntityId);
            Assert.AreNotEqual(1.17m, dynamicAllocationCampaign.AllocationParameters.Margin);

            dynamicAllocationCampaign = factory.MigrateDynamicAllocationCampaign(
                this.companyEntityId, this.campaignEntityId);
            Assert.AreEqual(1.17m, dynamicAllocationCampaign.AllocationParameters.Margin);
        }

        /// <summary>Test the factory can build DAC with approved inputs.</summary>
        [TestMethod]
        public void CampaignFactoryApprovedInputs()
        {
            var factory = new DynamicAllocationCampaignFactory();
            factory.BindRuntime(this.repository);

            var dac = factory.BuildDynamicAllocationCampaign(
                this.companyEntityId, this.campaignEntityId, true);
            Assert.AreEqual(this.campaignVersion, (int)dac.CampaignEntity.LocalVersion);
        }

        /// <summary>Test the factory throw if approved version requested and campaign is not approved.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void CampaignFactoryApprovedInputsNotApproved()
        {
            var factory = new DynamicAllocationCampaignFactory();
            factory.BindRuntime(this.repository);

            // Remove the approved version property
            var prop = this.campaignEntity.GetEntityPropertyByName(daName.InputsApprovedVersion);
            this.campaignEntity.Properties.Remove(prop);

            factory.BuildDynamicAllocationCampaign(this.companyEntityId, this.campaignEntityId, true);
        }

        /// <summary>Test the factory default converter binding.</summary>
        [TestMethod]
        public void CampaignFactoryDefaultRuntimeBinding()
        {
            var factory = new DynamicAllocationCampaignFactory();
            factory.BindRuntime(this.repository);

            Assert.AreSame(this.repository, factory.Repository);
            Assert.IsInstanceOfType(factory.Converter, typeof(DefaultCampaignConverter));
        }

        /// <summary>GetAllocationNodeMap happy path.</summary>
        [TestMethod]
        public void GetAllocationNodeMapSuccess()
        {
            SetupStubForNodeMapBlob(this.campaignEntity, this.nodeMapBlob, this.repository);

            var actualNodeMap = this.dynAllCampaign.RetrieveAllocationNodeMap();
            Assert.AreEqual(this.expecteNodeMap.Count, actualNodeMap.Count);
            Assert.IsTrue(actualNodeMap.ContainsKey("allocationid"));
        }

        /// <summary>GetAllocationNodeMap with no blob association present (success).</summary>
        [TestMethod]
        public void GetAllocationNodeMapNoBlobAssociation()
        {
            var actualNodeMap = this.dynAllCampaign.RetrieveAllocationNodeMap();
            Assert.AreEqual(0, actualNodeMap.Count);
        }

        /// <summary>GetDeliveryNetwork specified on campaign.</summary>
        [TestMethod]
        public void DeliveryNetworkFromCampaign()
        {
            this.companyEntity.SetDeliveryNetwork(DeliveryNetworkDesignation.Unknown);
            this.campaignEntity.SetDeliveryNetwork(DeliveryNetworkDesignation.GoogleDfp);
            this.dynAllCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);

            Assert.AreEqual(DeliveryNetworkDesignation.GoogleDfp, this.dynAllCampaign.DeliveryNetwork);
        }

        /// <summary>GetDeliveryNetwork specified on company.</summary>
        [TestMethod]
        public void DeliveryNetworkFromCompany()
        {
            this.companyEntity.SetDeliveryNetwork(DeliveryNetworkDesignation.GoogleDfp);
            this.dynAllCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);

            Assert.AreEqual(DeliveryNetworkDesignation.GoogleDfp, this.dynAllCampaign.DeliveryNetwork);
        }

        /// <summary>DefaultDeliveryNetwork should not be unknown.</summary>
        [TestMethod]
        public void DeliveryNetworkFromDefault()
        {
            this.dynAllCampaign = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);

            Assert.AreNotEqual(DeliveryNetworkDesignation.Unknown, this.dynAllCampaign.DeliveryNetwork);
        }

        /// <summary>If DefaultDeliveryNetwork not in config should be unknown.</summary>
        [TestMethod]
        public void DeliveryNetworkDefaultNotSpecified()
        {
            var configStub = MockRepository.GenerateStub<IConfig>();
            configStub.Stub(f => f.GetEnumValue<DeliveryNetworkDesignation>("Delivery.DefaultNetwork")).Throw(new ArgumentException("bad"));
            this.dynAllCampaign = new DynamicAllocationCampaign(
                this.repository, this.companyEntity, this.campaignEntity, configStub);

            Assert.AreEqual(DeliveryNetworkDesignation.Unknown, this.dynAllCampaign.DeliveryNetwork);
        }

        /// <summary>Test we can get the measure sources from the measure source factory.</summary>
        [TestMethod]
        public void CreateMeasureMapSuccess()
        {
            var measureSourceProvider = MockRepository.GenerateStub<IMeasureSourceProvider>();
            MeasureSourceFactory.Initialize(new[] { measureSourceProvider });
            var map = this.dynAllCampaign.RetrieveMeasureMap();
            Assert.IsNotNull(map);
        }

        /// <summary>Test we can get the DynamicAllocationEngine.</summary>
        [TestMethod]
        public void GetDynamicAllocationEngineSuccess()
        {
            var measureSourceProvider = MockRepository.GenerateStub<IMeasureSourceProvider>();
            MeasureSourceFactory.Initialize(new[] { measureSourceProvider });
            var dae = this.dynAllCampaign.CreateDynamicAllocationEngine();
            Assert.IsNotNull(dae);
        }

        /// <summary>Test AllocationParameters honor the precedence chain.</summary>
        [TestMethod]
        public void BuildAllocationParameters()
        {
            // Set up all configs on company
            AllocationParametersTestHelpers.Initialize(this.companyEntity);

            // Setup campaign with custom config
            var campaignConfigSettings = new Dictionary<string, string>
            {
                { "DynamicAllocation.Margin", "1.23" },
                { "DynamicAllocation.PerMilleFees", "0.081" },
                { "DynamicAllocation.BudgetBuffer", "1.42" },
            };
            this.campaignEntity.SetConfigSettings(campaignConfigSettings);

            // Rebuild the dac so it get the new settings.
            var dac = new DynamicAllocationCampaign(this.repository, this.companyEntityId, this.campaignEntityId);
            var allocationParameters = dac.AllocationParameters;

            // These should come from campaign
            Assert.AreEqual(allocationParameters.Margin, 1.23m);
            Assert.AreEqual(allocationParameters.PerMilleFees, .081m);
            Assert.AreEqual(allocationParameters.BudgetBuffer, 1.42m);

            // The rest come from company
            Assert.AreEqual(allocationParameters.DefaultEstimatedCostPerMille, 1.5m);
            Assert.AreEqual(allocationParameters.MaxNodesToExport, 500);
            Assert.AreEqual(allocationParameters.InitialAllocationTotalPeriodDuration, TimeSpan.Parse("1.00:00:00"));
            Assert.AreEqual(allocationParameters.InitialAllocationSinglePeriodDuration, TimeSpan.Parse("6:00:00"));
            Assert.AreEqual(allocationParameters.AllocationTopTier, 7);
            Assert.AreEqual(allocationParameters.AllocationNumberOfTiersToAllocateTo, 4);
            Assert.AreEqual(allocationParameters.AllocationNumberOfNodes, 1000);
            Assert.AreEqual(allocationParameters.ExportBudgetBoost, 1m);
            Assert.AreEqual(allocationParameters.LargestBudgetPercentAllowed, .03m);
            Assert.AreEqual(allocationParameters.NeutralBudgetCappingTier, 4);
            Assert.AreEqual(allocationParameters.LineagePenalty, .1);
            Assert.AreEqual(allocationParameters.LineagePenaltyNeutral, 1);
        }
        
        /// <summary>Helper method to initialize campaign entity and setup repository stub.</summary>
        /// <param name="campaign">The campaign entity to intialize.</param>
        /// <param name="blob">The node map blob to associate with the campaign.</param>
        /// <param name="stub">The repository stub.</param>
        private static void SetupStubForNodeMapBlob(CampaignEntity campaign, BlobEntity blob, IEntityRepository stub)
        {
            campaign.TryAssociateEntities(
                DynamicAllocationEntityProperties.AllocationNodeMap,
                string.Empty,
                new HashSet<IEntity> { blob },
                AssociationType.Relationship,
                false);

            RepositoryStubUtilities.SetupGetEntityStub(stub, null, blob.ExternalEntityId, blob, false);
        }
    }
}
