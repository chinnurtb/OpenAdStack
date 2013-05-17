// -----------------------------------------------------------------------
// <copyright file="ChargeBillingAccountActivityFixture.cs" company="Rare Crowds Inc">
//    Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Activities;
using ActivityTestUtilities;
using BillingActivities;
using DataAccessLayer;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace BillingUnitTests
{
    /// <summary>Unit-test fixture for ChargeBillingAccountActivity</summary>
    [TestClass]
    public class ChargeBillingAccountActivityFixture
    {
        /// <summary>Company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>Campaign id for testing</summary>
        private EntityId campaignEntityId;

        /// <summary>Authorization user id for testing</summary>
        private string authUserId;

        /// <summary>Activity request for testing</summary>
        private ActivityRequest activityRequest;

        /// <summary>Stubbed activity handler for testing.</summary>
        private IActivityHandler handler;

        /// <summary>Stubbed activity handler factory for testing.</summary>
        private IActivityHandlerFactory handlerFactory;

        /// <summary>Activity for testing.</summary>
        private ChargeBillingAccountActivity activity;

        /// <summary>Charge amount for testing</summary>
        private string chargeAmount;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.authUserId = "userfoo";
            this.chargeAmount = "10.00";

            // load all fields
            this.activityRequest = new ActivityRequest();
            this.activityRequest.Values.Add(EntityActivityValues.EntityId, this.companyEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.CampaignEntityId, this.campaignEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.AuthUserId, this.authUserId);
            this.activityRequest.Values.Add(EntityActivityValues.ChargeAmount, this.chargeAmount);

            this.handler = MockRepository.GenerateStub<IActivityHandler>();
            this.handlerFactory = MockRepository.GenerateStub<IActivityHandlerFactory>();

            // Set up our activity
            // Inject our stubbed handler factory instead of the real one
            this.activity = Activity.CreateActivity(
                    this.handlerFactory,
                    typeof(ChargeBillingAccountActivity),
                    new Dictionary<Type, object>(),
                    ActivityTestHelpers.SubmitActivityRequest) as ChargeBillingAccountActivity;
        }

        /// <summary>Happy path process request.</summary>
        [TestMethod]
        public void ProcessRequestSuccess()
        {
            this.handler.Stub(f => f.Execute()).Return(new Dictionary<string, string>());
            this.handlerFactory.Stub(f => f.CreateActivityHandler(
                    Arg<ActivityRequest>.Is.Same(this.activityRequest),
                    Arg<Dictionary<Type, object>>.Is.Anything)).Return(this.handler);
            var result = this.activity.Run(this.activityRequest);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidSuccessResult(result);
        }

        /// <summary>Process request returns error when handler factory throws.</summary>
        [TestMethod]
        public void ProcessRequestFactoryException()
        {
            this.handler.Stub(f => f.Execute()).Return(new Dictionary<string, string>());
            this.handlerFactory.Stub(f => f.CreateActivityHandler(null, null))
                .IgnoreArguments().Throw(new ArgumentException("error message"));
            var result = this.activity.Run(this.activityRequest);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidErrorResult(result, ActivityErrorId.GenericError, "error message");
        }

        /// <summary>Process request returns error when handler throws.</summary>
        [TestMethod]
        public void ProcessRequestHandlerException()
        {
            this.handler.Stub(f => f.Execute()).Throw(new ActivityException(ActivityErrorId.UserAccessDenied, "error message"));
            this.handlerFactory.Stub(f => f.CreateActivityHandler(null, null))
                .IgnoreArguments().Return(this.handler);
            var result = this.activity.Run(this.activityRequest);

            // Assert activity completed successfully
            ActivityTestHelpers.AssertValidErrorResult(result, ActivityErrorId.UserAccessDenied, "error message");
        }
    }
}
