//-----------------------------------------------------------------------
// <copyright file="AppNexusMeasureSourceProvider.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using DynamicAllocation;
using DynamicAllocationUtilities;

namespace AppNexusActivities.Measures
{
    /// <summary>Provider of AppNexus measure sources</summary>
    public class AppNexusMeasureSourceProvider : IMeasureSourceProvider
    {
        /// <summary>AppNexus measure source types</summary>
        private readonly IEnumerable<Type> measureSourceTypes;

        /// <summary>Initializes a new instance of the AppNexusMeasureSourceProvider class</summary>
        public AppNexusMeasureSourceProvider()
        {
            this.measureSourceTypes = 
                Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    typeof(AppNexusMeasureSourceBase).IsAssignableFrom(t) &&
                    !t.IsAbstract);
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

            var campaignOwner = context.OfType<UserEntity>().FirstOrDefault();
            if (campaignOwner == null)
            {
                throw new ArgumentException("Missing UserEntity", "context");
            }

            // Get the entity measure sources
            var entitySources =
                new IEntity[] { companyEntity, campaignEntity }
                .Select(entity => entity.GetMeasureSource())
                .Where(source => source != null);

            // Create instances of the embedded JSON measure sources
            var embeddedJsonMeasureMapResources = new[]
            {
                "AppNexusActivities.Measures.Resources.AppNexusBuiltin.js",
            };
            var jsonSources =
                embeddedJsonMeasureMapResources
                .Select(resourceName =>
                    new EmbeddedJsonMeasureSource(
                        Assembly.GetExecutingAssembly(),
                        resourceName));

            // Create instances of the AppNexus dynamic measure sources
            var entities = new IEntity[] { companyEntity, campaignEntity, campaignOwner };
            var apnxSources = this.measureSourceTypes
                .Select(t => Activator.CreateInstance(t, new object[] { entities }))
                .Cast<IMeasureSource>();

            // Return all the sources
            return
                entitySources
                .Concat(jsonSources)
                .Concat(apnxSources);
        }
    }
}
