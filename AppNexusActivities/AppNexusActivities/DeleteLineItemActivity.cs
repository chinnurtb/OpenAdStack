//-----------------------------------------------------------------------
// <copyright file="DeleteLineItemActivity.cs" company="Rare Crowds Inc">
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
        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            var context = CreateContext(request);
            var companyEntity = this.Repository.TryGetEntity<CompanyEntity>(context, request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntity = this.Repository.TryGetEntity<CampaignEntity>(context, request.Values[EntityActivityValues.CampaignEntityId]);

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
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);
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
