// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicAllocationCampaignTestStub.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using TestUtilities;
using Utilities.Serialization;

namespace DynamicAllocationTestUtilities
{
    /// <summary>
    /// Helper class to build a DA campaign with delivery data
    /// </summary>
    public class DynamicAllocationCampaignTestStub
    {
        /// <summary>Apnx raw delivery data for testing (first report pull)</summary>
        private readonly string rawDeliveryDataCsv1;

        /// <summary>Apnx raw delivery data for testing (second report pull)</summary>
        private readonly string rawDeliveryDataCsv2;

        /// <summary>measure list json for testing</summary>
        private readonly string measureListJson;

        /// <summary>node map json for testing</summary>
        private readonly string nodeMapJson;

        /// <summary>Initializes a new instance of the <see cref="DynamicAllocationCampaignTestStub"/> class.</summary>
        public DynamicAllocationCampaignTestStub()
        {
            // Get the raw data
            this.rawDeliveryDataCsv1 = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(DynamicAllocationCampaignTestStub), "Resources.ApnxDeliveryData1.csv");
            this.rawDeliveryDataCsv2 = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(DynamicAllocationCampaignTestStub), "Resources.ApnxDeliveryData2.csv");
            this.measureListJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(DynamicAllocationCampaignTestStub), "Resources.MeasureList.js");
            this.nodeMapJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(DynamicAllocationCampaignTestStub), "Resources.NodeMap.js");
        }

        /// <summary>
        /// Setup a test campaign with delivery and measure data.
        /// Get stubs are set up on Rhino unless repository is a SimulatedEntityRepository.
        /// This will work for narrow unit-tests that can set up save stubs for data they control.
        /// The simulated repository is more appropriate for integration tests that have interiors saves
        /// inaccessible to the test.
        /// </summary>
        /// <param name="repository">The entity Repository.</param>
        /// <param name="companyEntityId">The company entity id.</param>
        /// <param name="campaignEntityId">The campaign entity id.</param>
        /// <param name="campaignOwnerId">The campaign owner user id</param>
        public void SetupCampaign(IEntityRepository repository, EntityId companyEntityId, EntityId campaignEntityId, string campaignOwnerId)
        {
            // Determine what kind of stubs we should set up
            var isProxy = CheckIfProxyStub(repository);

            // Build a company and setup stub
            var companyJson =
                @"{{""EntityCategory"":""{0}"",""ExternalName"":""{1}""}}".FormatInvariant(
                    CompanyEntity.CompanyEntityCategory, "CompanyFoo");
            var companyEntity = EntityJsonSerializer.DeserializeCompanyEntity(companyEntityId, companyJson);

            // Build an owner user
            var ownerEntity = EntityTestHelpers.CreateTestUserEntity(new EntityId(), campaignOwnerId, "nobody@rc.dev");

            // Build a campaign
            var campaignJson =
                @"{{""EntityCategory"":""{0}"",""ExternalName"":""{1}""}}".FormatInvariant(
                    CampaignEntity.CampaignEntityCategory, "CampaignFoo");
            var campaignEntity = EntityJsonSerializer.DeserializeCampaignEntity(campaignEntityId, campaignJson);
            campaignEntity.LastModifiedDate = DateTime.UtcNow;

            campaignEntity.SetOwnerId(campaignOwnerId);

            // Build raw delivery data blob entities
            var rawDeliveryDataEntityId1 = new EntityId();
            var rawDeliveryDataEntity1 = BlobEntity.BuildBlobEntity(rawDeliveryDataEntityId1, this.rawDeliveryDataCsv1) as IEntity;
            rawDeliveryDataEntity1.LastModifiedDate = DateTime.UtcNow;

            var rawDeliveryDataEntityId2 = new EntityId();
            var rawDeliveryDataEntity2 = BlobEntity.BuildBlobEntity(rawDeliveryDataEntityId2, this.rawDeliveryDataCsv2) as IEntity;
            rawDeliveryDataEntity2.LastModifiedDate = DateTime.UtcNow;

            // Set delivery data index
            var deliveryDataIndexJson =
                AppsJsonSerializer.SerializeObject(
                    new List<string> { rawDeliveryDataEntityId1.ToString(), rawDeliveryDataEntityId2.ToString() });
            campaignEntity.SetPropertyValueByName(AppNexusEntityProperties.AppNexusRawDeliveryDataIndex, deliveryDataIndexJson);

            // Set measure inputs
            campaignEntity.SetPropertyValueByName(DynamicAllocationEntityProperties.MeasureList, this.measureListJson);

            // Set node map
            var nodeMapId = new EntityId();
            var nodeMapEntity = new BlobEntity(nodeMapId, this.nodeMapJson) as IEntity;

            campaignEntity.TryAssociateEntities(
                DynamicAllocationEntityProperties.AllocationNodeMap,
                string.Empty,
                new HashSet<IEntity> { nodeMapEntity },
                AssociationType.Relationship,
                true);

            // Set delivery network
            campaignEntity.SetPropertyValueByName(
                DeliveryNetworkEntityProperties.DeliveryNetwork, DeliveryNetworkDesignation.AppNexus.ToString());

            // Set Status approved
            campaignEntity.SetPropertyValueByName(DynamicAllocationEntityProperties.Status, DynamicAllocationEntityProperties.StatusApproved);

            // Setup default allocation paremeters.
            AllocationParametersTestHelpers.Initialize(campaignEntity);

            // Save the entities on a simulated repository
            if (!isProxy)
            {
                var requestContext = new RequestContext
                    {
                        ExternalCompanyId = companyEntityId,
                        EntityFilter = new RepositoryEntityFilter(true, true, true, true)
                    };
                repository.AddCompany(requestContext, companyEntity);
                repository.SaveEntity(requestContext, rawDeliveryDataEntity1);
                repository.SaveEntity(requestContext, rawDeliveryDataEntity2);
                repository.SaveEntity(requestContext, nodeMapEntity);
                repository.SaveEntity(requestContext, campaignEntity);
                repository.SaveUser(requestContext, ownerEntity);
                return;
            }

            // Setup Rhino stubs
            RepositoryStubUtilities.SetupGetEntityStub(repository, rawDeliveryDataEntityId1, rawDeliveryDataEntity1, false);
            RepositoryStubUtilities.SetupGetEntityStub(repository, rawDeliveryDataEntityId2, rawDeliveryDataEntity2, false);
            RepositoryStubUtilities.SetupGetEntityStub(repository, nodeMapId, nodeMapEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(repository, companyEntityId, companyEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(repository, campaignEntityId, campaignEntity, false);
            RepositoryStubUtilities.SetupGetUserStub(repository, campaignOwnerId, ownerEntity, false);
        }

        /// <summary>Determine if the repository is a Rhino proxy stub</summary>
        /// <param name="repository">The repository.</param>
        /// <returns>True if this is a proxy stub.</returns>
        private static bool CheckIfProxyStub(IEntityRepository repository)
        {
            return repository.GetType().FullName.Contains("IEntityRepositoryProxy");
        }
    }
}