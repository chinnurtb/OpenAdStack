// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CreateCampaignReportHandlerFactory.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationActivities;
using EntityUtilities;
using ReportingTools;
using ReportingUtilities;

namespace ReportingActivities
{
    /// <summary>
    /// Factory class for CreateCampaignReportActivity handlers.
    /// </summary>
    public class CreateCampaignReportHandlerFactory : IActivityHandlerFactory
    {
        /// <summary>Initializes a new instance of the <see cref="CreateCampaignReportHandlerFactory"/> class.</summary>
        public CreateCampaignReportHandlerFactory()
        {
            this.CampaignFactory = new DynamicAllocationCampaignFactory();
        }

        /// <summary>Initializes a new instance of the <see cref="CreateCampaignReportHandlerFactory"/> class.</summary>
        /// <param name="campaignFactory">Injection constructor for alternate campaign factory.</param>
        public CreateCampaignReportHandlerFactory(IDynamicAllocationCampaignFactory campaignFactory)
        {
            this.CampaignFactory = campaignFactory;
        }

        /// <summary>Gets the DynamicAllocationCampaign factory</summary>
        internal IDynamicAllocationCampaignFactory CampaignFactory { get; private set; }

        /// <summary>Create the activity handler.</summary>
        /// <param name="request">The activity request.</param>
        /// <param name="context">The activity context.</param>
        /// <returns>An IActivityHandler instance.</returns>
        public IActivityHandler CreateActivityHandler(ActivityRequest request, IDictionary<Type, object> context)
        {
            var companyEntityId = new EntityId(request.Values[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.CampaignEntityId]);
            var buildVerbose = request.Values.ContainsKey(ReportingActivityValues.VerboseReport);
            var saveLegacyConversion = request.Values.ContainsKey(ReportingActivityValues.SaveLegacyConversion);
            var reportType = request.Values[ReportingActivityValues.ReportType];
            var repository = (IEntityRepository)context[typeof(IEntityRepository)];

            // TODO: Generalize to non-DA campaigns
            // TODO: Support other networks and multiple networks
            this.CampaignFactory.BindRuntime(repository);

            var dynamicAllocationCampaign = saveLegacyConversion
                ? this.CampaignFactory.MigrateDynamicAllocationCampaign(companyEntityId, campaignEntityId)
                : this.CampaignFactory.BuildDynamicAllocationCampaign(companyEntityId, campaignEntityId);

            var generatorMap = new Dictionary<DeliveryNetworkDesignation, IReportGenerator>();
            if (dynamicAllocationCampaign.DeliveryNetwork == DeliveryNetworkDesignation.AppNexus)
            {
                var reportGenerator = new AppNexusBillingReport(repository, dynamicAllocationCampaign);
                generatorMap.Add(DeliveryNetworkDesignation.AppNexus, reportGenerator);
            }

            return new CampaignReportHandler(
                repository, generatorMap, companyEntityId, campaignEntityId, buildVerbose, reportType);
        }
    }
}