// -----------------------------------------------------------------------
// <copyright file="ChargeBillingAccountHandlerFixture.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Activities;
using BillingActivities;
using DataAccessLayer;
using Diagnostics;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaymentProcessor;
using ResourceAccess;
using Rhino.Mocks;
using Utilities;
using Utilities.Serialization;

namespace BillingUnitTests
{
    /// <summary>Unit-test fixture for ChargeBillingAccountHandler</summary>
    [TestClass]
    public class ChargeBillingAccountHandlerFixture
    {
        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>IResourceAccessHandler for testing</summary>
        private IResourceAccessHandler accessHandler;

        /// <summary>IPaymentProcessor for testing</summary>
        private IPaymentProcessor paymentProcessor;

        /// <summary>Company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>Campaign id for testing</summary>
        private EntityId campaignEntityId;

        /// <summary>Company entity for testing</summary>
        private CompanyEntity companyEntity;

        /// <summary>Campaign entity for testing</summary>
        private CampaignEntity campaignEntity;

        /// <summary>Authorization user id for testing</summary>
        private string authUserId;

        /// <summary>EntityId for testing.</summary>
        private EntityId userEntityId;

        /// <summary>UserEntity for testing.</summary>
        private UserEntity userEntity;

        /// <summary>Customer billing id for testing.</summary>
        private string billingCustomerId;

        /// <summary>Charge id for testing.</summary>
        private string chargeId;

