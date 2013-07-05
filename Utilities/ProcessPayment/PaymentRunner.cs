// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PaymentRunner.cs" company="Rare Crowds Inc">
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
using System.Linq;
using Activities;
using BillingActivities;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;
using Microsoft.Practices.Unity;
using PaymentProcessor;
using RuntimeIoc.WorkerRole;
using Utilities.Serialization;

namespace ProcessPayment
{
    /// <summary>Class to run payment processing.</summary>
    public class PaymentRunner
    {
        /// <summary>Gets or sets Repository.</summary>
        private IEntityRepository Repository { get; set; }

        /// <summary>Gets or sets Payment Processor.</summary>
        private IPaymentProcessor PaymentProcessor { get; set; }

        /// <summary>Gets or sets User Access Repository.</summary>
        private IUserAccessRepository UserAccessRepository { get; set; }

        /// <summary>Main run method.</summary>
        /// <param name="arguments">The command-line arguments.</param>
        public void Run(ProcessPaymentArgs arguments)
        {
            this.Initialize(arguments);

            var campaignId = arguments.CampaignEntityId;
            var userId = arguments.UserId;
            var companyId = arguments.CompanyEntityId;

            if (arguments.IsForceTestApprove)
            {
                this.ForceTestApproval(campaignId);
                return;
            }

            // Get the entities to make sure we can - fail early if not.
            // User id in request context for auditing trail only.
            var context = new RequestContext 
            {
                EntityFilter = new RepositoryEntityFilter(true, false, true, false),
                UserId = userId
            };
            var campaign = this.Repository.GetEntity(context, campaignId);
            this.Repository.GetUser(context, userId);

            if (arguments.IsForceApprove)
            {
                this.ForceApproval(context, campaign.ExternalEntityId, campaign.ExternalName);
                return;
            }

            var company = this.Repository.GetEntity(context, companyId);

            if (arguments.IsHistoryDump)
            {
                DumpBillingHistory(campaign, company, true);
                return;
            }

            // Setup activity request
            var request = new ActivityRequest
            {
                Task = EntityActivityTasks.ChargeBillingAccount,
                Values =
                {
                    { EntityActivityValues.AuthUserId, userId },
                    { EntityActivityValues.EntityId, companyId },
                    { EntityActivityValues.CampaignEntityId, campaignId },
                    { EntityActivityValues.ChargeAmount, arguments.Amount },
                }
            };

            if (arguments.IsRefund)
            {
                request.Values.Add(EntityActivityValues.ChargeId, arguments.ChargeId);
            }

            if (arguments.IsCheck)
            {
                request.Values.Add(EntityActivityValues.IsChargeCheck, true.ToString());
            }

            // Run the activity
            // Set up our activity
            var activityContext = new Dictionary<Type, object>
                {
                    { typeof(IEntityRepository), this.Repository },
                    { typeof(IUserAccessRepository), this.UserAccessRepository },
                    { typeof(IPaymentProcessor), this.PaymentProcessor },
                };
            
            var activity = Activity.CreateActivity(
                    typeof(ChargeBillingAccountActivity),
                    activityContext,
                    SubmitActivityRequest) as ChargeBillingAccountActivity;
            
            var result = activity.Run(request);
            
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "ChargeBillingAccountActivity failed. {0}".FormatInvariant(result.Error.Message));
            }

            DumpBillingHistory(this.Repository.GetEntity(context, campaignId), company, false);
        }

        /// <summary>Dummy method for activity call</summary>
        /// <param name="request">The activity request.</param>
        /// <param name="sourceName">The source name.</param>
        /// <returns>always true</returns>
        private static bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            return true;
        }

        /// <summary>Get the billing history off the campaign and dump to output.</summary>
        /// <param name="campaign">Campaign entity.</param>
        /// <param name="company">Company entity.</param>
        /// <param name="isFullHistory">True for full history.</param>
        private static void DumpBillingHistory(IEntity campaign, IEntity company, bool isFullHistory)
        {
            var billingId = company.TryGetPropertyByName<string>(BillingActivityNames.CustomerBillingId, "Could not find customer billing id.");
            var name = (string)company.ExternalName;
            Console.WriteLine("History for Customer. Name: {0}, Billing Id: {1}".FormatInvariant(name, billingId));

            var billingHistoryJson = campaign.TryGetPropertyByName<string>(
                BillingActivityNames.BillingHistory, "Could not retrieve billing history.");
            var history = AppsJsonSerializer.DeserializeObject<List<Dictionary<string, object>>>(billingHistoryJson);
            if (!isFullHistory)
            {
                history = history.Take(5).ToList();
            }

            foreach (var historyItem in history)
            {
                Console.WriteLine(
                    string.Join(", ", historyItem.Select(kvp => "{0}: {1}".FormatInvariant(kvp.Key, kvp.Value.ToString()))));
            }
        }

        /// <summary>Force billing approval of the campaign without going through the billing activies.</summary>
        /// <param name="context">Repository request context for saving.</param>
        /// <param name="campaignId">Campaign entity Id.</param>
        /// <param name="campaignName">Campaign entity Name.</param>
        private void ForceApproval(RequestContext context, EntityId campaignId, string campaignName)
        {
            this.Repository.TryUpdateEntity(
                context,
                campaignId,
                new List<EntityProperty> { new EntityProperty(BillingActivityNames.IsBillingApproved, true, PropertyFilter.Extended) });

            Console.WriteLine(
                "Billing approval forced for Campaign Name: {0}. Warning - customer billing information has not been verified.".FormatInvariant(
                (string)campaignName));
        }

        /// <summary>Force test billing approval of the campaign without going through the billing activies.</summary>
        /// <param name="campaignId">Campaign entity Id.</param>
        private void ForceTestApproval(EntityId campaignId)
        {
            var context = new RequestContext
            {
                EntityFilter = new RepositoryEntityFilter(true, false, true, false),
            };
            this.ForceApproval(context, campaignId, "Not Supplied");
        }

        /// <summary>Initialize runtime dependencies.</summary>
        /// <param name="arguments">The command-line arguments.</param>
        private void Initialize(ProcessPaymentArgs arguments)
        {
            var logFilePath = @"C:\logs\ReportRuns.log";
            if (arguments.LogFile != null)
            {
                logFilePath = arguments.LogFile.FullName;
            }

            LogManager.Initialize(new[]
                {
                    new FileLogger(logFilePath)
                });

            this.Repository = RuntimeIocContainer.Instance.Resolve<IEntityRepository>();
            this.PaymentProcessor = RuntimeIocContainer.Instance.Resolve<IPaymentProcessor>();
            this.UserAccessRepository = RuntimeIocContainer.Instance.Resolve<IUserAccessRepository>();
        }
    }
}