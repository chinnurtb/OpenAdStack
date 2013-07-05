//-----------------------------------------------------------------------
// <copyright file="RetrieveCampaignReportActivity.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.Linq;
using System.Threading;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocationUtilities;
using EntityUtilities;

namespace AppNexusActivities
{
    /// <summary>
    /// Activity for retrieving campaign report for a CampaignEntity based on it's ReportId.
    /// </summary>
    /// <remarks>
    /// Associates different entities.
    /// RequiredValues:
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    ///   ReportId - The AppNexus report id
    /// ResultValues:
    ///   ReportId - The ID of the requested AppNexus report
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    /// </remarks>
    [Name(AppNexusActivityTasks.RetrieveCampaignReport)]
    [RequiredValues(
        AppNexusActivityValues.ReportId,
        EntityActivityValues.CampaignEntityId)]
    [ResultValues(
        AppNexusActivityValues.ReportId,
        EntityActivityValues.CampaignEntityId,
        EntityActivityValues.CompanyEntityId)]
    public class RetrieveCampaignReportActivity : AppNexusActivity
    {
        /// <summary>Gets the time to wait between retrieve retries</summary>
        private static TimeSpan RetrieveReportRetryWait
        {
            get { return Config.GetTimeSpanValue("AppNexus.RetrieveReportRetryWait"); }
        }

        /// <summary>Gets the time to wait between retrieve retries</summary>
        private static int RetrieveReportMaxRetries
        {
            get { return Config.GetIntValue("AppNexus.RetrieveReportRetries"); }
        }

        /// <summary>Override to handle results of submitted requests.</summary>
        /// <param name="result">The result of the previously submitted work item</param>
        public override void OnActivityResult(ActivityResult result)
        {
            LogManager.Log(
                result.Succeeded ? LogLevels.Trace : LogLevels.Warning,
                "RetrieveCampaignReportActivity: Submitted APNXtoDAHistoryActivity request {0}.",
                result.Succeeded ? "succeeded" : "failed");
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            ActivityResult error = null;
            var context = CreateContext(request);
            var companyEntityId = context.ExternalCompanyId;
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
            var reportId = request.Values[AppNexusActivityValues.ReportId];

            // Get the entities
            var campaignEntity = this.Repository.TryGetEntity(context, campaignEntityId) as CampaignEntity;
            if (campaignEntity == null)
            {
                return EntityNotFoundError(campaignEntityId);
            }

            var companyEntity = this.Repository.TryGetEntity(context, companyEntityId) as CompanyEntity;
            if (companyEntity == null)
            {
                return EntityNotFoundError(companyEntityId);
            }

            using (var client = this.CreateAppNexusClient(context, companyEntity, campaignEntity))
            {
                // Retrieve the report data from AppNexus
                var reportData = this.RetrieveReport(client, reportId, out error);
                if (reportData == null)
                {
                    return error;
                }

                // Save the delivery data and associate it to the campaign
                if (!this.SaveCampaignDeliveryData(context, campaignEntity, reportData, out error))
                {
                    return error;
                }

                // Submit the request to transform the raw delivery data
                if (!this.SubmitTransformRequest(context, campaignEntity, out error))
                {
                    return error;
                }
            }

            // Respond with ready state and report entity id
            return this.SuccessResult(new Dictionary<string, string>
            {
                { AppNexusActivityValues.ReportId, reportId },
                { EntityActivityValues.CampaignEntityId, campaignEntity.ExternalEntityId.ToString() },
                { EntityActivityValues.CompanyEntityId, companyEntity.ExternalEntityId.ToString() }
            });
        }

        /// <summary>Retrieve a report from AppNexus</summary>
        /// <param name="client">AppNexus API client</param>
        /// <param name="reportId">Report id to retrieve</param>
        /// <param name="error">If failed, the error; Otherwise, null.</param>
        /// <returns>The report data, if successful; Otherwise, null.</returns>
        private string RetrieveReport(IAppNexusApiClient client, string reportId, out ActivityResult error)
        {
            error = null;
            try
            {
                var retries = 0;
                while (true)
                {
                    // Try to get the report from AppNexus
                    var reportData = client.RetrieveReport(reportId);
                    if (reportData != null)
                    {
                        return reportData;
                    }

                    // Report isn't ready yet
                    if (++retries > RetrieveReportMaxRetries)
                    {
                        error = ErrorResult(
                            ActivityErrorId.GenericError,
                            "Unable to retrieve report after {0} retries",
                            retries);
                        return null;
                    }

                    Thread.Sleep(RetrieveReportRetryWait);
                }
            }
            catch (AppNexusClientException ance)
            {
                error = AppNexusClientError(ance);
                return null;
            }
        }

