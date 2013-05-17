// -----------------------------------------------------------------------
// <copyright file="ChargeBillingAccountActivity.cs" company="Rare Crowds Inc">
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
    /// <summary>Activity for charging a customer billing account.</summary>
    /// <remarks>
    /// RequiredValues
    ///   EntityId - ExternalEntityId of the company
    ///   CampaignEntityId = ExternalEntityId of the campaign
    ///   AuthUserId - Authorization User Id
    ///   ChargeAmount - String representation of decimal amount to charge.
    /// OptionalValues
    ///   ChargeId - For refunds.
    ///   IsChargeCheck - If present perform a charge check (but not an actual charge)
    /// </remarks>
    [Name(EntityActivityTasks.ChargeBillingAccount)]
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.CampaignEntityId, EntityActivityValues.AuthUserId, EntityActivityValues.ChargeAmount)]
    public class ChargeBillingAccountActivity : EntityActivity
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
