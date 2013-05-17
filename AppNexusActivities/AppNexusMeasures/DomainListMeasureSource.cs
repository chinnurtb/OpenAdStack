//-----------------------------------------------------------------------
// <copyright file="DomainListMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DataAccessLayer;
using DynamicAllocation;
using Newtonsoft.Json;
using Utilities.Serialization;

namespace AppNexusActivities.Measures
{
    /// <summary>AppNexus inventory measure source</summary>
    internal class DomainListMeasureSource : AppNexusMeasureSourceBase
    {
        /// <summary>Measure type for inventory measures</summary>
        public const string TargetingType = "domainlist";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 11;

        /// <summary>Name for the inventory measure source</summary>
        private const string MeasureSourceName = "domainlist";

        /// <summary>Initializes a new instance of the DomainListMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public DomainListMeasureSource(IEntity[] entities)
            : base(MeasureIdPrefix, MeasureSourceName, entities)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Domain Lists"; }
        }

        /// <summary>Gets the targeting type</summary>
        protected override string MeasureType
        {
            get { return TargetingType; }
        }

        /// <summary>Gets a value indicating whether this measure source is "online"</summary>
        /// <remarks>Only Online sources can use the AppNexusClient</remarks>
        protected override bool Online
        {
            get { return true; }
        }

        /// <summary>Fetch the latest inventory measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var domainLists = this.AppNexusClient.GetMemberDomainLists();
            if (domainLists == null)
            {
                throw new InvalidOperationException("Unable to get domain lists from AppNexus.");
            }

            var measures =
                new[] { "Include", "Exclude" }
                .SelectMany(action =>
                    domainLists.ToDictionary(
                        category =>
                            this.GetMeasureId(
                                Convert.ToInt32(category["id"], CultureInfo.InvariantCulture) +
                                ((action == "Include" ? 1 : 2) * 1000000)),
                        category =>
                            this.CreateAppNexusMeasure(
                                new[]
                                {
                                    action,
                                    category["name"]
                                },
                                "{0}:{1}".FormatInvariant(action, category["id"]))))
                .ToDictionary();

            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.MaxValue,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }
    }
}
