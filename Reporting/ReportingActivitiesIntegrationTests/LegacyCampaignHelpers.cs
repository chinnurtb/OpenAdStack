// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LegacyCampaignHelpers.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;
using DynamicAllocationTestUtilities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using SimulatedDataStore;
using TestUtilities;
using Utilities.Serialization;

namespace ReportingActivitiesIntegrationTests
{
    /// <summary>
    /// Helper methods for legacy campaign tests
    /// </summary>
    internal static class LegacyCampaignHelpers
    {
        /// <summary>
        /// Setup a legacy (v none) test campaign with delivery and measure data.
        /// Get stubs are set up on Rhino unless repository is a SimulatedEntityRepository.
        /// This will work for narrow unit-tests that can set up save stubs for data they control.
        /// The simulated repository is more appropriate for integration tests that have interiors saves
        /// inaccessible to the test.
        /// </summary>
        /// <param name="repository">The entity Repository.</param>
        /// <param name="companyEntityId">The company entity id.</param>
        /// <param name="campaignEntityId">The campaign entity id.</param>
        internal static void SetupLegacyCampaign(IEntityRepository repository, EntityId companyEntityId, EntityId campaignEntityId)
        {
            var deprecateMeasuresJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ActivityIntegrationTestsFixture), "Resources.ValuationInputs_Measures.js");
            var deprecateBaseValuationSetJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ActivityIntegrationTestsFixture), "Resources.ValuationInputs_BaseValuations.js");
            var deprecateNodeValuationSetJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ActivityIntegrationTestsFixture), "Resources.ValuationInputs_EmptyNodeValuations.js");
            var rawDeliveryDataCsv1 = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ActivityIntegrationTestsFixture), "Resources.ApnxDeliveryData1.csv");
            var nodeMapJson = EmbeddedResourceHelper.GetEmbeddedResourceAsString(
                typeof(ActivityIntegrationTestsFixture), "Resources.NodeMap.js");

            // Build a company and setup stub
            var companyJson =
                @"{{""EntityCategory"":""{0}"",""ExternalName"":""{1}""}}".FormatInvariant(
                    CompanyEntity.CompanyEntityCategory, "CompanyFoo");
            var companyEntity = EntityJsonSerializer.DeserializeCompanyEntity(companyEntityId, companyJson);

            // Build a campaign
            var campaignJson =
                @"{{""EntityCategory"":""{0}"",""ExternalName"":""{1}""}}".FormatInvariant(
                    CampaignEntity.CampaignEntityCategory, "CampaignFoo");
            var campaignEntity = EntityJsonSerializer.DeserializeCampaignEntity(campaignEntityId, campaignJson);

            // set up legacy valuation inputs
            var measureSetEntityId = new EntityId();
            var baseValuationSetEntityId = new EntityId();
            var nodeValuationSetEntityId = new EntityId();

            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = "MeasureSetApproved",
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = measureSetEntityId,
                TargetExternalType = "???"
            });
            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = "BaseValuationSetApproved",
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = baseValuationSetEntityId,
                TargetExternalType = "???"
            });
            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = "NodeValuationSetApproved",
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = nodeValuationSetEntityId,
                TargetExternalType = "???"
            });

            // Setup legacy raw delivery data blob
            var rawDeliveryDataEntityId1 = new EntityId();
            var rawDeliveryDataEntity1 = BlobEntity.BuildBlobEntity(rawDeliveryDataEntityId1, rawDeliveryDataCsv1) as IEntity;

            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = "APNX_RawDeliveryData",
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = rawDeliveryDataEntityId1,
                TargetExternalType = "???"
            });

            // Set node map
            var nodeMapId = new EntityId();
            var nodeMap = AppsJsonSerializer.DeserializeObject<Dictionary<string, long[]>>(nodeMapJson);

            campaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = DynamicAllocationEntityProperties.AllocationNodeMap,
                TargetEntityCategory = BlobEntity.BlobEntityCategory,
                TargetEntityId = nodeMapId,
                TargetExternalType = "???"
            });

            // Setup default allocation paremeters.
            AllocationParametersTestHelpers.Initialize(campaignEntity);

            // Save the entities on a simulated repository
            var requestContext = new RequestContext
            {
                ExternalCompanyId = companyEntityId,
                EntityFilter = new RepositoryEntityFilter(true, true, true, true)
            };

            repository.AddCompany(requestContext, companyEntity);
            repository.SaveUser(requestContext, ownerEntity);
            SetupLegacyBlob(repository, requestContext, measureSetEntityId, deprecateMeasuresJson);
            SetupLegacyBlob(repository, requestContext, nodeValuationSetEntityId, deprecateNodeValuationSetJson);
            SetupLegacyBlob(repository, requestContext, baseValuationSetEntityId, deprecateBaseValuationSetJson);
            SetupLegacyBlob(repository, requestContext, nodeMapId, nodeMap);
            repository.SaveEntity(requestContext, rawDeliveryDataEntity1);
            repository.SaveEntity(requestContext, campaignEntity);
        }

        /// <summary>We only need to do this for blobs that were saved as xml serialized non-string data.</summary>
        /// <param name="repository">The repository</param>
        /// <param name="context">The request context</param>
        /// <param name="blobEntityId">The blob entity id</param>
        /// <param name="objToBlob">The object to save as a blob.</param>
        internal static void SetupLegacyBlob(IEntityRepository repository, RequestContext context, EntityId blobEntityId, object objToBlob)
        {
            var isSim = repository is SimulatedEntityRepository;

            // If repository is not a proxy stub or simulated we need to actually
            // save a legacy blob. Use the SimulatedEntityRepository helper to do this for us.
            if (!isSim)
            {
                LegacyBlobHelpers.SaveLegacyBlob(repository, context, blobEntityId, objToBlob);
                return;
            }

            // We only need an in-memory BlobEntity with legacy (xml serialized) bytes
            var blobEntity = LegacyBlobHelpers.GetBlobWithLegacyBytes(blobEntityId, objToBlob);

            repository.SaveEntity(null, blobEntity);
        }
    }
}
