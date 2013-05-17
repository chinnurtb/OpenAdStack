// -----------------------------------------------------------------------
// <copyright file="RawDeliveryData.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using AppNexusUtilities;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;
using GoogleDfpUtilities;
using Utilities;
using Utilities.Serialization;

namespace DynamicAllocationActivities
{
    /// <summary>Class to handle extracting raw delivery data from a DynamicAllocation campaign entity.</summary>
    public class RawDeliveryData : IRawDeliveryData
    {
        /// <summary>Initializes a new instance of the <see cref="RawDeliveryData"/> class.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        /// <param name="companyEntity">The companyEntity for the campaign.</param>
        /// <param name="campaignEntity">The campaignEntity.</param>
        public RawDeliveryData(IEntityRepository repository, CompanyEntity companyEntity, CampaignEntity campaignEntity)
        {
            this.Repository = repository;
            this.CompanyEntity = companyEntity;
            this.CampaignEntity = campaignEntity;
        }

        /// <summary>Gets and IEntityRepository instance.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Gets CampaignEntity</summary>
        internal CampaignEntity CampaignEntity { get; private set; }

        /// <summary>Gets CompanyEntity</summary>
        internal CompanyEntity CompanyEntity { get; private set; }

        /// <summary>Retrieve zero or more raw delivery data indexes for a campaign from storage.</summary>
        /// <returns>The indexes items. No partial success. Collection may be empty if no indexes were found but there were no failures.</returns>
        public IEnumerable<RawDeliveryDataIndexItem> RetrieveRawDeliveryDataIndexItems()
        {
            var indexItems = new List<RawDeliveryDataIndexItem>();

            // Optimistically try to get APNX raw delivery data as a property
            var rawDeliveryDataIndex = RetrieveRawDeliveryDataIndexAsProperty(
                this.CampaignEntity, AppNexusEntityProperties.AppNexusRawDeliveryDataIndex);

            // Treat property based or legacy blob index as exclusive
            if (rawDeliveryDataIndex == null)
            {
                // See if we have raw APNX delivery data in a legacy blob entity
                rawDeliveryDataIndex = this.RetrieveAppNexusRawDeliveryDataIndexAsBlob(
                    this.CampaignEntity, AppNexusEntityProperties.AppNexusRawDeliveryDataIndex);
            }

            // Null just means it's not present
            if (rawDeliveryDataIndex != null)
            {
                indexItems.Add(
                    new RawDeliveryDataIndexItem(DeliveryNetworkDesignation.AppNexus, rawDeliveryDataIndex));
            }

            // Try to get DFP raw delivery data as a property
            rawDeliveryDataIndex = RetrieveRawDeliveryDataIndexAsProperty(
                this.CampaignEntity, GoogleDfpEntityProperties.DfpRawDeliveryDataIndex);
            if (rawDeliveryDataIndex != null)
            {
                indexItems.Add(
                    new RawDeliveryDataIndexItem(DeliveryNetworkDesignation.GoogleDfp, rawDeliveryDataIndex));
            }

            return indexItems;
        }

