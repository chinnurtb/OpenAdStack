//-----------------------------------------------------------------------
// <copyright file="GetSegmentDataCostCsv.cs" company="Rare Crowds Inc">
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
using EntityActivities;
using EntityUtilities;

namespace AppNexusActivities.AppActivities
{
    /// <summary>
    /// Activity for getting AppNexus App users' advertisers
    /// </summary>
    /// <remarks>
    /// Retrieves users' advertisers from AppNexus
    /// RequiredValues:
    ///   AuthUserId - The user's user id (used to call AppNexus APIs)
    /// </remarks>
    [Name(AppNexusActivityTasks.GetDataCostCsv)]
    [RequiredValues(EntityActivityValues.AuthUserId)]
    public class GetSegmentDataCostCsv : AppNexusActivity
    {
        /// <summary>Gets the activity's runtime category</summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.InteractiveFetch; }
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessAppNexusRequest(ActivityRequest request)
        {
            // Check that user is an AppNexusApp user
            var context = CreateRepositoryContext(RepositoryContextType.InternalEntityGet, request);
            var userId = request.Values[EntityActivityValues.AuthUserId];
            var user = this.Repository.GetUser(context, userId);
            if (user.GetUserType() != UserType.AppNexusApp)
            {
                return ErrorResult(ActivityErrorId.GenericError, "Activity not supported for non-AppNexusApp users");
            }

            // Get segment measures for the user's member and return as a CSV template
            var config = new ConfigManager.CustomConfig();
            var segmentMeasureSource = new Measures.SegmentMeasureSource(user, config);
            var dataCostCsv = segmentMeasureSource.CreateSegmentDataCostCsvTemplate(false);
            return this.SuccessResult(new Dictionary<string, string>
            {
                { AppNexusActivityValues.DataCostCsv, dataCostCsv }
            });
        }
    }
}
