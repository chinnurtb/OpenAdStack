//-----------------------------------------------------------------------
// <copyright file="UpdateCreativeAuditStatus.cs" company="Rare Crowds Inc">
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
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Activity for updating the audit status of campaigns exported to AppNexus
    /// </summary>
    /// <remarks>
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CreativeEntityId - The EntityId of the Creative
    /// ResultValues:
    ///   CreativeEntityId - The EntityId of the Creative
    ///   AuditStatus - The updated creative audit status
    /// </remarks>
    [Name(AppNexusActivityTasks.UpdateCreativeAuditStatus)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CreativeEntityId)]
    [ResultValues(
        EntityActivityValues.CreativeEntityId,
        AppNexusActivityValues.AuditStatus,
        EntityActivityValues.CompanyEntityId)]
    public class UpdateCreativeAuditStatus : AppNexusActivity
    {
        /// <summary>Updates a creative's AppNexus audit status</summary>
        /// <remarks>
        /// Gets the creative's current AppNexus audit status,
        /// then updates and saves the entity.
        /// </remarks>
        /// <param name="repository">Entity repository</param>
        /// <param name="context">Repository context</param>
        /// <param name="client">AppNexus API client</param>
        /// <param name="creativeEntity">Creative to update</param>
        /// <returns>The updated status</returns>
        public static string UpdateAuditStatus(
            IEntityRepository repository,
            RequestContext context,
            IAppNexusApiClient client,
            ref CreativeEntity creativeEntity)
        {
            // Get the creative AppNexus id
            var creativeId = creativeEntity.GetAppNexusCreativeId();
            if (creativeId == null)
            {
                var msg = "The creative '{0}' ({1}) has no AppNexus creative id"
                    .FormatInvariant(
                        creativeEntity.ExternalName,
                        creativeEntity.ExternalEntityId);
                throw new ActivityException(ActivityErrorId.GenericError, msg);
            }

            // Get the creative from AppNexus
            var creativeValues = client.GetCreative((int)creativeId);
            if (creativeValues == null)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "AppNexus creative '{0}' for CreativeEntity '{1}' ({2}) could not be found",
                    creativeId,
                    creativeEntity.ExternalName,
                    creativeEntity.ExternalEntityId);
                return "DELETED";
            }

            // TODO: error check
            var auditStatus = creativeValues[AppNexusValues.AuditStatus] as string;

            // Update the creative entities status and save it
            creativeEntity.SetAppNexusAuditStatus(auditStatus);
            repository.SaveEntity(context, creativeEntity);

            return auditStatus;
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            // Get the entities
            var context = CreateContext(request);
            var companyEntity = this.Repository.GetEntity<CompanyEntity>(
                context, request.Values[EntityActivityValues.CompanyEntityId]);
            var creativeEntity = this.Repository.GetEntity<CreativeEntity>(
                context, request.Values[EntityActivityValues.CreativeEntityId]);
            var creativeOwner = this.Repository.GetUser(
                context, creativeEntity.GetOwnerId());

            // Verify the creative has been exported
            if (creativeEntity.GetAppNexusCreativeId() == null)
            {
                return ErrorResult(
                    ActivityErrorId.GenericError,
                    "The creative '{0}' ({1}) has no AppNexus creative id",
                    creativeEntity.ExternalName,
                    creativeEntity.ExternalEntityId);
            }

            // Update the creative's audit status
            using (var client = this.CreateAppNexusClient(
                context, companyEntity, creativeEntity, creativeOwner))
            {
                var auditStatus = UpdateAuditStatus(this.Repository, context, client, ref creativeEntity);

                // Return the creative ID to the activity request source
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CreativeEntityId, creativeEntity.ExternalEntityId.ToString() },
                    { EntityActivityValues.CompanyEntityId, companyEntity.ExternalEntityId.ToString() },
                    { AppNexusActivityValues.AuditStatus, auditStatus }
                });
            }
        }
    }
}