        /// <summary>Get a single raw delivery data item.</summary>
        /// <param name="rawDeliveryDataEntityId">The raw delivery data entity id.</param>
        /// <returns>The raw delivery data item, or null if not found.</returns>
        public RawDeliveryDataItem RetrieveRawDeliveryDataItem(EntityId rawDeliveryDataEntityId)
        {
            var context = new RequestContext { ExternalCompanyId = this.CompanyEntity.ExternalEntityId };

            // Get the raw delivery data entity - throw if it cannot be retrieved
            IEntity rawDeliveryDataEntity;
            try
            {
                rawDeliveryDataEntity = this.Repository.GetEntity(context, rawDeliveryDataEntityId);
            }
            catch (DataAccessEntityNotFoundException e)
            {
                var msg = "Raw Delivery Data entity not found. Entity Id - {0}."
                    .FormatInvariant(rawDeliveryDataEntityId);
                throw new ActivityException(ActivityErrorId.DataAccess, msg, e);
            }

            // Last modified date of the entity is a good proxy for when the report was pulled.
            DateTime deliveryReportDate = rawDeliveryDataEntity.LastModifiedDate;

            // Optimistically get the payload as a property on the entity
            // TODO: This should be converted back to a BlobEntity now that they are not special cases
            var rawDeliveryDataPayloadProperty = rawDeliveryDataEntity.TryGetPropertyValueByName(
                    DynamicAllocationEntityProperties.RawDeliveryDataEntityPayloadName);

            if (rawDeliveryDataPayloadProperty != null)
            {
                return new RawDeliveryDataItem(rawDeliveryDataPayloadProperty, deliveryReportDate);
            }

            // Now treat it as a blob entity and try to get the payload that way.
            string rawDeliveryDataPayload;
            var rawDataBlobEntity = rawDeliveryDataEntity as BlobEntity;

            if (rawDataBlobEntity == null)
            {
                var msg = "Raw Delivery Data payload could not be retrieved from entity: Entity ID - {0}."
                    .FormatInvariant(rawDeliveryDataEntityId);
                throw new ActivityException(ActivityErrorId.GenericError, msg.FormatInvariant(rawDeliveryDataEntityId));
            }

            try
            {
                rawDeliveryDataPayload = rawDataBlobEntity.DeserializeBlob<string>();
            }
            catch (AppsGenericException e)
            {
                var msg = "Raw Delivery Data payload could not be deserialized: Entity ID - {0}."
                    .FormatInvariant(rawDeliveryDataEntityId);
                throw new ActivityException(
                    ActivityErrorId.InvalidJson, msg.FormatInvariant(rawDeliveryDataEntityId), e);
            }

            return new RawDeliveryDataItem(rawDeliveryDataPayload, deliveryReportDate);
        }

        /// <summary>Retrieve a raw delivery data index for a campaign.</summary>
        /// <param name="campaignEntity">The campaign entity.</param>
        /// <param name="indexPropertyName">The index property name.</param>
        /// <returns>An array of raw data entity id's, or null if not found.</returns>
        /// <exception cref="ArgumentException">If the data is present but cannot be fully retrieved.</exception>
        internal static EntityId[] RetrieveRawDeliveryDataIndexAsProperty(
            CampaignEntity campaignEntity,
            string indexPropertyName)
        {
            var deliveryDataIndexProperty = campaignEntity.TryGetPropertyValueByName(indexPropertyName);
            if (deliveryDataIndexProperty == null)
            {
                // Nothing wrong, just not present
                return null;
            }

            // Get the raw delivery data index of blob id's
            var rawDeliveryDataIndex =
                AppsJsonSerializer.DeserializeObject<List<string>>(deliveryDataIndexProperty).Select(
                    id => new EntityId(id)).ToArray();

            return rawDeliveryDataIndex;
        }

        /// <summary>Retrieve a legacy raw delivery data index for a campaign.</summary>
        /// <param name="campaignEntity">The campaign entity.</param>
        /// <param name="indexAssociationName">The index association name.</param>
        /// <returns>An array of raw data entity id's, or null if not found.</returns>
        /// <exception cref="ArgumentException">If the data is present but cannot be fully retrieved.</exception>
        internal EntityId[] RetrieveAppNexusRawDeliveryDataIndexAsBlob(
            CampaignEntity campaignEntity,
            string indexAssociationName)
        {
            var context = new RequestContext { ExternalCompanyId = this.CompanyEntity.ExternalEntityId };

            var rawDeliveryDataIndexAssociation =
                campaignEntity.TryGetAssociationByName(indexAssociationName);
            if (rawDeliveryDataIndexAssociation == null)
            {
                // Nothing wrong, just not present
                return null;
            }

            // Get the raw delivery data index of blob id's
            var indexEntityId = rawDeliveryDataIndexAssociation.TargetEntityId;
            var rawDeliveryDataIndexBlob = this.Repository.GetEntity(context, indexEntityId) as BlobEntity;

            var rawDeliveryDataIndex = rawDeliveryDataIndexBlob.DeserializeBlob<List<string>>()
                .Select(id => new EntityId(id)).ToArray();

            return rawDeliveryDataIndex;
        }
    }
}
