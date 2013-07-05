// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BillingActivityHandlerFactoryFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using Activities;
using BillingActivities;
using DataAccessLayer;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaymentProcessor;
using Rhino.Mocks;
using Utilities;

namespace BillingUnitTests
{
    /// <summary>
    /// Unit test fixture for BillingActivityHandlerFactoryFixture
    /// </summary>
    [TestClass]
    public class BillingActivityHandlerFactoryFixture
    {
        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>IUserAccessRepository for testing</summary>
        private IUserAccessRepository accessRepository;

        /// <summary>IPaymentProcessor for testing</summary>
        private IPaymentProcessor paymentProcessor;

        /// <summary>Company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>Authorization user id for testing</summary>
        private string authUserId;

        /// <summary>Activity request for testing</summary>
        private ActivityRequest activityRequest;

        /// <summary>Activity context for testing</summary>
        private Dictionary<Type, object> activityContext;

        /// <summary>api request payload</summary>
        private string messagePayload;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyEntityId = new EntityId();
            this.authUserId = "userfoo";
            this.messagePayload = "somestuff";
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.accessRepository = MockRepository.GenerateStub<IUserAccessRepository>();
            this.paymentProcessor = MockRepository.GenerateStub<IPaymentProcessor>();

            this.activityRequest = new ActivityRequest();
            this.activityRequest.Values.Add(EntityActivityValues.EntityId, this.companyEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.AuthUserId, this.authUserId);

            this.activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.repository },
                    { typeof(IUserAccessRepository), this.accessRepository },
                    { typeof(IPaymentProcessor), this.paymentProcessor },
                };
        }

        /// <summary>Happy path create SaveBillingInfoHandler.</summary>
        [TestMethod]
        public void CreateSaveBillingInfoHandlerSuccess()
        {
            this.activityRequest.Task = EntityActivityTasks.SaveBillingInfo;
            this.activityRequest.Values.Add(EntityActivityValues.MessagePayload, this.messagePayload);

            var factory = new BillingActivityHandlerFactory();
            var handler = (SaveBillingInfoHandler)factory.CreateActivityHandler(this.activityRequest, this.activityContext);
            Assert.IsNotNull(handler);
            Assert.IsInstanceOfType(handler, typeof(SaveBillingInfoHandler));
            Assert.AreSame(this.repository, handler.Repository);
            Assert.IsNotNull(handler.AccessHandler);
            Assert.AreSame(this.paymentProcessor, handler.PaymentProcessor);
            Assert.AreEqual(this.companyEntityId, handler.CompanyEntityId);
            Assert.AreEqual(this.authUserId, handler.AuthUserId);
            Assert.AreEqual(this.messagePayload, handler.MessagePayload);
        }

        /// <summary>Happy path create ChargeBillingAccountHandler.</summary>
        [TestMethod]
        public void CreateChargeBillingAccountHandlerSuccess()
        {
            this.activityRequest.Task = EntityActivityTasks.ChargeBillingAccount;
            var campaignEntityId = new EntityId();
            this.activityRequest.Values.Add(EntityActivityValues.CampaignEntityId, campaignEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.ChargeAmount, "10.00");

            var factory = new BillingActivityHandlerFactory();
            var handler = (ChargeBillingAccountHandler)factory.CreateActivityHandler(this.activityRequest, this.activityContext);
            Assert.IsNotNull(handler);
            Assert.IsInstanceOfType(handler, typeof(ChargeBillingAccountHandler));
            Assert.AreSame(this.repository, handler.Repository);
            Assert.IsNotNull(handler.AccessHandler);
            Assert.AreSame(this.paymentProcessor, handler.PaymentProcessor);
            Assert.AreEqual(this.companyEntityId, handler.CompanyEntityId);
            Assert.AreEqual(this.authUserId, handler.AuthUserId);
            Assert.AreEqual(campaignEntityId, handler.CampaignEntityId);
            Assert.AreEqual(null, handler.ChargeId);
            Assert.IsFalse(handler.IsCheck);
        }

        /// <summary>Charge id handled.</summary>
        [TestMethod]
        public void CreateChargeBillingAccountHandlerWithChargeId()
        {
            this.activityRequest.Task = EntityActivityTasks.ChargeBillingAccount;
            var campaignEntityId = new EntityId();
            this.activityRequest.Values.Add(EntityActivityValues.CampaignEntityId, campaignEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.ChargeAmount, "10.00");
            this.activityRequest.Values.Add(EntityActivityValues.ChargeId, "uid");

            var factory = new BillingActivityHandlerFactory();
            var handler = (ChargeBillingAccountHandler)factory.CreateActivityHandler(this.activityRequest, this.activityContext);
            Assert.AreEqual("uid", handler.ChargeId);
        }

        /// <summary>Charge check handled.</summary>
        [TestMethod]
        public void CreateChargeBillingAccountHandlerWithCheck()
        {
            this.activityRequest.Task = EntityActivityTasks.ChargeBillingAccount;
            var campaignEntityId = new EntityId();
            this.activityRequest.Values.Add(EntityActivityValues.CampaignEntityId, campaignEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.ChargeAmount, "10.00");
            this.activityRequest.Values.Add(EntityActivityValues.IsChargeCheck, true.ToString());

            var factory = new BillingActivityHandlerFactory();
            var handler = (ChargeBillingAccountHandler)factory.CreateActivityHandler(this.activityRequest, this.activityContext);
            Assert.IsTrue(handler.IsCheck);
        }

        /// <summary>Non-parsable charge amount throws.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateChargeBillingAccountHandlerInvalidChargeAmount()
        {
            this.activityRequest.Task = EntityActivityTasks.ChargeBillingAccount;
            var campaignEntityId = new EntityId();
            this.activityRequest.Values.Add(EntityActivityValues.CampaignEntityId, campaignEntityId);
            this.activityRequest.Values.Add(EntityActivityValues.ChargeAmount, "nan");

            var factory = new BillingActivityHandlerFactory();
            factory.CreateActivityHandler(this.activityRequest, this.activityContext);
        }

        /// <summary>Throw if task not recognized.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateActivityHandlerInvalidTask()
        {
            this.activityRequest.Task = "NotTheOne";

            var factory = new BillingActivityHandlerFactory();
            factory.CreateActivityHandler(this.activityRequest, this.activityContext);
        }
    }
}
