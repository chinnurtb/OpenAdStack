// -----------------------------------------------------------------------
// <copyright file="ChargeBillingAccountHandler.cs" company="Rare Crowds Inc">
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
using Activities;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;
using PaymentProcessor;
using ResourceAccess;
using Utilities;
using Utilities.Serialization;

namespace BillingActivities
{
    /// <summary>Class to save customer billing information.</summary>
    public class ChargeBillingAccountHandler : IActivityHandler
    {
        /// <summary>Billing History charge type or a charge check.</summary>
        internal const string ChargeTypeCheck = "check";

        /// <summary>Billing History charge type or a charge check.</summary>
        internal const string ChargeTypeCharge = "charge";

        /// <summary>Billing History charge type or a charge check.</summary>
        internal const string ChargeTypeRefund = "refund";

        /// <summary>Initializes a new instance of the <see cref="ChargeBillingAccountHandler"/> class.</summary>
        /// <param name="repository">IEntityRepository instance.</param>
        /// <param name="accessHandler">IResourceAccessHandler instance.</param>
        /// <param name="paymentProcessor">IPaymentProcessor instance.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="campaignEntityId">Campaign entity id.</param>
        /// <param name="authUserId">Authorization user id.</param>
        /// <param name="chargeAmount">The amount to be charged (or refunded).</param>
        /// <param name="chargeId">The charge id for a refund (null if not a refund)</param>
        internal ChargeBillingAccountHandler(IEntityRepository repository, IResourceAccessHandler accessHandler, IPaymentProcessor paymentProcessor, EntityId companyEntityId, EntityId campaignEntityId, string authUserId, decimal chargeAmount, string chargeId)
            : this(repository, accessHandler, paymentProcessor, companyEntityId, campaignEntityId, authUserId, chargeAmount, false, chargeId)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="ChargeBillingAccountHandler"/> class.</summary>
        /// <param name="repository">IEntityRepository instance.</param>
        /// <param name="accessHandler">IResourceAccessHandler instance.</param>
        /// <param name="paymentProcessor">IPaymentProcessor instance.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="campaignEntityId">Campaign entity id.</param>
        /// <param name="authUserId">Authorization user id.</param>
        /// <param name="chargeAmount">The amount to be charged (or refunded).</param>
        /// <param name="isCheck">True if this is a charge check.</param>
        /// <param name="chargeId">The charge id for a refund (null if not a refund)</param>
        internal ChargeBillingAccountHandler(IEntityRepository repository, IResourceAccessHandler accessHandler, IPaymentProcessor paymentProcessor, EntityId companyEntityId, EntityId campaignEntityId, string authUserId, decimal chargeAmount, bool isCheck, string chargeId)
        {
            if (repository == null)
            {
                throw new AppsGenericException("Null repository instance passed to ChargeBillingAccountHandler constructor.");
            }

            if (accessHandler == null)
            {
                throw new AppsGenericException("Null accessHandler instance passed to ChargeBillingAccountHandler constructor.");
            }

            if (paymentProcessor == null)
            {
                throw new AppsGenericException("Null paymentProcessor instance passed to ChargeBillingAccountHandler constructor.");
            }

            if (companyEntityId == null)
            {
                throw new AppsGenericException("Null companyEntityId passed to ChargeBillingAccountHandler constructor.");
            }

            if (campaignEntityId == null)
            {
                throw new AppsGenericException("Null campaignEntityId passed to ChargeBillingAccountHandler constructor.");
            }

            if (authUserId == null)
            {
                throw new AppsGenericException("Null authUserId passed to ChargeBillingAccountHandler constructor.");
            }

            if (chargeAmount < 0 && isCheck)
            {
                throw new AppsGenericException("Negative charge amount not allowed with charge check in ChargeBillingAccountHandler constructor.");
            }

            if (chargeAmount < 0 && chargeId == null)
            {
                throw new AppsGenericException("Null chargeId not allowed if refunding in ChargeBillingAccountHandler constructor.");
            }

            this.Repository = repository;
            this.AccessHandler = accessHandler;
            this.CompanyEntityId = companyEntityId;
            this.CampaignEntityId = campaignEntityId;
            this.AuthUserId = authUserId;
            this.PaymentProcessor = paymentProcessor;
            this.ChargeAmount = chargeAmount;
            this.ChargeId = chargeId;
            this.IsCheck = isCheck;
        }

        /// <summary>Gets the payment processor.</summary>
        internal IPaymentProcessor PaymentProcessor { get; private set; }

        /// <summary>Gets the company entity id.</summary>
        internal EntityId CompanyEntityId { get; private set; }

        /// <summary>Gets the campaign entity id.</summary>
        internal EntityId CampaignEntityId { get; private set; }

        /// <summary>Gets the repository instance.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Gets the IResourceAccessHandler instance.</summary>
        internal IResourceAccessHandler AccessHandler { get; private set; }

        /// <summary>Gets the authorization user id.</summary>
        internal string AuthUserId { get; private set; }

        /// <summary>Gets the charge amount.</summary>
        internal decimal ChargeAmount { get; private set; }

        /// <summary>Gets the charge id.</summary>
        internal string ChargeId { get; private set; }

