//-----------------------------------------------------------------------
// <copyright file="GetSegmentDataCostCsv.cs" company="Rare Crowds Inc">
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
