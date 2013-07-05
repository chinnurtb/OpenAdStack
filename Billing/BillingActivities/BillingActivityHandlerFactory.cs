// -----------------------------------------------------------------------
// <copyright file="BillingActivityHandlerFactory.cs" company="Rare Crowds Inc">
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
using EntityUtilities;
using PaymentProcessor;
using ResourceAccess;
using Utilities;

namespace BillingActivities
{
    /// <summary>Factory class for billing activity handlers.</summary>
    public class BillingActivityHandlerFactory : IActivityHandlerFactory
    {
        /// <summary>Create the handler for the billing activity request.</summary>
        /// <param name="request">The activity request.</param>
        /// <param name="context">The activity context.</param>
        /// <returns>An IActivityHandler instance.</returns>
        public IActivityHandler CreateActivityHandler(ActivityRequest request, IDictionary<Type, object> context)
        {
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
            var repository = (IEntityRepository)context[typeof(IEntityRepository)];
            var userAccessRepository = (IUserAccessRepository)context[typeof(IUserAccessRepository)];
            var paymentProcessor = (IPaymentProcessor)context[typeof(IPaymentProcessor)];
            var userId = request.Values[EntityActivityValues.AuthUserId];
            var accessHandler = new ResourceAccessHandler(userAccessRepository, repository);

            if (request.Task == EntityActivityTasks.SaveBillingInfo)
            {
                var messagePayload = request.Values[EntityActivityValues.MessagePayload];
                return new SaveBillingInfoHandler(repository, accessHandler, paymentProcessor, companyEntityId, messagePayload, userId);
            }

            if (request.Task == EntityActivityTasks.ChargeBillingAccount)
            {
                var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
                var chargeAmountString = request.Values[EntityActivityValues.ChargeAmount];
                var chargeId = request.Values.ContainsKey(EntityActivityValues.ChargeId)
                                   ? request.Values[EntityActivityValues.ChargeId]
                                   : null;
                var isCheck = request.Values.ContainsKey(EntityActivityValues.IsChargeCheck);

                decimal chargeAmount;
                if (!decimal.TryParse(chargeAmountString, out chargeAmount))
                {
                    var chargeMsg = "Invalid ChargeAmount for ChargeBillingAccount request. ChargeAmount: {0}"
                        .FormatInvariant(chargeAmountString);
                    throw new AppsGenericException(chargeMsg);
                }

                return new ChargeBillingAccountHandler(
                    repository, accessHandler, paymentProcessor, companyEntityId, campaignEntityId, userId, chargeAmount, isCheck, chargeId);
            }

            var msg = "Unrecognized activity task: {0}".FormatInvariant(request.Task ?? "unspecified");
            throw new AppsGenericException(msg);
        }
    }
}