        /// <summary>Gets a value indicating whether this is a charge check.</summary>
        internal bool IsCheck { get; private set; }

        /// <summary>Execute the activity handler.</summary>
        /// <returns>The activity result.</returns>
        public IDictionary<string, string> Execute()
        {
            var requestContext = new RequestContext
            {
                UserId = this.AuthUserId,
                ExternalCompanyId = this.CompanyEntityId,
                EntityFilter = new RepositoryEntityFilter(true, false, true, false)
            };

            var canonicalResource = new CanonicalResource(
                    new Uri("https://localhost/api/entity/company/{0}".FormatInvariant(this.CompanyEntityId), UriKind.Absolute), "POST");
            if (!this.AccessHandler.CheckAccessByUserId(this.Repository, canonicalResource, this.AuthUserId))
            {
                var msg = "User access denied. UserId: {0}, Resource: {1}".FormatInvariant(
                    this.AuthUserId, canonicalResource.CanonicalDescriptor);
                throw new ActivityException(ActivityErrorId.UserAccessDenied, msg);
            }

            // Get customer billing Id.
            var company = this.Repository.GetEntity<CompanyEntity>(requestContext, this.CompanyEntityId);
            var customerBillingId = company.TryGetPropertyByName<string>(BillingActivityNames.CustomerBillingId, null);
            if (customerBillingId == null)
            {
                var msg = "Customer Billing Id missing. Company Id: {0}".FormatInvariant(this.CompanyEntityId);
                throw new ActivityException(ActivityErrorId.GenericError, msg);
            }

            string chargeId;
            string chargeType;
            if (this.IsCheck)
            {
                chargeType = ChargeTypeCheck;
                chargeId = this.PaymentProcessor.PerformChargeCheck(
                    customerBillingId, this.ChargeAmount, BillingActivityNames.DefaultPaymentDescription);
            }
            else if (this.ChargeAmount >= 0)
            {
                chargeType = ChargeTypeCharge;
                chargeId = this.PaymentProcessor.ChargeCustomer(
                    customerBillingId, this.ChargeAmount, BillingActivityNames.DefaultPaymentDescription);
            }
            else
            {
                chargeType = ChargeTypeRefund;
                chargeId = this.PaymentProcessor.RefundCustomer(customerBillingId, this.ChargeId, -this.ChargeAmount);
            }

            var historyJson = this.TryAddChargeToHistory(requestContext, chargeId, chargeType);
           
            if (historyJson == null || !this.TryUpdateCampaign(requestContext, historyJson, this.IsCheck))
            {
                // Recovery scenario may require manual intervention currently. Send an alert.
                var msg = "Payment provider action succeeded but campaign could not be updated. Company: {0}, Campaign: {1}, Charge Id: {2}"
                    .FormatInvariant(this.CompanyEntityId, this.CampaignEntityId, chargeId);
                LogManager.Log(LogLevels.Error, true, msg);
                throw new ActivityException(ActivityErrorId.DataAccess, msg);
            }

            return new Dictionary<string, string>();
        }

        /// <summary>Update the campaign.</summary>
        /// <param name="requestContext">The repository request context.</param>
        /// <param name="historyJson">The billing history json.</param>
        /// <param name="isCheck">True if this is a charge check.</param>
        /// <returns>Returns a historyJson string.</returns>
        private bool TryUpdateCampaign(RequestContext requestContext, string historyJson, bool isCheck)
        {
            var properties = new List<EntityProperty>
                {
                    new EntityProperty(BillingActivityNames.BillingHistory, historyJson, PropertyFilter.Extended)
                };

            if (isCheck)
            {
                properties.Add(new EntityProperty(BillingActivityNames.IsBillingApproved, true, PropertyFilter.Extended));
            }

            return this.Repository.TryUpdateEntity(requestContext, this.CampaignEntityId, properties);
        }

        /// <summary>Add the charge to the campaigns billing history.</summary>
        /// <param name="requestContext">The repository request context.</param>
        /// <param name="chargeId">The id of the charge in the payment system.</param>
        /// <param name="type">Type of charge activity.</param>
        /// <returns>Returns a historyJson string.</returns>
        private string TryAddChargeToHistory(RequestContext requestContext, string chargeId, string type)
        {
            try
            {
                var campaign = this.Repository.GetEntity<CampaignEntity>(requestContext, this.CampaignEntityId);
                var historyJson = campaign.TryGetPropertyByName<string>(BillingActivityNames.BillingHistory, null);

                var history = new List<Dictionary<string, object>>();
                if (historyJson != null)
                {
                    history = AppsJsonSerializer.DeserializeObject<List<Dictionary<string, object>>>(historyJson);
                }

                var historyItem = new Dictionary<string, object>
                {
                    { "proc", this.PaymentProcessor.ProcessorName },
                    { "date", DateTime.UtcNow },
                    { "cid", chargeId },
                    { "camt", this.ChargeAmount },
                    { "type", type },
                };

                history.Insert(0, historyItem);
                return AppsJsonSerializer.SerializeObject(history);
            }
            catch (AppsGenericException ex)
            {
                var msg = "Failed to add billing activity to history. {0}".FormatInvariant(ex.ToString());
                LogManager.Log(LogLevels.Error, msg);
            }

            return null;
        }
    }
}
