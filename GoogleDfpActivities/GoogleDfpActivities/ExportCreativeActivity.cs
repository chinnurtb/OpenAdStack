//-----------------------------------------------------------------------
// <copyright file="ExportCreativeActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using Google.Api.Ads.Dfp.v201206;
using GoogleDfpActivities.Exporters;
using GoogleDfpClient;
using GoogleDfpUtilities;
using Newtonsoft.Json;
using ScheduledActivities;

namespace GoogleDfpActivities
{
    /// <summary>Activity for exporting Google DFP creatives from CreativeEntities</summary>
    /// <remarks>
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CreativeEntityId - The EntityId of the CreativeEntity
    /// ResultValues:
    ///   CreativeEntityId - The EntityId of the CreativeEntity
    ///   CreativeId - The Google DFP id of the creative
    /// </remarks>
    [Name(GoogleDfpActivityTasks.ExportCreative)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CreativeEntityId)]
    [ResultValues(
        EntityActivityValues.CreativeEntityId,
        GoogleDfpActivityValues.CreativeId)]
    public class ExportCreativeActivity : DfpActivity
    {
        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessDfpRequest(ActivityRequest request)
        {
            var context = CreateContext(request);
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var creativeEntityId = new EntityId(request.Values[EntityActivityValues.CreativeEntityId]);

            // Get the entities
            var companyEntity = (CompanyEntity)this.Repository.TryGetEntity(context, companyEntityId);
            var creativeEntity = (CreativeEntity)this.Repository.TryGetEntity(context, creativeEntityId);

            try
            {
                // Create the exporter instance
                using (var exporter = new DfpCreativeExporter(companyEntity, creativeEntity))
                {
                    // Create the DFP advertiser (if needed).
                    if (!exporter.AdvertiserExists)
                    {
                        var advertiserId = exporter.CreateAdvertiser();
                        companyEntity.SetDfpAdvertiserId(advertiserId);
                        this.Repository.SaveEntity(context, companyEntity);
                    }

                    // Create the creative
                    var creativeId = exporter.CreateCreative();
                    creativeEntity.SetDfpCreativeId(creativeId);
                    this.Repository.SaveEntity(context, creativeEntity);
                }

                // Return the creative entity and Google DFP creative id to the activity request source
                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CreativeEntityId, creativeEntity.ExternalEntityId.ToString() },
                    { GoogleDfpActivityValues.CreativeId, creativeEntity.GetDfpCreativeId().ToString() }
                });
            }
            catch (GoogleDfpClientException dfpe)
            {
                LogManager.Log(
                    LogLevels.Error,
                    true,
                    "Google DFP Export Failed for creative '{0}' ({1}):\n{2}",
                    creativeEntity.ExternalName,
                    creativeEntityId,
                    dfpe);
                return this.DfpClientError(dfpe);
            }
        }
    }
}
