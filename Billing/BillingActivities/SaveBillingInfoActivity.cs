// -----------------------------------------------------------------------
// <copyright file="SaveBillingInfoActivity.cs" company="Rare Crowds Inc">
//    Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Activities;
using Diagnostics;
using EntityActivities;
using EntityUtilities;

namespace BillingActivities
{
    /// <summary>Activity for saving customer billing information to a company.</summary>
    /// <remarks>
    /// RequiredValues
    ///   EntityId - ExternalEntityId of the company
    ///   AuthUserId - Authorization User Id
    ///   MessagePayload - Api request payload
    /// </remarks>
    [Name(EntityActivityTasks.SaveBillingInfo)]
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.AuthUserId, EntityActivityValues.MessagePayload)]
    public class SaveBillingInfoActivity : EntityActivity
    {
        /// <summary>The activity handler factory.</summary>
        private IActivityHandlerFactory activityHandlerFactory = new BillingActivityHandlerFactory();

        /// <summary>Gets the handler factory override.</summary>
        protected override IActivityHandlerFactory ActivityHandlerFactory
        {
            get { return this.activityHandlerFactory; }
            set { this.activityHandlerFactory = value; }
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            try
            {
                var activityHandler = this.ActivityHandlerFactory.CreateActivityHandler(request, this.Context);
                var results = activityHandler.Execute();
                return this.SuccessResult(results.ToDictionary());
            }
            catch (ActivityException ex)
            {
                LogManager.Log(LogLevels.Error, ex.ToString());
                return this.ErrorResult(ex.ActivityErrorId, ex);
            }
            catch (Exception ex)
            {
                LogManager.Log(LogLevels.Error, ex.ToString());
                return this.ErrorResult(ActivityErrorId.GenericError, ex);
            }
        }
    }
}
