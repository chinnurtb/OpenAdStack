//-----------------------------------------------------------------------
// <copyright file="RequestCampaignReportActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;

namespace AppNexusActivities
{
    /// <summary>
    /// Activity for requesting campaign reports for a CampaignEntity.
    /// </summary>
    /// <remarks>
    /// Associates different entities.
    /// RequiredValues:
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    /// ResultValues:
    ///   ReportId - The ID of the requested AppNexus report (used for retrieval)
    ///   LineItemId - The ID of the line item the report was requested for
    ///   CampaignEntityId - The EntityId of the CampaignEntity
    ///   CompanyEntityId - The EntityId of the CompanyEntity
    ///   Reschedule - Whether to schedule another request after this one
    /// </remarks>
    [Name(AppNexusActivityTasks.RequestCampaignReport)]
    [RequiredValues(
        EntityActivityValues.CompanyEntityId,
        EntityActivityValues.CampaignEntityId)]
    [ResultValues(
        AppNexusActivityValues.ReportId,
        AppNexusActivityValues.LineItemId,
        EntityActivityValues.CampaignEntityId,
        EntityActivityValues.CompanyEntityId,
        AppNexusActivityValues.Reschedule)]
    public class RequestCampaignReportActivity : AppNexusActivity
    {
        /// <summary>Gets the system auth user id</summary>
        private static TimeSpan PostEndDateReportingPeriod
        {
            get { return Config.GetTimeSpanValue("AppNexus.PostEndDateReportPeriod"); }
        }

        /// <summary>Gets the time between AppNexus delivery report requests</summary>
        private static TimeSpan ReportRequestFrequency
        {
            get { return Config.GetTimeSpanValue("Delivery.ReportFrequency"); }
        }

        /// <summary>Handle the result</summary>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityResult result)
        {
            if (!result.Succeeded)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "RequestCampaignReportActivity - Error in {0}: {1} (WorkItem: {2})",
                    result.Task,
                    result.Error.Message,
                    result.RequestId);
            }
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            // Get the entities
            var context = CreateContext(request);
            CampaignEntity campaignEntity = null;
            CompanyEntity companyEntity = null;
            try
            {
                var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
                var advertiserEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
                
                companyEntity = this.Repository.TryGetEntity(context, advertiserEntityId) as CompanyEntity;
                if (companyEntity == null)
                {
                    return EntityNotFoundError(advertiserEntityId);
                }

                campaignEntity = this.Repository.TryGetEntity(context, campaignEntityId) as CampaignEntity;
                if (campaignEntity == null)
                {
                    return EntityNotFoundError(campaignEntityId);
                }
            }
            catch (ArgumentException ae)
            {
                return ErrorResult(ActivityErrorId.GenericError, ae);
            }

            // Get the AppNexus ids
            int? advertiserId = companyEntity.GetAppNexusAdvertiserId();
            if (advertiserId == null)
            {
                return ErrorResult(ActivityErrorId.GenericError, "Company '{0}' does not have an AppNexus advertiser id.", companyEntity.ExternalEntityId);
            }

            int? lineItemId = campaignEntity.GetAppNexusLineItemId();
            if (lineItemId == null)
            {
                return ErrorResult(ActivityErrorId.GenericError, "Campaign '{0}' does not have an AppNexus line item id.", campaignEntity.ExternalEntityId);
            }

            using (var client = this.CreateAppNexusClient(context, companyEntity, campaignEntity))
            {
                // Request the report
                var reportId = client.RequestDeliveryReport((int)advertiserId, (int)lineItemId);

                // Determine if additional retrieve requests should be scheduled
                var nextScheduledTime = DateTime.UtcNow + ReportRequestFrequency;
                var reschedule = nextScheduledTime < (DateTime)campaignEntity.EndDate + PostEndDateReportingPeriod;
                LogManager.Log(
                    LogLevels.Trace,
                    "Flagging campaign '{0}' ({1}) should {2} re-scheduled. (EndDate: {3}, Next Scheduled Time: {4}, Post-EndDate Reporting Period: {5}",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    reschedule ? "be" : "NOT be",
                    campaignEntity.EndDate,
                    nextScheduledTime,
                    PostEndDateReportingPeriod);

                return this.SuccessResult(new Dictionary<string, string>
                {
                    { EntityActivityValues.CompanyEntityId, request.Values[EntityActivityValues.CompanyEntityId] },
                    { EntityActivityValues.CampaignEntityId, request.Values[EntityActivityValues.CampaignEntityId] },
                    { AppNexusActivityValues.ReportId, reportId },
                    { AppNexusActivityValues.LineItemId, ((int)lineItemId).ToString(CultureInfo.InvariantCulture) },
                    { AppNexusActivityValues.Reschedule, reschedule.ToString() }
                });
            }
        }
    }
}