        /// <summary>Charge amount for testing.</summary>
        private decimal chargeAmount;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            LogManager.Initialize(new[] { MockRepository.GenerateStub<ILogger>() });
            this.companyEntityId = new EntityId();
            this.campaignEntityId = new EntityId();
            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, "companyname");
            this.campaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                this.campaignEntityId, "campaignname", 10000, DateTime.UtcNow, DateTime.UtcNow, "persona");
            this.billingCustomerId = "somecustomerid";
            this.companyEntity.SetPropertyByName(BillingActivityNames.CustomerBillingId, this.billingCustomerId);
            this.authUserId = "userfoo";
            this.userEntityId = new EntityId();
            this.userEntity = EntityTestHelpers.CreateTestUserEntity(this.userEntityId, this.authUserId, string.Empty);
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.accessHandler = MockRepository.GenerateStub<IResourceAccessHandler>();
            this.paymentProcessor = MockRepository.GenerateStub<IPaymentProcessor>();

            // Setup default payment processor stubs
            this.paymentProcessor.Stub(f => f.ProcessorName).Return(StripePaymentProcessor.ProcessorNameBack);
            this.chargeId = "uid";
            this.chargeAmount = 10.00m;
            this.paymentProcessor.Stub(f => f.ChargeCustomer(
                    Arg<string>.Is.Anything,
                    Arg<decimal>.Is.Anything,
                    Arg<string>.Is.Anything)).Return(this.chargeId);

            this.paymentProcessor.Stub(f => f.RefundCustomer(
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<decimal>.Is.Anything)).Return(this.chargeId);

            // Setup stubs for access checks
            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Equal(this.authUserId)))
                .Return(this.userEntity);
            this.accessHandler.Stub(f => f.CheckAccess(
                    Arg<CanonicalResource>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.userEntityId))).Return(true);

            // Setup get stub for company and campaign
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, this.companyEntity, false);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.campaignEntityId, this.campaignEntity, false);
        }

        /// <summary>Happy path construction success.</summary>
        [TestMethod]
        public void ConstructorSuccess()
        {
            var handler = new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.campaignEntityId, this.authUserId, this.chargeAmount, true, this.chargeId);
            Assert.IsNotNull(handler);
            Assert.IsInstanceOfType(handler, typeof(ChargeBillingAccountHandler));
            Assert.AreSame(this.repository, handler.Repository);
            Assert.AreSame(this.accessHandler, handler.AccessHandler);
            Assert.AreEqual(this.companyEntityId, handler.CompanyEntityId);
            Assert.AreEqual(this.campaignEntityId, handler.CampaignEntityId);
            Assert.AreEqual(this.authUserId, handler.AuthUserId);
            Assert.AreEqual(this.chargeAmount, handler.ChargeAmount);
            Assert.AreEqual(this.chargeId, handler.ChargeId);
            Assert.AreEqual(true, handler.IsCheck);
        }

        /// <summary>Null repository should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullRepository()
        {
            new ChargeBillingAccountHandler(
                null, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.campaignEntityId, this.authUserId, 0m, false, null);
        }

        /// <summary>Null access handler should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullAccessHandler()
        {
            new ChargeBillingAccountHandler(
                this.repository, null, this.paymentProcessor, this.companyEntityId, this.campaignEntityId, this.authUserId, 0m, false, null);
        }

        /// <summary>Null payment processor should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullPaymentProcessor()
        {
            new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, null, this.companyEntityId, this.campaignEntityId, this.authUserId, 0m, false, null);
        }

        /// <summary>Null company id should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullCompanyId()
        {
            new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.campaignEntityId, null, this.authUserId, 0m, false, null);
        }

        /// <summary>Null campaign id should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullCampaignId()
        {
            new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, null, this.authUserId, 0m, false, null);
        }

        /// <summary>Null auth user id should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullAuthUserId()
        {
            new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.campaignEntityId, this.companyEntityId, null, 0m, false, null);
        }

        /// <summary>Null charge id in refund scenario should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullChargeIdOnRefund()
        {
            new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.campaignEntityId, this.companyEntityId, this.authUserId, -10m, false, null);
        }

        /// <summary>Charge check with negative amount fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateChargeBillingAccountHandlerWithCheckAndNegativeAmount()
        {
            new ChargeBillingAccountHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.campaignEntityId, this.companyEntityId, this.authUserId, -10m, true, "uid");
        }

        /// <summary>Failed access check should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void ExecuteAccessDenied()
        {
            // Setup stub for failed access check
            this.accessHandler = MockRepository.GenerateStub<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(
                    Arg<CanonicalResource>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.userEntityId))).Return(false);
            var handler = this.BuildHandler(true);

            handler.Execute();
        }

        /// <summary>Missing customer billing id should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void ExecuteCustomerBillingIdMissing()
        {
            // Remove billing id property from company
            var billingIdProperty = this.companyEntity.Properties.Single(p => p.Name == BillingActivityNames.CustomerBillingId);
            this.companyEntity.Properties.Remove(billingIdProperty);

            var handler = this.BuildHandler(true);
            handler.Execute();
        }

        /// <summary>Happy-path customer charge success.</summary>
        [TestMethod]
        public void ExecuteChargeSuccess()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var handler = this.BuildHandler(true, e => { savedProperties = e; });

            handler.Execute();

            this.paymentProcessor.AssertWasCalled(f => f.ChargeCustomer(
                this.billingCustomerId, this.chargeAmount, BillingActivityNames.DefaultPaymentDescription));

            Assert.IsFalse(savedProperties.Any(p => p.Name == BillingActivityNames.IsBillingApproved));
            this.AssertBillingHistory(savedProperties, 1, ChargeBillingAccountHandler.ChargeTypeCharge);
        }

        /// <summary>Happy-path customer refund success.</summary>
        [TestMethod]
        public void ExecuteRefundSuccess()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            this.chargeAmount = -10.00m;
            var handler = this.BuildHandler(false, e => { savedProperties = e; });

            handler.Execute();

            this.paymentProcessor.AssertWasCalled(f => f.RefundCustomer(
                this.billingCustomerId, this.chargeId, -this.chargeAmount));

            Assert.IsFalse(savedProperties.Any(p => p.Name == BillingActivityNames.IsBillingApproved));
            this.AssertBillingHistory(savedProperties, 1, ChargeBillingAccountHandler.ChargeTypeRefund);
        }

        /// <summary>Update billing history.</summary>
        [TestMethod]
        public void ExecuteChargeBillingHistoryIsUpdated()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var handler = this.BuildHandler(true, e => { savedProperties = e; });

            // Setup campaign with existing history
            handler.Execute();
            var historyJson = (string)savedProperties.Single(p => p.Name == BillingActivityNames.BillingHistory).Value;
            this.campaignEntity.SetPropertyByName(BillingActivityNames.BillingHistory, historyJson);

            // Call it again to append
            handler.Execute();

            this.AssertBillingHistory(savedProperties, 2);
        }

        /// <summary>Update billing history throws if existing json invalid.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void ExecuteChargeBillingInvalidHistoryJsonThrows()
        {
            var handler = this.BuildHandler(true);

            var historyJson = "Not the json we were expecting";
            this.campaignEntity.SetPropertyByName(BillingActivityNames.BillingHistory, historyJson);

            handler.Execute();
        }

        /// <summary>Execute a charge check successfully.</summary>
        [TestMethod]
        public void ExecuteChargeCheck()
        {
            IEnumerable<EntityProperty> savedProperties = null;
            var handler = this.BuildHandler(true, true, e => { savedProperties = e; });

            this.paymentProcessor.Stub(f => f.PerformChargeCheck(
                    Arg<string>.Is.Anything,
                    Arg<decimal>.Is.Anything,
                    Arg<string>.Is.Anything)).Return(this.chargeId);

            handler.Execute();

            this.paymentProcessor.AssertWasCalled(f => f.PerformChargeCheck(
                this.billingCustomerId, this.chargeAmount, BillingActivityNames.DefaultPaymentDescription));

            var isBillingApproved =
                (bool)savedProperties.Single(p => p.Name == BillingActivityNames.IsBillingApproved).Value;
            Assert.IsTrue(isBillingApproved);
            this.AssertBillingHistory(savedProperties, 1, ChargeBillingAccountHandler.ChargeTypeCheck);
        }

        /// <summary>Throw if we fail to update campaign.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void ExecuteUpdateCampaignFails()
        {
            var handler = this.BuildHandler(true, false, e => { }, true);

            handler.Execute();
        }
        
        /// <summary>Build a ChargeBillingAccountHandler instance for testing.</summary>
        /// <param name="isCharge">True for charge, false for refund.</param>
        /// <returns>The handler instance</returns>
        private ChargeBillingAccountHandler BuildHandler(bool isCharge)
        {
            return BuildHandler(isCharge, e => { });
        }

        /// <summary>Build a ChargeBillingAccountHandler instance for testing.</summary>
        /// <param name="isCharge">True for charge, false for refund.</param>
        /// <param name="captureProperties">Variable to capture saved properties.</param>
        /// <returns>The handler instance</returns>
        private ChargeBillingAccountHandler BuildHandler(bool isCharge, Action<IEnumerable<EntityProperty>> captureProperties)
        {
            return BuildHandler(isCharge, false, captureProperties, false);
        }

        /// <summary>Build a ChargeBillingAccountHandler instance for testing.</summary>
        /// <param name="isCharge">True for charge, false for refund.</param>
        /// <param name="isCheck">True for a charge check.</param>
        /// <param name="captureProperties">Variable to capture saved properties.</param>
        /// <returns>The handler instance</returns>
        private ChargeBillingAccountHandler BuildHandler(bool isCharge, bool isCheck, Action<IEnumerable<EntityProperty>> captureProperties)
        {
            return BuildHandler(isCharge, isCheck, captureProperties, false);
        }

        /// <summary>Build a ChargeBillingAccountHandler instance for testing.</summary>
        /// <param name="isCharge">True for charge, false for refund.</param>
        /// <param name="isCheck">True for a charge check.</param>
        /// <param name="captureProperties">Variable to capture saved properties.</param>
        /// <param name="failUpdate">True to fail update.</param>
        /// <returns>The handler instance</returns>
        private ChargeBillingAccountHandler BuildHandler(bool isCharge, bool isCheck, Action<IEnumerable<EntityProperty>> captureProperties, bool failUpdate)
        {
            RepositoryStubUtilities.SetupTryUpdateEntityStub(this.repository, this.campaignEntityId, captureProperties, failUpdate);

            var chargeIdToUse = isCharge ? null : this.chargeId;
            return new ChargeBillingAccountHandler(
                this.repository,
                this.accessHandler,
                this.paymentProcessor,
                this.companyEntityId,
                this.campaignEntityId,
                this.authUserId,
                this.chargeAmount,
                isCheck,
                chargeIdToUse);
        }
        
        /// <summary>Assert the billing history is valid after a charge.</summary>
        /// <param name="savedProperties">The list of saved properties captured in the repository update.</param>
        /// <param name="expectedCount">The number of entries expected in the history.</param>
        private void AssertBillingHistory(IEnumerable<EntityProperty> savedProperties, int expectedCount)
        {
            AssertBillingHistory(savedProperties, expectedCount, null);
        }

        /// <summary>Assert the billing history is valid after a charge.</summary>
        /// <param name="savedProperties">The list of saved properties captured in the repository update.</param>
        /// <param name="expectedCount">The number of entries expected in the history.</param>
        /// <param name="expectedType">Expected charge type. Null to suppress checking this.</param>
        private void AssertBillingHistory(IEnumerable<EntityProperty> savedProperties, int expectedCount, string expectedType)
        {
            var historyJson = (string)savedProperties.Single(p => p.Name == BillingActivityNames.BillingHistory).Value;
            var history = AppsJsonSerializer.DeserializeObject<List<Dictionary<string, object>>>(historyJson);
            Assert.IsNotNull(history);
            Assert.AreEqual(expectedCount, history.Count);

            foreach (var historyItem in history)
            {
                // {PaymentProcessor:"Stripe",ChargeDate:"",ChargeId:"uid",ChargeAmount:10.00}
                Assert.AreEqual(StripePaymentProcessor.ProcessorNameBack, (string)historyItem["proc"]);
                Assert.IsNotNull((DateTime)(new PropertyValue(PropertyType.Date, (string)historyItem["date"])));
                Assert.AreEqual(this.chargeId, (string)historyItem["cid"]);
                Assert.AreEqual(this.chargeAmount, Convert.ToDecimal((double)historyItem["camt"]));

                if (expectedType == null)
                {
                    continue;
                }

                Assert.AreEqual(expectedType, (string)historyItem["type"]);
            }
        }
    }
}
