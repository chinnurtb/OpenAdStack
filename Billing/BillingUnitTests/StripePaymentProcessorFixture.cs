// -----------------------------------------------------------------------
// <copyright file="StripePaymentProcessorFixture.cs" company="Rare Crowds Inc">
//    Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaymentProcessor;
using Utilities;

namespace BillingUnitTests
{
    /// <summary>Unit-test fixture for Stripe implementation of IPaymentProcessor.</summary>
    [TestClass]
    public class StripePaymentProcessorFixture
    {
        /// <summary>Stripe secret key used for testing.</summary>
        private const string StripeTestSecretKey = "sk_test_somestuff";

        /// <summary>Successful construction.</summary>
        [TestMethod]
        public void ConstructorSuccess()
        {
            Assert.IsNotNull(new StripePaymentProcessor(StripeTestSecretKey));
        }

        /// <summary>Failed construction for null key.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullKey()
        {
            Assert.IsNotNull(new StripePaymentProcessor(null));
        }

        /// <summary>Failed construction for empty key.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorEmptyKey()
        {
            Assert.IsNotNull(new StripePaymentProcessor(string.Empty));
        }

        /// <summary>Throws if token arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateCustomerTokenArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.CreateCustomer(null, "foo");
        }

        /// <summary>Throws if token arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateCustomerTokenArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.CreateCustomer(string.Empty, "foo");
        }

        /// <summary>Throws if customer name arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateCustomerNameArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.CreateCustomer("foo", null);
        }

        /// <summary>Throws if customer name arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void CreateCustomerNameArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.CreateCustomer("foo", string.Empty);
        }

        /// <summary>Throws if token arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerTokenArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.UpdateCustomer(null, "foo", "foo");
        }

        /// <summary>Throws if token arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerTokenArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.UpdateCustomer(string.Empty, "foo", "foo");
        }

        /// <summary>Throws if id arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerBillingIdArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.UpdateCustomer("foo", null, "foo");
        }

        /// <summary>Throws if id arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerBillingIdArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.UpdateCustomer("foo", string.Empty, "foo");
        }

        /// <summary>Throws if name arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerNameArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.UpdateCustomer("foo", "foo", null);
        }

        /// <summary>Throws if name arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void UpdateCustomerNameArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.UpdateCustomer("foo", "foo", string.Empty);
        }

        /// <summary>Throws if id arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ChargeCustomerBillingIdArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.ChargeCustomer(null, 1.00m, "desc");
        }

        /// <summary>Throws if id arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ChargeCustomerBillingIdArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.ChargeCustomer(string.Empty, 1.00m, "desc");
        }

        /// <summary>Throws if id description is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ChargeCustomerDescriptionArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.ChargeCustomer("foo", 1.00m, null);
        }

        /// <summary>Throws if id description is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ChargeCustomerDescriptionArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.ChargeCustomer("foo", 1.00m, string.Empty);
        }

        /// <summary>Throws if id arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void RefundCustomerBillingIdArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.RefundCustomer(null, "foo", 1.00m);
        }

        /// <summary>Throws if id arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void RefundCustomerBillingIdArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.RefundCustomer(string.Empty, "foo", 1.00m);
        }

        /// <summary>Throws if transaction id arg is null.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void RefundCustomerTransactionIdArgNull()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.RefundCustomer("foo", null, 1.00m);
        }

        /// <summary>Throws if transaction id arg is empty.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void RefundCustomerTransactionIdArgEmpty()
        {
            var paymentProcessor = new StripePaymentProcessor(StripeTestSecretKey);
            paymentProcessor.RefundCustomer("foo", string.Empty, 1.00m);
        }
    }
}
