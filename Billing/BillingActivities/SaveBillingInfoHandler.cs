// -----------------------------------------------------------------------
// <copyright file="SaveBillingInfoHandler.cs" company="Rare Crowds Inc">
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
    public class SaveBillingInfoHandler : IActivityHandler
    {
        /// <summary>Initializes a new instance of the <see cref="SaveBillingInfoHandler"/> class.</summary>
        /// <param name="repository">IEntityRepository instance.</param>
        /// <param name="accessHandler">IResourceAccessHandler instance.</param>
        /// <param name="paymentProcessor">IPaymentProcessor instance.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="messagePayload">The api request payload.</param>
        /// <param name="authUserId">Authorization user id.</param>
        internal SaveBillingInfoHandler(
            IEntityRepository repository, 
            IResourceAccessHandler accessHandler, 
            IPaymentProcessor paymentProcessor, 
            EntityId companyEntityId, 
            string messagePayload, 
            string authUserId)
        {
            if (repository == null)
            {
                throw new AppsGenericException("Null repository instance passed to SaveBillingInfoHandler constructor.");
            }

            if (accessHandler == null)
            {
                throw new AppsGenericException("Null accessHandler instance passed to SaveBillingInfoHandler constructor.");
            }

            if (paymentProcessor == null)
            {
                throw new AppsGenericException("Null paymentProcessor instance passed to SaveBillingInfoHandler constructor.");
            }

            if (companyEntityId == null)
            {
                throw new AppsGenericException("Null companyEntityId passed to SaveBillingInfoHandler constructor.");
            }

            if (messagePayload == null)
            {
                throw new AppsGenericException("Null messagePayload passed to SaveBillingInfoHandler constructor.");
            }

            if (authUserId == null)
            {
                throw new AppsGenericException("Null authUserId passed to SaveBillingInfoHandler constructor.");
            }

            this.Repository = repository;
            this.AccessHandler = accessHandler;
            this.CompanyEntityId = companyEntityId;
            this.MessagePayload = messagePayload;
            this.AuthUserId = authUserId;
            this.PaymentProcessor = paymentProcessor;
        }

        /// <summary>Gets the payment processor.</summary>
        internal IPaymentProcessor PaymentProcessor { get; private set; }

        /// <summary>Gets the api request payload.</summary>
        internal string MessagePayload { get; private set; }

        /// <summary>Gets the company entity id.</summary>
        internal EntityId CompanyEntityId { get; private set; }

        /// <summary>Gets the repository instance.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Gets the IResourceAccessHandler instance.</summary>
        internal IResourceAccessHandler AccessHandler { get; private set; }

        /// <summary>Gets the authorization user id.</summary>
        internal string AuthUserId { get; private set; }

        /// <summary>Execute the activity handler.</summary>
        /// <returns>The activity result.</returns>
        public IDictionary<string, string> Execute()
        {
            var storageRequestContext = new RequestContext
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

            // Get/save external properties
            var company = this.Repository.GetEntity<CompanyEntity>(storageRequestContext, this.CompanyEntityId);
            var token = AppsJsonSerializer.DeserializeObject<Dictionary<string, string>>(this.MessagePayload)["BillingInfoToken"];
            LogManager.Log(LogLevels.Information, "BillingInfoToken received: {0}".FormatInvariant(token));
            var customerBillingId = company.TryGetPropertyByName<string>(BillingActivityNames.CustomerBillingId, null);

            if (string.IsNullOrEmpty(customerBillingId))
            {
                customerBillingId = this.PaymentProcessor.CreateCustomer(token, company.ExternalName);
                company.SetPropertyByName("CustomerBillingId", customerBillingId, PropertyFilter.Extended);
                this.Repository.SaveEntity(storageRequestContext, company);
                LogManager.Log(LogLevels.Information, "CustomerBillingId updated: {0}".FormatInvariant(customerBillingId));
            }
            else
            {
                this.PaymentProcessor.UpdateCustomer(token, customerBillingId, company.ExternalName);
                LogManager.Log(LogLevels.Information, "CustomerBillingId updated: {0}".FormatInvariant(customerBillingId));
            }

            return new Dictionary<string, string>();
        }
    }
}