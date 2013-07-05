// -----------------------------------------------------------------------
// <copyright file="StripePaymentProcessor.cs" company="Rare Crowds Inc">
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
using Diagnostics;
using Stripe;
using Utilities;

namespace PaymentProcessor
{
    /// <summary>Implemenation of IPaymentProcessor for Stripe.com</summary>
    public class StripePaymentProcessor : IPaymentProcessor
    {
        /// <summary>The name of the payment processor.</summary>
        internal const string ProcessorNameBack = "Stripe";

        /// <summary>Initializes a new instance of the <see cref="StripePaymentProcessor"/> class.</summary>
        /// <param name="secretKey">The secret key for accessing Stripe.com api.</param>
        public StripePaymentProcessor(string secretKey)
        {
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new AppsGenericException("Valid secret key not supplied.");
            }

            this.SecretKey = secretKey;
        }

        /// <summary>Gets the name of the PaymentProcessor.</summary>
        public string ProcessorName
        {
            get { return ProcessorNameBack; }
        }

        /// <summary>Gets the secret part of the api access key.</summary>
        internal string SecretKey { get; private set; }

        /// <summary>Create a customer on the billing service.</summary>
        /// <param name="billingInformationToken">A token representing the customers billing information.</param>
        /// <param name="companyName">The internal name of the customer (company name).</param>
        /// <returns>A customer billing id.</returns>
        public string CreateCustomer(string billingInformationToken, string companyName)
        {
            try
            {
                if (string.IsNullOrEmpty(billingInformationToken))
                {
                    throw new ArgumentException("Missing or invalid billing information token.", "billingInformationToken");
                }

                if (string.IsNullOrEmpty(companyName))
                {
                    throw new ArgumentException("Missing or invalid company name.", "companyName");
                }

                var stripeCustomerService = new StripeCustomerService(this.SecretKey);
                var stripeCustomer = new StripeCustomerCreateOptions();
                stripeCustomer.Description = companyName;
                stripeCustomer.TokenId = billingInformationToken;
                var resultStripeCustomer = stripeCustomerService.Create(stripeCustomer);

                var msg = "Stripe Billing Id created. Id: {0}, Token: {1}, Company Name: {2}"
                    .FormatInvariant(resultStripeCustomer.Id, billingInformationToken, companyName);
                LogManager.Log(LogLevels.Information, msg);

                return resultStripeCustomer.Id;
            }
            catch (StripeException ex)
            {
                var msg = "Error creating new Stripe Billing Id. Token: {0}, Company Name: {1}."
                    .FormatInvariant(billingInformationToken, companyName);
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
            catch (ArgumentException ex)
            {
                var msg = "Error creating new Stripe Billing Id - missing or invalid arguments.";
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
        }

        /// <summary>Create a customer on the billing service.</summary>
        /// <param name="billingInformationToken">A token representing the customers billing information.</param>
        /// <param name="customerBillingId">The billing id of the customer.</param>
        /// <param name="companyName">The internal name of the customer (company name).</param>
        /// <returns>A customer billing id.</returns>
        public string UpdateCustomer(string billingInformationToken, string customerBillingId, string companyName)
        {
            try
            {
                if (string.IsNullOrEmpty(billingInformationToken))
                {
                    throw new ArgumentException("Missing or invalid billing information token.", "billingInformationToken");
                }

                if (string.IsNullOrEmpty(companyName))
                {
                    throw new ArgumentException("Missing or invalid company name.", "companyName");
                }

                if (string.IsNullOrEmpty(customerBillingId))
                {
                    throw new ArgumentException("Missing or invalid customer billing id.", "customerBillingId");
                }
                
                var stripeCustomerService = new StripeCustomerService(this.SecretKey);
                var stripeCustomer = new StripeCustomerUpdateOptions();
                stripeCustomer.TokenId = billingInformationToken;
                stripeCustomer.Description = companyName;
                var resultStripeCustomer = stripeCustomerService.Update(customerBillingId, stripeCustomer);

                var msg = "Stripe Billing Id updated. Id: {0}, Token: {1}"
                    .FormatInvariant(resultStripeCustomer.Id, customerBillingId, billingInformationToken);
                LogManager.Log(LogLevels.Information, msg);

                return resultStripeCustomer.Id;
            }
            catch (StripeException ex)
            {
                var msg = "Error updating Stripe Billing Id. Token: {0}, Company Name: {1}."
                    .FormatInvariant(billingInformationToken, customerBillingId);
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
            catch (ArgumentException ex)
            {
                var msg = "Error updating Stripe Billing Id - missing or invalid arguments.";
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
        }

        /// <summary>Charge a customer on the billing service.</summary>
        /// <param name="customerBillingId">The billing id of the customer.</param>
        /// <param name="chargeAmount">Amount of charge in U.S. dollars.</param>
        /// <param name="chargeDescription">Description of charge.</param>
        /// <returns>A charge id.</returns>
        public string ChargeCustomer(string customerBillingId, decimal chargeAmount, string chargeDescription)
        {
            try
            {
                if (string.IsNullOrEmpty(customerBillingId))
                {
                    throw new ArgumentException("Missing or invalid customer billing id.", "customerBillingId");
                }

                if (string.IsNullOrEmpty(chargeDescription))
                {
                    throw new ArgumentException("Missing or invalid charge description.", "chargeDescription");
                }

                var stripeChargeService = new StripeChargeService(this.SecretKey);
                var stripeCharge = new StripeChargeCreateOptions();
                stripeCharge.AmountInCents = (int)(chargeAmount * 100);
                stripeCharge.Currency = "usd";
                stripeCharge.Description = chargeDescription;
                stripeCharge.CustomerId = customerBillingId;
                var resultStripeCharge = stripeChargeService.Create(stripeCharge);

                var msg = "Stripe Charge created. Billing Id: {0}, Charge Id: {1}, Amount in cents: {2}, Description: {3}"
                    .FormatInvariant(customerBillingId, resultStripeCharge.Id, resultStripeCharge.AmountInCents, chargeDescription);
                LogManager.Log(LogLevels.Information, msg);

                return resultStripeCharge.Id;
            }
            catch (StripeException ex)
            {
                var msg = "Error charging Stripe Billing Id. Id: {0}, Amount in cents: {1}, Description: {2}."
                    .FormatInvariant(customerBillingId, chargeAmount, chargeDescription);
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
            catch (ArgumentException ex)
            {
                var msg = "Error charging Stripe Billing Id - missing or invalid arguments.";
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
        }

        /// <summary>Refund a customer on the billing service.</summary>
        /// <param name="customerBillingId">The billing id of the customer.</param>
        /// <param name="chargeId">Id of charge to refund against.</param>
        /// <param name="refundAmount">Amount of refund in U.S. dollars.</param>
        /// <returns>A charge id.</returns>
        public string RefundCustomer(string customerBillingId, string chargeId, decimal refundAmount)
        {
            try
            {
                if (string.IsNullOrEmpty(customerBillingId))
                {
                    throw new ArgumentException("Missing or invalid customer billing id.", "customerBillingId");
                }

                if (string.IsNullOrEmpty(chargeId))
                {
                    throw new ArgumentException("Missing or invalid transaction id.", "chargeId");
                }

                var stripeChargeService = new StripeChargeService(this.SecretKey);
                var amountInCents = (int)(refundAmount * 100);
                var resultStripeCharge = stripeChargeService.Refund(chargeId, amountInCents);

                var msg = "Stripe Refund submitted. Billing Id: {0}, Charge Id: {1}, Amount in cents: {2}"
                    .FormatInvariant(customerBillingId, resultStripeCharge.Id, resultStripeCharge.AmountInCents);
                LogManager.Log(LogLevels.Information, msg);

                return resultStripeCharge.Id;
            }
            catch (StripeException ex)
            {
                var msg = "Error refunding Stripe Billing Id. Id: {0}, Charge Id: {1}, Refund amount: {2}."
                    .FormatInvariant(customerBillingId, chargeId, refundAmount);
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
            catch (ArgumentException ex)
            {
                var msg = "Error refunding Stripe Billing Id - missing or invalid arguments.";
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }
        }

        /// <summary>Perform a charge check (but not an actual charge) on the billing service.</summary>
        /// <param name="customerBillingId">The billing id of the customer.</param>
        /// <param name="chargeAmount">Amount of charge in U.S. dollars.</param>
        /// <param name="chargeDescription">Description of charge.</param>
        /// <returns>A charge id.</returns>
        public string PerformChargeCheck(string customerBillingId, decimal chargeAmount, string chargeDescription)
        {
            // TODO: For now this is implemented as a charge/refund - when we replace strip.net lib implement it as
            // an uncaptured charge and a refund.
            string chargeId;

            try
            {
                chargeId = this.ChargeCustomer(customerBillingId, chargeAmount, chargeDescription);
            }
            catch (AppsGenericException ex)
            {
                var msg = "Error performing charge check on Stripe Billing Id. Id: {0}, Amount in cents: {1}, Description: {2}."
                    .FormatInvariant(customerBillingId, chargeAmount, chargeDescription);
                LogManager.Log(LogLevels.Error, msg);
                throw new AppsGenericException(msg, ex);
            }

            try
            {
                chargeId = this.RefundCustomer(customerBillingId, chargeId, chargeAmount);

                var msg = "Stripe Charge Check performed. Billing Id: {0}, Charge Id: {1}, Description: {2}."
                    .FormatInvariant(customerBillingId, chargeId, chargeDescription);
                LogManager.Log(LogLevels.Information, msg);
            }
            catch (AppsGenericException ex)
            {
                // This needs to be an alert
                var msg = "Error refunding charge check on Stripe Billing Id. Id: {0}, Amount in cents: {1}, Description: {2}."
                    .FormatInvariant(customerBillingId, chargeAmount, chargeDescription);
                LogManager.Log(LogLevels.Error, true, msg);
                throw new AppsGenericException(msg, ex);
            }

            return chargeId;
        }
    }
}
