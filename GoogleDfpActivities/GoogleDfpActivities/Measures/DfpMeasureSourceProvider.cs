//-----------------------------------------------------------------------
// <copyright file="DfpMeasureSourceProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using DynamicAllocationUtilities;

namespace GoogleDfpActivities.Measures
{
    /// <summary>Provider of Google DFP measure sources</summary>
    public class DfpMeasureSourceProvider : IMeasureSourceProvider
    {
        /// <summary>Google DFP measure source types</summary>
        private readonly IEnumerable<Type> dfpMeasureSourceTypes;

        /// <summary>Initializes a new instance of the DfpMeasureSourceProvider class</summary>
        public DfpMeasureSourceProvider()
        {
            try
            {
                this.dfpMeasureSourceTypes =
                    Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t =>
                        typeof(DfpMeasureSourceBase).IsAssignableFrom(t) &&
                        !t.IsAbstract);
            }
            catch (ReflectionTypeLoadException e)
            {
                throw new DeliveryNetworkExporterException(
                    e,
                    "Error loading measure source types: {0}\n{1}",
                    e,
                    string.Join("\n", e.LoaderExceptions.Select(le => le.ToString())));
            }
        }

        /// <summary>Gets the delivery network the measure sources are for</summary>
        public DeliveryNetworkDesignation DeliveryNetwork
        {
            get { return DeliveryNetworkDesignation.GoogleDfp; }
        }

        /// <summary>Gets the version of the measure source provider</summary>
        public int Version
        {
            get { return 1; }
        }

        /// <summary>Gets the DFP measure sources.</summary>
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
            
            // Create instances of the Google DFP measure sources
            var args = new object[] { companyEntity, campaignEntity };
            var dfpSources = this.dfpMeasureSourceTypes
                .Select(t => Activator.CreateInstance(t, args))
                .Cast<IMeasureSource>();

            // Get the entity measure sources
            var entitySources =
                new IEntity[] { companyEntity, campaignEntity }
                .Select(entity => entity.GetMeasureSource())
                .Where(source => source != null);

            // Return both sets of measure sources
            return dfpSources.Concat(entitySources);
        }
    }
}
