// -----------------------------------------------------------------------
// <copyright file="StripePaymentProcessorFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaymentProcessor;
using Stripe;
using Utilities;

namespace BillingIntegrationTests
{
    /// <summary>Integration-test fixture for Stripe implementation of IPaymentProcessor.</summary>
    [TestClass]
    public class StripePaymentProcessorFixture
    {
        /// <summary>Stripe secret key used for testing.</summary>
        private string stripeTestSecretKey;

        /// <summary>Stripe public key used for testing.</summary>
        private string stripeTestPublicKey;

        /// <summary>Per-test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.stripeTestSecretKey = ConfigurationManager.AppSettings["PaymentProcessor.ApiSecretKey"];
            this.stripeTestPublicKey = ConfigurationManager.AppSettings["PaymentProcessor.ApiPublicKey"];
        }

        /// <summary>Successfully run customer charge pipeline (create, update, charge, refund).</summary>
        [TestMethod]
        public void ChargePipelineSuccess()
        {
            var tokenId = this.GetTestBillingToken();
            IPaymentProcessor paymentProcessor = new StripePaymentProcessor(this.stripeTestSecretKey);
            var customerBillingId = paymentProcessor.CreateCustomer(tokenId, "rc_TestCompany");
            Assert.IsFalse(string.IsNullOrEmpty(customerBillingId));

            tokenId = this.GetTestBillingToken();
            var customerBillingIdUpdate = paymentProcessor.UpdateCustomer(tokenId, customerBillingId, "rc_TestCompanyModified");
            Assert.AreEqual(customerBillingId, customerBillingIdUpdate);

            var stripeCustomerService = new StripeCustomerService(this.stripeTestSecretKey);
            var resultStripeCustomer = stripeCustomerService.Get(customerBillingId);
            Assert.AreEqual("rc_TestCompanyModified", resultStripeCustomer.Description);

            var chargeId = paymentProcessor.ChargeCustomer(customerBillingId, 10.00m, "Rare Crowds Inc.");
            Assert.IsFalse(string.IsNullOrEmpty(chargeId));

            var stripeChargeService = new StripeChargeService(this.stripeTestSecretKey);
            var stripeCharge = stripeChargeService.Get(chargeId);
            Assert.AreEqual(1000, stripeCharge.AmountInCents);

            var chargeIdRefund = paymentProcessor.RefundCustomer(customerBillingId, chargeId, 5.00m);
            Assert.AreEqual(chargeId, chargeIdRefund);

            stripeCharge = stripeChargeService.Get(chargeId);
            Assert.AreEqual(1000, stripeCharge.AmountInCents);
            Assert.AreEqual(500, stripeCharge.AmountInCentsRefunded);

            var chargeIdCheck = paymentProcessor.PerformChargeCheck(customerBillingId, 10.00m, "Rare Crowds Inc.");
            stripeCharge = stripeChargeService.Get(chargeIdCheck);
            Assert.AreEqual(1000, stripeCharge.AmountInCents);
            Assert.AreEqual(1000, stripeCharge.AmountInCentsRefunded);
        }

        /// <summary>Failure throws on create customer.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateCustomerFail()
        {
            IPaymentProcessor paymentProcessor = new StripePaymentProcessor(this.stripeTestSecretKey);
            paymentProcessor.CreateCustomer("bogustoken", "rc_Fail");
        }

        /// <summary>Failure throws on update customer.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerFail()
        {
            var tokenId = this.GetTestBillingToken();
            IPaymentProcessor paymentProcessor = new StripePaymentProcessor(this.stripeTestSecretKey);
            paymentProcessor.UpdateCustomer(tokenId, "bogusbillingid", "rc_Fail");
        }

        /// <summary>Failure throws on charge customer.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ChargeCustomerFail()
        {
            IPaymentProcessor paymentProcessor = new StripePaymentProcessor(this.stripeTestSecretKey);
            paymentProcessor.ChargeCustomer("bogusbillingid", 10.00m, "rc_Fail");
        }

        /// <summary>Failure throws on perform charge check.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void PerformChargeCheckFail()
        {
            IPaymentProcessor paymentProcessor = new StripePaymentProcessor(this.stripeTestSecretKey);
            paymentProcessor.PerformChargeCheck("bogusbillingid", 10.00m, "rc_Fail");
        }

        /// <summary>Failure throws on refund customer.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void RefundCustomerFail()
        {
            IPaymentProcessor paymentProcessor = new StripePaymentProcessor(this.stripeTestSecretKey);
            paymentProcessor.RefundCustomer("bogusbillingid", "buguschargeId", 10.00m);
        }

        /// <summary>Call Stripe api to get a test billing token.</summary>
        /// <returns>The token id.</returns>
        private string GetTestBillingToken()
        {
            var tokenService = new StripeTokenService(this.stripeTestPublicKey);
            var stripeToken = new StripeTokenCreateOptions();
            stripeToken.CardNumber = "4242424242424242";
            stripeToken.CardCvc = "123";
            stripeToken.CardExpirationMonth = "{0}".FormatInvariant(DateTime.Now.AddMonths(1).Month);
            stripeToken.CardExpirationYear = "{0}".FormatInvariant(DateTime.Now.AddYears(1).Year);
            return tokenService.Create(stripeToken).Id;
        }
    }
}