        /// <summary>
        /// Saves report data as a blob and associates the blob to the campaign
        /// </summary>
        /// <param name="context">Repository request context</param>
        /// <param name="campaignEntity">The campaign entity</param>
        /// <param name="reportData">The report data</param>
        /// <param name="error">If failed, the error result; Otherwise, null.</param>
        /// <returns>True if successful; Otherwise, false.</returns>
        private bool SaveCampaignDeliveryData(
            RequestContext context,
            CampaignEntity campaignEntity,
            string reportData,
            out ActivityResult error)
        {
            error = null;

            BlobEntity rawDeliveryDataBlob = null;
            try
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Saving {0} characters of report for campaign '{1}' ({2})...",
                    reportData.Length,
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);

                // Save the report data to a new blob entity
                rawDeliveryDataBlob = BlobEntity.BuildBlobEntity(new EntityId(), reportData);
                this.Repository.SaveEntity(context, rawDeliveryDataBlob);
                LogManager.Log(
                    LogLevels.Information,
                    "Report for campaign '{0}' ({1}) saved as blob '{2}'",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    rawDeliveryDataBlob.ExternalEntityId);
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unhandled exception while creating/saving raw delivery data blob: {0}",
                    e);
                throw;
            }

            BlobEntity deliveryDataIndexBlob = null;
            try
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Updating delivery data index for campaign '{0}' ({1})",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);

                // Update delivery data index
                List<string> deliveryDataIndex = null;
                var indexAssociation = campaignEntity.TryGetAssociationByName(AppNexusEntityProperties.AppNexusRawDeliveryDataIndex);
                if (indexAssociation == null)
                {
                    LogManager.Log(
                        LogLevels.Trace,
                        "No delivery data index found for campaign '{0}' ({1}). Creating a new one.",
                        campaignEntity.ExternalName,
                        campaignEntity.ExternalEntityId);
                    deliveryDataIndex = new List<string>();
                }
                else
                {
                    var previousIndexBlob = this.Repository.GetEntity(context, indexAssociation.TargetEntityId) as BlobEntity;
                    deliveryDataIndex = previousIndexBlob.DeserializeBlob<List<string>>();
                    LogManager.Log(
                        LogLevels.Trace,
                        "Loaded saved delivery data index ({0}) for campaign '{1}' ({2}) for updating.",
                        previousIndexBlob.ExternalEntityId,
                        campaignEntity.ExternalName,
                        campaignEntity.ExternalEntityId);
                }

                deliveryDataIndex.Add(rawDeliveryDataBlob.ExternalEntityId.ToString());
                deliveryDataIndexBlob = BlobEntity.BuildBlobEntity<List<string>>(new EntityId(), deliveryDataIndex);

                LogManager.Log(
                    LogLevels.Trace,
                    "Saving delivery data index blob for campaign '{0}' ({1}) for updating.",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);
                this.Repository.SaveEntity(context, deliveryDataIndexBlob);

                LogManager.Log(
                    LogLevels.Trace,
                    "Saved raw delivery data blob ({0}) for campaign '{1}' ({2}) for updating.",
                    rawDeliveryDataBlob.ExternalEntityId,
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unhandled exception while updating delivery data index: {0}",
                    e);
                throw;
            }

            // Associate the updated index with the campaign, replacing the
            // currently associated delivery data index blob (if any exists)
            try
            {
                campaignEntity.AssociateEntities(
                    AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                    string.Empty,
                    new HashSet<IEntity> { deliveryDataIndexBlob },
                    AssociationType.Relationship,
                    true);
                var updateContext = context.BuildContextWithNameFilters(
                    new List<string>(), new List<string> { AppNexusEntityProperties.AppNexusRawDeliveryDataIndex });
                this.Repository.ForceUpdateEntity(updateContext, campaignEntity);
            }
            catch (DataAccessException dae)
            {
                error = ErrorResult(
                    ActivityErrorId.DataAccess,
                    "Unable to associate updated delivery data index blob '{0}' with campaign '{1}' ({2}): {3}",
                    rawDeliveryDataBlob.ExternalEntityId,
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    dae);
                return false;
            }

            return true;
        }

        /// <summary>Submits an activity request to transform the raw delivery data for a campaign</summary>
        /// <param name="context">Repository request context</param>
        /// <param name="campaignEntity">The campaign entity</param>
        /// <param name="error">If failed, the error result; Otherwise, null.</param>
        /// <returns>True if successful; Otherwise, false.</returns>
        private bool SubmitTransformRequest(
            RequestContext context,
            CampaignEntity campaignEntity,
            out ActivityResult error)
        {
            error = null;

            // Submit request to transform as needed
            // Create and submit an APNXtoDAHistoryActivity
            var activityRequest = CreateRequestFromContext(
                context,
                DynamicAllocationActivityTasks.GetCampaignDeliveryData,
                new Dictionary<string, string>
                {
                    { EntityActivityValues.CampaignEntityId, campaignEntity.ExternalEntityId.ToString() }
                });

            if (!this.SubmitRequest(activityRequest, true))
            {
                error = ErrorResult(
                    ActivityErrorId.GenericError,
                    "Unable to submit activity request to transform raw delivery data for campaign '{0}' ({1})",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);
                return false;
            }

            return true;
        }
    }
}
