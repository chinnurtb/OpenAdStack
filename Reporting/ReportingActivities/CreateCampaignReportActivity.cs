// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateCampaignReportActivity.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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
