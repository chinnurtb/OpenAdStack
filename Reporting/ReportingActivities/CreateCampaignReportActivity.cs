// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateCampaignReportActivity.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using Activities;
using Diagnostics;
using EntityActivities;
using EntityUtilities;

namespace ReportingActivities
{
    /// <summary>
    /// Activity to create a new campaign report.
    /// </summary>
    [Name("CreateCampaignReportActivity")]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CampaignEntityId, "ReportType")]
    public class CreateCampaignReportActivity : EntityActivity
    {
        /// <summary>The activity handler factory.</summary>
        private IActivityHandlerFactory activityHandlerFactory = new CreateCampaignReportHandlerFactory();

        /// <summary>Gets the handler factory override.</summary>
        protected override IActivityHandlerFactory ActivityHandlerFactory
        {
            get { return this.activityHandlerFactory; }
            set { this.activityHandlerFactory = value; }
        }

        /// <summary>Process the activity request.</summary>
        /// <param name="request">The activity request parameters.</param>
        /// <returns>The result of the activity.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "There is nothing that should not be handled at this point.")]
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            try
            {
                var activityHandler = this.ActivityHandlerFactory.CreateActivityHandler(request, this.Context);
                var results = activityHandler.Execute();
                return this.SuccessResult(results.ToDictionary());
            }
            catch (Exception e)
            {
                LogManager.Log(LogLevels.Error, e.ToString());
                return this.ErrorResult(ActivityErrorId.GenericError, e);
            }
        }
    }
}
