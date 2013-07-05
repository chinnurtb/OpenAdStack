// -----------------------------------------------------------------------
// <copyright file="RawDeliveryDataFixture.cs" company="Rare Crowds Inc">
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
using AppNexusUtilities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using GoogleDfpUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using TestUtilities;
using Utilities.Serialization;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// Unit-test fixture for RawDeliveryData class
    /// </summary>
    [TestClass]
    public class RawDeliveryDataFixture
    {
        /// <summary>Utc time value for testing.</summary>
        private readonly DateTime time10pmESTasUTC = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.Parse("2012-03-16 22:00:00"), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));

        /// <summary>IEntity repository stub for testing.</summary>
        private IEntityRepository repository;

        /// <summary>Campaign EntityId for testing.</summary>
        private EntityId campaignEntityId;

        /// <summary>Company EntityId for testing.</summary>
        private EntityId companyEntityId;

        /// <summary>CampaignEntity for testing.</summary>
        private CampaignEntity campaignEntity;

        /// <summary>CompanyEntity for testing.</summary>
        private CompanyEntity companyEntity;

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

            // Setup default campaign and company stubs
            this.repository.Stub(f => f.GetEntity(
                Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(this.campaignEntityId))).Return(this.campaignEntity);
            this.repository.Stub(f => f.GetEntity(
                Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(this.companyEntityId))).Return(this.companyEntity);
        }

        /// <summary>Test we can get the raw delivery data indexes from the campaign entity.</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataIndexesSuccess()
        {
            // Setup raw delivery data index property
            var index = new List<string> { new EntityId(), new EntityId() };
            var indexJson = AppsJsonSerializer.SerializeObject(index);
            this.campaignEntity.SetPropertyValueByName(AppNexusEntityProperties.AppNexusRawDeliveryDataIndex, indexJson);

            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var indexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();

            Assert.AreEqual(1, indexes.Count());
            Assert.AreEqual(DeliveryNetworkDesignation.AppNexus, indexes[0].DeliveryNetwork);
            Assert.AreEqual(2, indexes[0].RawDeliveryDataEntityIds.Count());
        }

        /// <summary>Test when raw delivery data indexes have not been created.</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataIndexesNotPresent()
        {
            // Don't initialize the indexes
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var indexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();

            Assert.AreEqual(0, indexes.Count());
        }

        /// <summary>Test we can get raw delivery data from from an entity.</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataSuccess()
        {
            var rawDeliveryDataEntityId = new EntityId();
            var rawDeliveryDataEntity = EntityTestHelpers.CreateTestPartnerEntity(
                rawDeliveryDataEntityId, "ExternalNameFoo");
            rawDeliveryDataEntity.LastModifiedDate = DateTime.UtcNow;
            var rawDeliveryData = "raw data, really";
            rawDeliveryDataEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.RawDeliveryDataEntityPayloadName, rawDeliveryData);

            // Setup raw delivery data repository stub
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, rawDeliveryDataEntityId, rawDeliveryDataEntity, false);

            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataItem = rawData.RetrieveRawDeliveryDataItem(rawDeliveryDataEntityId);

            Assert.AreEqual(rawDeliveryDataEntity.LastModifiedDate, rawDeliveryDataItem.DeliveryDataReportDate);
            Assert.AreEqual(rawDeliveryData, rawDeliveryDataItem.RawDeliveryData);
        }

        /// <summary>Test retrieve raw delivery data indexes is correct when none is present.</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataIndexesNone()
        {
            // Assert no index is retrieved for bare campaign
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems();
            Assert.AreEqual(0, rawDeliveryDataIndexes.Count());
        }

        /// <summary>Test we retrieve raw delivery data for DFP</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataGoogleDoubleClickForPub()
        {
            var expectedReportDate = DateTime.UtcNow;

            // Setup the campaign with a DFP raw delivery data index
            SetupRawDeliveryDataOnCampaign(
                this.repository,
                this.campaignEntity,
                new[] { "Resources.DfpDeliveryData.csv" },
                expectedReportDate,
                GoogleDfpEntityProperties.DfpRawDeliveryDataIndex,
                false,
                false);

            // Assert we can retrieve the raw delivery data index
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();
            Assert.AreEqual(rawDeliveryDataIndexes.Single().DeliveryNetwork, DeliveryNetworkDesignation.GoogleDfp);
            Assert.AreEqual(1, rawDeliveryDataIndexes.Single().RawDeliveryDataEntityIds.Count());

            // Assert we can retrieve raw data
            var rawDeliveryDataEntityId = rawDeliveryDataIndexes[0].RawDeliveryDataEntityIds.First();
            var rawDeliveryDataItem = rawData.RetrieveRawDeliveryDataItem(rawDeliveryDataEntityId);

            Assert.IsNotNull(rawDeliveryDataItem.RawDeliveryData);
            Assert.AreEqual(expectedReportDate, rawDeliveryDataItem.DeliveryDataReportDate);
        }

        /// <summary>Test we retrieve raw delivery data from APNX with an index property on campaign</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataAppNexusAsProperty()
        {
            var expectedReportDate = DateTime.UtcNow;

            // Setup the campaign with a APNX raw delivery data index
            SetupRawDeliveryDataOnCampaign(
                this.repository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryDataA.csv", "Resources.ApnxDeliveryDataB.csv" },
                expectedReportDate,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            // Assert we can retrieve this raw delivery data index
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();
            Assert.AreEqual(rawDeliveryDataIndexes.Single().DeliveryNetwork, DeliveryNetworkDesignation.AppNexus);
            Assert.AreEqual(2, rawDeliveryDataIndexes.Single().RawDeliveryDataEntityIds.Count());

            // Assert we can retrieve raw data
            var rawDeliveryDataEntityId = rawDeliveryDataIndexes[0].RawDeliveryDataEntityIds.First();
            
            var rawDeliveryDataItem = rawData.RetrieveRawDeliveryDataItem(rawDeliveryDataEntityId);

            Assert.IsNotNull(rawDeliveryDataItem.RawDeliveryData);
            Assert.AreEqual(expectedReportDate, rawDeliveryDataItem.DeliveryDataReportDate);
        }

        /// <summary>Test we retrieve raw delivery data indexes from with multiple index properties present.</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataIndexesMultipleNetwork()
        {
            // Setup the campaign with a APNX raw delivery data index
            SetupRawDeliveryDataOnCampaign(
                this.repository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryData.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                false);

            // Setup the campaign with a DFP raw delivery data index
            SetupRawDeliveryDataOnCampaign(
                this.repository,
                this.campaignEntity,
                new[] { "Resources.DfpDeliveryData.csv" },
                DateTime.UtcNow,
                GoogleDfpEntityProperties.DfpRawDeliveryDataIndex,
                false,
                false);

            // Assert we can retieve this raw delivery data
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();
            Assert.AreEqual(2, rawDeliveryDataIndexes.Count);
            Assert.IsTrue(rawDeliveryDataIndexes.Any(c => c.DeliveryNetwork == DeliveryNetworkDesignation.AppNexus));
            Assert.IsTrue(rawDeliveryDataIndexes.Any(c => c.DeliveryNetwork == DeliveryNetworkDesignation.GoogleDfp));
        }

        /// <summary>Test return null when raw data entity payload not present.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void RetrieveRawDeliveryDataFailEntityPayload()
        {
            SetupRawDeliveryDataOnCampaign(
                this.repository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryData.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                true,
                false);

            // Assert we can retieve this raw delivery data
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();
            var rawDeliveryDataEntityId = rawDeliveryDataIndexes[0].RawDeliveryDataEntityIds.First();
            rawData.RetrieveRawDeliveryDataItem(rawDeliveryDataEntityId);
        }

        /// <summary>Test return null when raw data entity retrieve fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void RetrieveRawDeliveryDataFailEntity()
        {
            SetupRawDeliveryDataOnCampaign(
                this.repository,
                this.campaignEntity,
                new[] { "Resources.ApnxDeliveryData.csv" },
                DateTime.UtcNow,
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                false,
                true);

            // Assert we fail correctly
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();
            var rawDeliveryDataEntityId = rawDeliveryDataIndexes[0].RawDeliveryDataEntityIds.First();
            try
            {
                rawData.RetrieveRawDeliveryDataItem(rawDeliveryDataEntityId);
            }
            catch (ActivityException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(DataAccessEntityNotFoundException));
                throw;
            }
        }

        /// <summary>Test we can retrieve raw delivery from APNX as a blob association on campaign</summary>
        [TestMethod]
        public void RetrieveRawDeliveryDataAppNexusBlobAssociation()
        {
            var expectedReportDate = this.time10pmESTasUTC;

            // Set up the stub for getting the raw delivery data blobs
            var dynDeliveryDataBlobEntityIds = new List<EntityId>();
            foreach (var deliveryDataResource in new[] { "Resources.ApnxDeliveryDataA.csv", "Resources.ApnxDeliveryDataB.csv" })
            {
                var deliveryDataCsv = EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(GetCampaignDeliveryDataFixture), deliveryDataResource);
                var dynDeliveryDataBlobEntityId = new EntityId();
                dynDeliveryDataBlobEntityIds.Add(dynDeliveryDataBlobEntityId);
                var dynDeliveryDataBlob = BlobEntity.BuildBlobEntity(dynDeliveryDataBlobEntityId, deliveryDataCsv);
                dynDeliveryDataBlob.LastModifiedDate = expectedReportDate;
                RepositoryStubUtilities.SetupGetEntityStub(
                    this.repository, dynDeliveryDataBlobEntityId, dynDeliveryDataBlob, false);
            }

            // Set up the stub for getting the raw delivery data index blob
            var dynDeliveryDataIndexBlobEntityId = new EntityId();
            var deliveryDataIndex = new List<string>(dynDeliveryDataBlobEntityIds.Select(id => id.ToString()));
            var dynDeliveryDataIndexBlob = BlobEntity.BuildBlobEntity(dynDeliveryDataIndexBlobEntityId, deliveryDataIndex);
            dynDeliveryDataIndexBlob.LastModifiedDate = this.time10pmESTasUTC;
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, dynDeliveryDataIndexBlobEntityId, dynDeliveryDataIndexBlob, false);

            // Setup campaign
            this.campaignEntity.Associations.Add(
                new Association
                {
                    ExternalName = AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                    TargetEntityId = dynDeliveryDataIndexBlobEntityId
                });

            // Assert we can retieve this raw delivery data index
            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems().ToList();
            Assert.AreEqual(rawDeliveryDataIndexes.Single().DeliveryNetwork, DeliveryNetworkDesignation.AppNexus);
            Assert.AreEqual(2, rawDeliveryDataIndexes.Single().RawDeliveryDataEntityIds.Count());

            // Assert we can retrieve the raw delivery data
            var rawDeliveryDataItem = rawData.RetrieveRawDeliveryDataItem(dynDeliveryDataBlobEntityIds[0]);
            Assert.IsNotNull(rawDeliveryDataItem.RawDeliveryData);
            Assert.AreEqual(expectedReportDate, rawDeliveryDataItem.DeliveryDataReportDate);
            rawDeliveryDataItem = rawData.RetrieveRawDeliveryDataItem(dynDeliveryDataBlobEntityIds[1]);
            Assert.IsNotNull(rawDeliveryDataItem.RawDeliveryData);
            Assert.AreEqual(expectedReportDate, rawDeliveryDataItem.DeliveryDataReportDate);
        }

        /// <summary>Test we handle the situation where the index entry blob reference is not valid.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void RetrieveRawDeliveryDataAppNexusBlobAssociationIndexEntityNotFound()
        {
            // Setup campaign
            var blobEntityId = new EntityId();
            this.campaignEntity.Associations.Add(
                new Association
                {
                    ExternalName = AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                    TargetEntityId = blobEntityId
                });

            // Set up stub to fail
            RepositoryStubUtilities.SetupGetEntityStub(
                this.repository, blobEntityId, null, true);

            var rawData = new RawDeliveryData(this.repository, this.companyEntity, this.campaignEntity);
            var rawDeliveryDataIndexes = rawData.RetrieveRawDeliveryDataIndexItems();
            Assert.IsNull(rawDeliveryDataIndexes);
        }

        /// <summary>Setup a campaign with a raw delivery data index as a property.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        /// <param name="sourceCampaignEntity">The campaign to set up.</param>
        /// <param name="deliveryDataResources">The delivery data resources.</param>
        /// <param name="latestDeliveryReportDate">The latest delivery report date to use.</param>
        /// <param name="deliveryDataIndexPropertyName">The delivery data index property name.</param>
        /// <param name="failEntityPayload">True if raw data entity payload should not be setup.</param>
        /// <param name="failEntity">True if getting the raw data entity should fail.</param>
        /// <returns>The index of raw data entity id's.</returns>
        internal static List<string> SetupRawDeliveryDataOnCampaign(
            IEntityRepository repository,
            CampaignEntity sourceCampaignEntity,
            string[] deliveryDataResources,
            DateTime latestDeliveryReportDate,
            string deliveryDataIndexPropertyName,
            bool failEntityPayload,
            bool failEntity)
        {
            var rawDeliveryDataCsvs = deliveryDataResources.Select(r =>
                    EmbeddedResourceHelper.GetEmbeddedResourceAsString(typeof(RawDeliveryDataFixture), r))
                    .ToArray();

            return SetupRawDeliveryDataOnCampaignCsv(
                repository,
                sourceCampaignEntity,
                rawDeliveryDataCsvs,
                latestDeliveryReportDate,
                deliveryDataIndexPropertyName,
                failEntityPayload,
                failEntity);
        }

        /// <summary>Setup a campaign with a raw delivery data index as a property.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        /// <param name="sourceCampaignEntity">The campaign to set up.</param>
        /// <param name="rawDeliveryDataCsvs">The delivery data csv's.</param>
        /// <param name="latestDeliveryReportDate">The latest delivery report date to use.</param>
        /// <param name="deliveryDataIndexPropertyName">The delivery data index property name.</param>
        /// <param name="failEntityPayload">True if raw data entity payload should not be setup.</param>
        /// <param name="failEntity">True if getting the raw data entity should fail.</param>
        /// <returns>The index of raw data entity id's.</returns>
        internal static List<string> SetupRawDeliveryDataOnCampaignCsv(
            IEntityRepository repository,
            CampaignEntity sourceCampaignEntity,
            string[] rawDeliveryDataCsvs,
            DateTime latestDeliveryReportDate,
            string deliveryDataIndexPropertyName,
            bool failEntityPayload,
            bool failEntity)
        {
            var rawDeliveryDataIndex = new List<string>();

            var currentReportDate = latestDeliveryReportDate;

            foreach (var rawDeliveryDataCsv in rawDeliveryDataCsvs)
            {
                var rawDeliveryDataEntityId = new EntityId();
                var rawDeliveryDataEntity = EntityTestHelpers.CreateTestPartnerEntity(
                    rawDeliveryDataEntityId, "ExternalNameFoo");

                // Walk the report dates back in time as we add them to the index
                rawDeliveryDataEntity.LastModifiedDate = currentReportDate;
                currentReportDate = currentReportDate.AddDays(-1);

                if (!failEntityPayload)
                {
                    rawDeliveryDataEntity.SetPropertyValueByName(
                        DynamicAllocationEntityProperties.RawDeliveryDataEntityPayloadName, rawDeliveryDataCsv);
                }

                RepositoryStubUtilities.SetupGetEntityStub(
                    repository, rawDeliveryDataEntityId, rawDeliveryDataEntity, failEntity);

                rawDeliveryDataIndex.Add((EntityId)rawDeliveryDataEntity.ExternalEntityId);
            }

            // Setup raw delivery data index property
            var indexJson = AppsJsonSerializer.SerializeObject(rawDeliveryDataIndex);

            // Setup campaign
            sourceCampaignEntity.SetPropertyValueByName(deliveryDataIndexPropertyName, indexJson);

            return rawDeliveryDataIndex;
        }
    }
}
