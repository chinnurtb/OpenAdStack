//-----------------------------------------------------------------------
// <copyright file="ExportCreativeActivity.cs" company="Rare Crowds Inc">
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
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using ScheduledActivities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>Activity for exporting creatives to AppNexus</summary>
    /// <remarks>
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CreativeEntityId - The EntityId of the Creative
    /// ResultValues:
    ///   CreativeId - The ID of the AppNexus creative
    /// </remarks>
    [Name(AppNexusActivityTasks.ExportCreative)]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CreativeEntityId)]
    [ResultValues(AppNexusActivityValues.CreativeId, EntityActivityValues.CreativeEntityId)]
    public class ExportCreativeActivity : AppNexusActivity
    {
        /// <summary>Gets the time between AppNexus creative update requests</summary>
        internal static TimeSpan CreativeStatusUpdateFrequency
        {
            get { return Config.GetTimeSpanValue("Delivery.CreativeUpdateFrequency"); }
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
            
            // Check if the creative has already been exported
            if (creativeEntity.GetAppNexusCreativeId() != null &&
                creativeEntity.GetCreativeType() == CreativeType.AppNexus)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Not exporting creative '{0}' ({1}): Creative was imported from AppNexus (AppNexus Creative {2})",
                    creativeEntity.ExternalName,
                    creativeEntity.ExternalEntityId,
                    creativeEntity.GetAppNexusCreativeId());
            }
            else
            {
                this.ExportCreative(context, creativeOwner, ref companyEntity, ref creativeEntity);
            }

            // Return the creative ID to the activity request source
            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.CreativeEntityId, creativeEntity.ExternalEntityId.ToString() },
                { AppNexusActivityValues.CreativeId, creativeEntity.GetAppNexusCreativeId().ToString() }
            });
        }

        /// <summary>Export a new creative to AppNexus</summary>
        /// <param name="context">Repository request context</param>
        /// <param name="creativeOwner">Creative owner user entity</param>
        /// <param name="companyEntity">Company entity</param>
        /// <param name="creativeEntity">Creative to export</param>
        private void ExportCreative(
            RequestContext context,
            UserEntity creativeOwner,
            ref CompanyEntity companyEntity,
            ref CreativeEntity creativeEntity)
        {
            using (var client = this.CreateAppNexusClient(
                context, companyEntity, creativeEntity, creativeOwner))
            {
                // Get the AppNexus advertiser id
                var advertiserId = companyEntity.GetAppNexusAdvertiserId() ??
                    this.CreateAppNexusAdvertiser(client, context, ref companyEntity);
                var creativeId = creativeEntity.GetAppNexusCreativeId();

                using (var exporter = new AppNexusCreativeExporter(
                    advertiserId, companyEntity, creativeEntity, creativeOwner))
                {
                    if (creativeId.HasValue)
                    {
                        // TODO: Support creative updating
                        LogManager.Log(
                            LogLevels.Warning,
                            "Creative updating currently unsupported. Not exporting updating AppNexus creative '{0}' for '{1}' ({2}).",
                            creativeId,
                            creativeEntity.ExternalName,
                            creativeEntity.ExternalEntityId);
                        return;
                    }

                    // Export new creative
                    LogManager.Log(
                        LogLevels.Trace,
                        "Exporting new creative '{0}' ({1}) to AppNexus...",
                        creativeEntity.ExternalName,
                        creativeEntity.ExternalEntityId);
                    creativeId = exporter.ExportCreative();

                    // Add AppNexus creative and profile ids to the CreativeEntity
                    creativeEntity.SetAppNexusCreativeId((int)creativeId);
                }
            }

            this.Repository.SaveEntity(context, creativeEntity);

            // Register the exported CreativeEntity for status updates
            if (!Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                DeliveryNetworkSchedulerRegistries.CreativesToUpdate,
                DateTime.UtcNow + CreativeStatusUpdateFrequency,
                creativeEntity.ExternalEntityId.ToString(),
                companyEntity.ExternalEntityId.ToString(),
                DeliveryNetworkDesignation.AppNexus))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Failed to schedule creative '{0}' ({1}) for status updates",
                    creativeEntity.ExternalName,
                    creativeEntity.ExternalEntityId);
            }
        }
    }
}
