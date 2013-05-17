//-----------------------------------------------------------------------
// <copyright file="DeleteLineItemActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Activity for deleting AppNexus line-items associated with campaigns
    /// </summary>
    /// <remarks>
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CampaignEntityId - The EntityId of the Campaign
    /// ResultValues:
    ///   LineItemId - The AppNexus id of the deleted line-item
    ///   CampaignEntityId - The EntityId of the Campaign
    /// </remarks>
    [Name(AppNexusActivityTasks.DeleteLineItem)]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CampaignEntityId)]
    [ResultValues(AppNexusActivityValues.LineItemId, EntityActivityValues.CampaignEntityId)]
    public class DeleteLineItemActivity : AppNexusActivity
    {
        /// <summary>Gets the company and campaign entities</summary>
        /// <param name="context">Repository context</param>
        /// <param name="companyEntityId">Company EntityId</param>
        /// <param name="campaignEntityId">Campaign EntityId</param>
        /// <param name="errorResult">
        /// Null if successful; otherwise a result containing the error
        /// </param>
        /// <returns>Tuple containing the entities</returns>
        internal Tuple<CompanyEntity, CampaignEntity> GetEntities(
            RequestContext context,
            EntityId companyEntityId,
            EntityId campaignEntityId,
            out ActivityResult errorResult)
        {
            errorResult = null;

            // Get the entities
            var companyEntity = this.Repository.TryGetEntity(context, companyEntityId) as CompanyEntity;
            if (companyEntity == null)
            {
                errorResult = EntityNotFoundError(companyEntityId);
                return null;
            }

            var campaignEntity = this.Repository.TryGetEntity(context, campaignEntityId) as CampaignEntity;
            if (campaignEntity == null)
            {
                errorResult = EntityNotFoundError(campaignEntityId);
                return null;
            }

            return new Tuple<CompanyEntity, CampaignEntity>(companyEntity, campaignEntity);
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            var context = CreateContext(request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);

            // Get the entities
            ActivityResult error = null;
            var entities = this.GetEntities(context, companyEntityId, campaignEntityId, out error);
            if (entities == null)
            {
                return error;
            }

            var companyEntity = entities.Item1;
            var campaignEntity = entities.Item2;

            // Get the AppNexus advertiser id
            var advertiserId = companyEntity.GetAppNexusAdvertiserId();
            if (advertiserId == null)
            {
                return ErrorResult(
                    ActivityErrorId.GenericError,
                    "The company '{0}' ({1}) does not have an AppNexus advertiser id value",
                    companyEntity.ExternalName,
                    companyEntity.ExternalEntityId);
            }

            // Get the AppNexus line-item id
            var lineItemId = campaignEntity.GetAppNexusLineItemId();
            if (lineItemId == null)
            {
                return ErrorResult(
                    ActivityErrorId.GenericError,
                    "The campaign '{0}' ({1}) does not have an AppNexus line-item id value",
                    companyEntity.ExternalName,
                    companyEntity.ExternalEntityId);
            }

            // Get the AppNexus domain list id (if any)
            var includeDomainListId = campaignEntity.GetAppNexusIncludeDomainListId();

            using (var client = this.CreateAppNexusClient(context, companyEntity, campaignEntity))
            {
                // Delete the line-item
                client.DeleteLineItem((int)advertiserId, (int)lineItemId);
                LogManager.Log(
                    LogLevels.Information,
                    "Deleted AppNexus line-item {0} for campaign '{1}' ({2})",
                    lineItemId,
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);
                
                // Delete the domain list (if any)
                if (includeDomainListId != null)
                {
                    client.DeleteDomainList((int)includeDomainListId);
                    LogManager.Log(
                        LogLevels.Information,
                        "Deleted AppNexus domain list {0} for campaign '{1}' ({2})",
                        (int)includeDomainListId,
                        campaignEntity.ExternalName,
                        campaignEntity.ExternalEntityId);
                }
            }

            // Return the AppNexus id of the deleted line-item and its campaign
            return this.SuccessResult(new Dictionary<string, string>
            {
                { AppNexusActivityValues.LineItemId, lineItemId.ToString() },
                { EntityActivityValues.CampaignEntityId, campaignEntity.ExternalEntityId.ToString() }
            });
        }
    }
}
