//-----------------------------------------------------------------------
// <copyright file="CreateCampaignActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Activities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for creating a Campaign
    /// </summary>
    /// <remarks>
    /// Creates a campaign and associates it with the company
    /// TODO: Should the campaign already have the company association in it? yes?
    /// RequiredValues
    ///   CompanyEntityId - ExternalEntityId of the company to add campaigns to
    ///   CampaignEntityId - ExternalEntityId for the campaign
    ///   Campaign - The campaign as json
    /// </remarks>
    [Name(EntityActivityTasks.CreateCampaign)]
    [RequiredValues(
        EntityActivityValues.EntityId,
        EntityActivityValues.ParentEntityId,
        EntityActivityValues.MessagePayload)]
    [ResultValues(EntityActivityValues.Campaign)]
    public class CreateCampaignActivity : EntityActivity
    {
        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(RepositoryContextType.ExternalEntitySave, request, EntityActivityValues.ParentEntityId);
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);

            // Get data from request
            var owner = (UserEntity)this.Repository.GetUser(internalContext, externalContext.UserId);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.ParentEntityId]);
            var company = (CompanyEntity)this.Repository.TryGetEntity(internalContext, companyEntityId);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
            var campaign = EntityJsonSerializer.DeserializeCampaignEntity(
                campaignEntityId,
                request.Values[EntityActivityValues.MessagePayload]);
            var config = EntityActivityUtilities.BuildCustomConfigFromEntities(company, campaign);

            // Check if the campaign already exists
            var original = this.Repository.TryGetEntity(internalContext, campaignEntityId) as CampaignEntity;
            if (original != null)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "A campaign with the same ExternalEntityId ({0}) already exists!",
                    campaignEntityId);
                
                // TODO: Throw an exception/return error result???
            }
            
            // Set the owner
            campaign.SetOwnerId(owner.UserId);

            // Set the default properties
            SetDefaultProperties(config, company, ref campaign);

            // Add the new campaign
            this.Repository.SaveEntity(externalContext, campaign);

            // Associate the new campaign with the Company
            // TODO replace "campaign" with constant
            this.Repository.AssociateEntities(internalContext, companyEntityId, "campaign", string.Empty, new HashSet<IEntity> { campaign }, AssociationType.Child, false);

            // Return a result with the added campaign as the output
            // The added campaign will have additional data assigned by
            // the DAL such as the EntityId
            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Campaign, campaign.SerializeToJson() }
            });
        }

        /// <summary>Sets default properties on the campaign</summary>
        /// <param name="config">The configuration</param>
        /// <param name="company">The company</param>
        /// <param name="campaign">The campaign</param>
        private static void SetDefaultProperties(IConfig config, CompanyEntity company, ref CampaignEntity campaign)
        {
            SetDefaultDeliveryNetwork(config, company, ref campaign);
            SetDefaultExporterVersion(config, ref campaign);
        }

        /// <summary>Sets the default delivery network</summary>
        /// <remarks>
        /// If defined, uses the company's delivery network.
        /// Otherwise uses the default from the config
        /// </remarks>
        /// <param name="config">The config</param>
        /// <param name="company">The company</param>
        /// <param name="campaign">The campaign</param>
        private static void SetDefaultDeliveryNetwork(IConfig config, CompanyEntity company, ref CampaignEntity campaign)
        {
            if (campaign.TryGetPropertyValueByName(DeliveryNetworkEntityProperties.DeliveryNetwork) == null)
            {
                var deliveryNetwork = company.GetDeliveryNetwork();
                if (deliveryNetwork == DeliveryNetworkDesignation.Unknown)
                {
                    deliveryNetwork = config.GetEnumValue<DeliveryNetworkDesignation>("Delivery.DefaultNetwork");
                }

                campaign.SetDeliveryNetwork(deliveryNetwork);
            }
        }

        /// <summary>Sets the default exporter version</summary>
        /// <param name="config">Config to get the default from</param>
        /// <param name="campaign">The campaign</param>
        private static void SetDefaultExporterVersion(IConfig config, ref CampaignEntity campaign)
        {
            if (!campaign.HasSystemProperty(DeliveryNetworkEntityProperties.ExporterVersion))
            {
                try
                {
                    var exporterVersion =
                        campaign.GetDeliveryNetwork() == DeliveryNetworkDesignation.AppNexus ?
                            config.GetIntValue("AppNexus.DefaultExporterVersion") :
                        campaign.GetDeliveryNetwork() == DeliveryNetworkDesignation.GoogleDfp ?
                            config.GetIntValue("GoogleDfp.DefaultExporterVersion") :
                            0;
                    campaign.SetSystemProperty(
                        DeliveryNetworkEntityProperties.ExporterVersion,
                        exporterVersion);
                }
                catch (ArgumentException ae)
                {
                    LogManager.Log(LogLevels.Error, "Error getting default exporter version: {0}", ae);
                }
            }
        }
    }
}
