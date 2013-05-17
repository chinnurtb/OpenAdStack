//-----------------------------------------------------------------------
// <copyright file="AppNexusMeasureSourceProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationUtilities;

namespace Utilities.AllocationSimulator
{
    /// <summary>Provider of AppNexus measure sources</summary>
    public class AppNexusMeasureSourceProvider : IMeasureSourceProvider
    {
        /// <summary>Name of the embedded JSON measure map</summary>
        private const string AppNexusMeasureMapResourceNameFormat = "AllocationSimulator.Resources.MeasureMap.AppNexus.{0}.js";

        /// <summary>The target profile</summary>
        private readonly string TargetProfile;

        /// <summary>
        /// Initializes a new instance of the AppNexusMeasureSourceProvider class.
        /// </summary>
        /// <param name="targetProfile">The target profile</param>
        public AppNexusMeasureSourceProvider(string targetProfile)
        {
            this.TargetProfile = targetProfile;
        }

        /// <summary>Gets the delivery network designation</summary>
        public DeliveryNetworkDesignation DeliveryNetwork
        {
            get { return DeliveryNetworkDesignation.AppNexus; }
        }

        /// <summary>Gets the measure source provider version</summary>
        public int Version
        {
            get { return 1; }
        }

        /// <summary>Gets the name of the AppNexusMeasureMapResource</summary>
        internal string AppNexusMeasureMapResourceName
        {
            get { return AppNexusMeasureMapResourceNameFormat.FormatInvariant(this.TargetProfile); }
        }

        /// <summary>Gets the AppNexus measure sources.</summary>
        /// <param name="context">
        /// Context objects needed for creating the sources.
        /// </param>
        /// <returns>The measure sources</returns>
        public IEnumerable<IMeasureSource> GetMeasureSources(params object[] context)
        {
            // Get the embedded measure sources
            var sources = new List<IMeasureSource>();
            sources.Add(new EmbeddedJsonMeasureSource(
                    Assembly.GetExecutingAssembly(),
                    this.AppNexusMeasureMapResourceName));

            // Add company and campaign custom measures (if provided)
            var companyEntity = context.OfType<CompanyEntity>().FirstOrDefault();
            if (companyEntity != null && companyEntity.GetMeasureSource() != null)
            {
                sources.Add(companyEntity.GetMeasureSource());
            }

            var campaignEntity = context.OfType<CampaignEntity>().FirstOrDefault();
            if (campaignEntity != null && campaignEntity.GetMeasureSource() != null)
            {
                sources.Add(campaignEntity.GetMeasureSource());
            }

            return sources;
        }
    }
}
