//-----------------------------------------------------------------------
// <copyright file="AppNexusLegacyMeasureSourceProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;
////using EntityActivities;

namespace AppNexusActivities.Measures
{
    /// <summary>Provider of legacy AppNexus measure sources</summary>
    public class AppNexusLegacyMeasureSourceProvider : IMeasureSourceProvider
    {
        /// <summary>Name of the embedded JSON legacy measure map</summary>
        private const string LegacyMeasureMapResourceName = "AppNexusActivities.Measures.Resources.LegacyMeasureMap.js";

        /// <summary>Gets the delivery network designation</summary>
        public DeliveryNetworkDesignation DeliveryNetwork
        {
            get { return DeliveryNetworkDesignation.AppNexus; }
        }

        /// <summary>Gets the measure source provider version</summary>
        public int Version
        {
            get { return 0; }
        }

        /// <summary>Gets the AppNexus measure sources.</summary>
        /// <param name="context">
        /// Context objects needed for creating the sources.
        /// Expects a CompanyEntity and a CampaignEntity.
        /// </param>
        /// <returns>The measure sources</returns>
        public IEnumerable<IMeasureSource> GetMeasureSources(params object[] context)
        {
            var companyEntity = context.OfType<CompanyEntity>().FirstOrDefault();
            if (companyEntity == null)
            {
                throw new ArgumentException("Missing CompanyEntity", "context");
            }

            var campaignEntity = context.OfType<CampaignEntity>().FirstOrDefault();
            if (campaignEntity == null)
            {
                throw new ArgumentException("Missing CampaignEntity", "context");
            }

            // Get the entity measure sources
            var sources = new List<IMeasureSource>(
                new IEntity[] { companyEntity, campaignEntity }
                .Select(entity => entity.GetMeasureSource())
                .Where(source => source != null));

            // Get the embedded legacy measure map source
            var legacyMeasures = new EmbeddedJsonMeasureSource(
                Assembly.GetExecutingAssembly(),
                LegacyMeasureMapResourceName);
            sources.Add(legacyMeasures);

            return sources;
        }
    }
}
