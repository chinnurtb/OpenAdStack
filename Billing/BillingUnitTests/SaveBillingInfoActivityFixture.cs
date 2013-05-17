// -----------------------------------------------------------------------
// <copyright file="SaveBillingInfoActivityFixture.cs" company="Rare Crowds Inc">
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
    /// <summary>Unit-test fixture for SaveBillingInfoActivity</summary>
    [TestClass]
    public class SaveBillingInfoActivityFixture
    {
        /// <summary>Company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>Authorization user id for testing</summary>
        private string authUserId;

        /// <summary>Activity request for testing</summary>
        private ActivityRequest activityRequest;

        /// <summary>api request payload</summary>
        private string messagePayload;

        /// <summary>Stubbed activity handler for testing.</summary>
        private IActivityHandler handler;

        /// <summary>Stubbed activity handler factory for testing.</summary>
        private IActivityHandlerFactory handlerFactory;

        /// <summary>Activity for testing.</summary>
        private SaveBillingInfoActivity activity;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyEntityId = new EntityId();
            this.authUserId = "userfoo";
            this.messagePayload = "somestuff";

            // load all fields
            this.activityRequest = new ActivityRequest();
            this.activityRequest.Values.Add(EntityActivityValues.EntityId, this.companyEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.AuthUserId, this.authUserId);
            this.activityRequest.Values.Add(EntityActivityValues.MessagePayload, this.messagePayload);

            this.handler = MockRepository.GenerateStub<IActivityHandler>();
            this.handlerFactory = MockRepository.GenerateStub<IActivityHandlerFactory>();

            // Set up our activity
            // Inject our stubbed handler factory instead of the real one
            this.activity = Activity.CreateActivity(
                    this.handlerFactory,
                    typeof(SaveBillingInfoActivity),
                    new Dictionary<Type, object>(),
                    ActivityTestHelpers.SubmitActivityRequest) as SaveBillingInfoActivity;
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
