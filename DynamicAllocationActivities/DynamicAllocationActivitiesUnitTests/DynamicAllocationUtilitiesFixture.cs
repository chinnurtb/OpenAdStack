//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationUtilitiesFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Activities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>Tests for DynamicAllocationActivityUtilities</summary>
    [TestClass]
    public class DynamicAllocationUtilitiesFixture
    {
        /// <summary>Test campaign entity</summary>
        private CompanyEntity companyEntity;

        /// <summary>Test campaign entity</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Test campaign entity</summary>
        private UserEntity campaignOwner;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            DynamicAllocationActivitiesTestHelpers.SetupMeasureSourceFactoryStub();

            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(
                new EntityId(), Guid.NewGuid().ToString("N"));
            this.campaignOwner = EntityTestHelpers.CreateTestUserEntity(
                new EntityId(), Guid.NewGuid().ToString("N"), "nobody@example.com");
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(), Guid.NewGuid().ToString("N"), 1000, DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "Mr. Perfect");
            this.campaignEntity.SetPropertyByName<string>(
                DeliveryNetworkEntityProperties.DeliveryNetwork, DeliveryNetworkDesignation.AppNexus.ToString());
            this.campaignEntity.SetExporterVersion(0);
        }

        /// <summary>Test verification of campaign valuation inputs</summary>
        [TestMethod]
        public void VerifyCampaignValuationInputs()
        {
            var measureList = @"{""IdealValuation"":17.6, ""MaxValuation"":""3.29"", ""Measures"":[{""measureId"":""1155940"", ""group"":"""", ""valuation"":""56"", ""pinned"":false}, {""measureId"":""1155964"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1106030"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1345698"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200106"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200123"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1201053"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200852"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}]}";
            this.campaignEntity.SetPropertyByName<string>(DynamicAllocationEntityProperties.MeasureList, measureList);
            DynamicAllocationActivityUtilities.VerifyHasRequiredValuationInputs(
                this.companyEntity, this.campaignEntity, this.campaignOwner);
        }

        /// <summary>Test verification of campaign valuation inputs</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void VerifyCampaignValuationInputsUnknownDataCost()
        {
            // 9999999 in embedded MeasureMap.js has "dataProvider": "Unknown"
            var measureList = @"{""IdealValuation"":17.6, ""MaxValuation"":""3.29"", ""Measures"":[{""measureId"":""9999999"", ""group"":"""", ""valuation"":""56"", ""pinned"":false}, {""measureId"":""1155964"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1106030"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1345698"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200106"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200123"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1201053"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}, {""measureId"":""1200852"", ""group"":"""", ""valuation"":""50"", ""pinned"":false}]}";
            this.campaignEntity.SetPropertyByName<string>(DynamicAllocationEntityProperties.MeasureList, measureList);

            try
            {
                DynamicAllocationActivityUtilities.VerifyHasRequiredValuationInputs(
                    this.companyEntity, this.campaignEntity, this.campaignOwner);
            }
            catch (ActivityException ae)
            {
                Assert.IsTrue(ae.Message.Contains("9999999"));
                throw;
            }
        }

        /// <summary>Test verification of campaign valuation inputs</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void VerifyCampaignValuationInputsMissingDataCosts()
        {
            DynamicAllocationActivityUtilities.VerifyHasRequiredValuationInputs(
                this.companyEntity, this.campaignEntity, this.campaignOwner);
        }
    }
}
