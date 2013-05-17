// -----------------------------------------------------------------------
// <copyright file="ReallocateNowActivity.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Activities;
using AppNexusUtilities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocationUtilities;
using EntityUtilities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Activity for performing immediate budget reallocations
    /// </summary>
    /// <remarks>
    /// Gets budget allocations for the provided inputs
    /// RequiredValues:
    ///   CompanyEntityId - 
    ///   CampaignEntityId - 
    /// ResultValues:
    ///   SuccessResult - 
    /// </remarks>
    [Name(AppNexusActivityTasks.ReallocateNow)]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CampaignEntityId)]
    [ResultValues(EntityActivityValues.CampaignEntityId)]
    public class ReallocateNowActivity : AppNexusActivity
    {
        /// <summary>Tracking dictionary</summary>
        private IPersistentDictionary<TrackingInfo> tracking;

        /// <summary>
        /// Enum to define the states for submitted activities
        /// </summary>
        internal enum AppNexusActivitySubmitState
        {
            /// <summary>
            /// The Submitted enum value
            /// </summary>
            //// [DescriptionAttribute("Submitted")]
            Submitted,

            /// <summary>
            /// The Completed enum value
            /// </summary>
            //// [DescriptionAttribute("Completed")]
            Completed,

            /// <summary>
            /// The Failed enum value
            /// </summary>
            ////[DescriptionAttribute("Failed")]
            Failed
        }

        /// <summary>
        /// Gets the TrackingInfo dictionary
        /// </summary>
        internal IPersistentDictionary<TrackingInfo> Tracking
        {
            get
            {
                this.tracking = this.tracking ?? PersistentDictionaryFactory.CreateDictionary<TrackingInfo>("immediaterealloc-tracking");
                return this.tracking;
            }
        }

        /// <summary>
        /// On Activity Result
        /// </summary>
        /// <param name="result">Activity Result</param>
        public override void OnActivityResult(ActivityResult result)
        {
            if (result.Succeeded)
            {
                var trackingKey = this.Tracking.Where(kvp => kvp.Value.WorkItemDictionary.ContainsKey(result.RequestId)).Select(kvp => kvp.Key).Single();
                var trackingEntry = this.Tracking[trackingKey];

                switch (result.Task)
                {
                    case AppNexusActivityTasks.RequestCampaignReport:
                        this.HandleRequestReportActivityResult(result, trackingKey, trackingEntry);
                        break;

                    case AppNexusActivityTasks.RetrieveCampaignReport:
                        this.HandleRetrieveReportActivityResult(result, trackingKey, trackingEntry);
                        break;

                    case DynamicAllocationActivityTasks.GetBudgetAllocations:
                        LogManager.Log(
                            LogLevels.Information,
                            "GetBudgetAllocations successful for campaign {0}, line item {1}",
                            result.Values[EntityActivityValues.CampaignEntityId],
                            result.Values[AppNexusActivityValues.LineItemId]);

                        // update the dictionary
                        this.UpdateDictionaryFromResult(result, trackingKey, trackingEntry, Enum.GetName(typeof(AppNexusActivitySubmitState), AppNexusActivitySubmitState.Completed));

                        break;
                }
            }
            else
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Error processing '{0}' activity: {1}",
                    result.Task,
                    result.Error.Message);
            }
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            // get campaign
            var context = CreateContext(request);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
                 
            var entities = this.Repository.GetEntitiesById(context, new EntityId[] { campaignEntityId });
            var campaignEntity = entities.Single(e => (string)e.EntityCategory == CampaignEntity.CampaignEntityCategory);

            // make sure there is a line item id for this campaign
            int? lineItemId = ((CampaignEntity)campaignEntity).GetAppNexusLineItemId();
            if (lineItemId == null)
            {
                return ErrorResult(ActivityErrorId.GenericError, "Campaign '{0}' does not have an AppNexus line item id.", campaignEntity.ExternalEntityId);
            }
 
            // Request a campaign report
            var requestCampaignReportActivity = CreateRequestFromContext(
                context,
                AppNexusActivityTasks.RequestCampaignReport,
                new Dictionary<string, string>
                {
                        { EntityActivityValues.CampaignEntityId, campaignEntityId.ToString() }
                });
           
            var trackingKey = "{0}_{1}"
                .FormatInvariant(
                    campaignEntity.ExternalEntityId,
                    DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            var trackingInfo = new TrackingInfo(
                campaignEntityId.ToString(), campaignEntity.ExternalName, (int)lineItemId);
            if (!this.SubmitRequest(requestCampaignReportActivity, true))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "failed to submit APXS Campaign report request for campaign id:{0}",
                    campaignEntityId);
                LogManager.Log(LogLevels.Error, message); 

                trackingInfo.WorkItemDictionary[requestCampaignReportActivity.Id] = "{0}|{1}"
                    .FormatInvariant(
                        requestCampaignReportActivity.Task,
                        AppNexusActivitySubmitState.Failed);
                this.Tracking[trackingKey] = trackingInfo;

                return this.ErrorResult(ActivityErrorId.GenericError, message);
            }

            trackingInfo.WorkItemDictionary[requestCampaignReportActivity.Id] = "{0}|{1}"
                .FormatInvariant(
                    requestCampaignReportActivity.Task,
                    AppNexusActivitySubmitState.Submitted);
            this.Tracking[trackingKey] = trackingInfo;

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.CampaignEntityId, campaignEntityId.ToString() }
            });
        }

        /// <summary> Update the dictionary with the result of activity submission </summary>
        /// <param name="result">the Activity result </param>
        /// <param name="trackingKey">The tracking Key.</param>
        /// <param name="trackingEntry">The tracking Entry.</param>
        /// <param name="dictionaryValue">Value to set in dictionary</param>
        private void UpdateDictionaryFromResult(ActivityResult result, string trackingKey, TrackingInfo trackingEntry, string dictionaryValue)
        {
            trackingEntry.ReportId = result.Values[AppNexusActivityValues.ReportId];
            trackingEntry.WorkItemDictionary[result.RequestId] = "{0}|{1}".FormatInvariant(result.Task, dictionaryValue);
            this.Tracking[trackingKey] = trackingEntry;
        }

        /// <summary>
        /// Handles the RequestReport activity result
        /// </summary>
        /// <param name="result">the Activity result</param>
        /// <param name="trackingKey">the tracking key</param>
        /// <param name="trackingEntry">the tracking entry</param>
        private void HandleRequestReportActivityResult(ActivityResult result, string trackingKey, TrackingInfo trackingEntry)
        {
            LogManager.Log(
                LogLevels.Information,
                "Request campaign report successful for campaign {0}, line item {1}",
                result.Values[EntityActivityValues.CampaignEntityId],
                result.Values[AppNexusActivityValues.LineItemId]);

            // update the dictionary
            this.UpdateDictionaryFromResult(result, trackingKey, trackingEntry, Enum.GetName(typeof(AppNexusActivitySubmitState), AppNexusActivitySubmitState.Completed));

            // submit a Retrieve Report activity
            var request = CreateRequestFromResult(
                result,
                AppNexusActivityTasks.RetrieveCampaignReport,
                new Dictionary<string, string>
                {
                    { EntityActivityValues.CampaignEntityId, result.Values[EntityActivityValues.CampaignEntityId] },
                    { AppNexusActivityValues.ReportId, result.Values[AppNexusActivityValues.ReportId] }
                });

            if (!this.SubmitRequest(request, true))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "failed to submit APXS Retreive Campaign report request for campaign id:{0}, report id: {1}",
                    result.Values[EntityActivityValues.CampaignEntityId],
                    result.Values[AppNexusActivityValues.ReportId]);

                trackingEntry.WorkItemDictionary[request.Id] = "{0}|{1}".FormatInvariant(request.Task, AppNexusActivitySubmitState.Failed);
            }
            else
            {
                trackingEntry.WorkItemDictionary[request.Id] = "{0}|{1}".FormatInvariant(request.Task, AppNexusActivitySubmitState.Submitted);
            }

            this.Tracking[trackingKey] = trackingEntry;
        }

        /// <summary>
        /// Handles the RetrieveReport activity result
        /// </summary>
        /// <param name="result">the Activity result</param>
        /// <param name="trackingKey">the tracking key</param>
        /// <param name="trackingEntry">the tracking entry</param>
        private void HandleRetrieveReportActivityResult(ActivityResult result, string trackingKey, TrackingInfo trackingEntry)
        {
            LogManager.Log(
               LogLevels.Information,
               "Retrieve campaign report successful for campaign {0}, line item {1}",
               result.Values[EntityActivityValues.CampaignEntityId],
               result.Values[AppNexusActivityValues.LineItemId]);

            // update the dictionary
            this.UpdateDictionaryFromResult(result, trackingKey, trackingEntry, Enum.GetName(typeof(AppNexusActivitySubmitState), AppNexusActivitySubmitState.Completed));

            var budgetAllocationsActivity = CreateRequestFromResult(
                result,
                DynamicAllocationActivityTasks.GetBudgetAllocations,
                new Dictionary<string, string>
                {
                    { EntityActivityValues.CampaignEntityId, result.Values[EntityActivityValues.CampaignEntityId] },
                    { DynamicAllocationActivityValues.AllocationStartDate, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) }
                });

            if (!this.SubmitRequest(budgetAllocationsActivity, true))
            {
                LogManager.Log(
                    LogLevels.Error,
                    "failed to submit GetBudgetAllocations request for campaign id:{0}, report id: {1}",
                    result.Values[EntityActivityValues.CampaignEntityId],
                    result.Values[AppNexusActivityValues.ReportId]);

                trackingEntry.WorkItemDictionary[budgetAllocationsActivity.Id] = "{0}|{1}".FormatInvariant(budgetAllocationsActivity.Task, AppNexusActivitySubmitState.Failed);
            }
            else
            {
                trackingEntry.WorkItemDictionary[budgetAllocationsActivity.Id] = "{0}|{1}".FormatInvariant(budgetAllocationsActivity.Task, AppNexusActivitySubmitState.Submitted);
            }

            this.Tracking[trackingKey] = trackingEntry;
        }

        /// <summary>
        /// Class definition for the item stored in the persistent dictionary 
        /// </summary>
        [DataContract]
        internal class TrackingInfo
        {
            /// <summary>
            /// Initializes a new instance of the TrackingInfo class.
            /// </summary>
            /// <param name="campaignEntityId">Campaign Entity Id</param>
            /// <param name="campaignName">Campaign Name</param>
            /// <param name="lineItemId">LineItem Id</param>
            public TrackingInfo(string campaignEntityId, string campaignName, int lineItemId)
            {
                this.CampaignEntityId = campaignEntityId;
                this.CampaignName = campaignName;
                this.LineItemId = lineItemId;
                this.ReportId = string.Empty;  // ReportId not known until OnActivityResult
                this.WorkItemDictionary = new Dictionary<string, string>();
           }

            /// <summary>
            /// Gets or sets the campaign entity id
            /// </summary>
            [DataMember]
            public string CampaignEntityId { get; set; }

            /// <summary>
            /// Gets or sets the campaign name
            /// </summary>
            [DataMember]
            public string CampaignName { get; set; }

            /// <summary>
            /// Gets or sets the line item id
            /// </summary>
            public int LineItemId { get; set; }

            /// <summary>
            /// Gets or sets the report id
            /// </summary>
            [DataMember]
            public string ReportId { get; set; }

            /// <summary>
            /// Gets or sets the workitem dictionary
            /// Key is WorkItem Id, value is Activity Task name and status, using pipe symbol (|) as separator
            /// </summary>
            [DataMember]
            public Dictionary<string, string> WorkItemDictionary { get; set; }
        }
    }
}
