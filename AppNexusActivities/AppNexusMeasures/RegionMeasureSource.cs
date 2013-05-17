//-----------------------------------------------------------------------
// <copyright file="RegionMeasureSource.cs" company="Rare Crowds Inc">
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
    /// <summary>AppNexus region measure source</summary>
    internal class RegionMeasureSource : GeotargetingMeasureSourceBase
    {
        /// <summary>Measure subtype for metro code measures</summary>
        public const string TargetingSubType = "region";

        /// <summary>Name of the iso3166-2.csv resource</summary>
        /// <remarks>
        /// US and Canada ISO 3166-2 "Subcountry codes" supported
        /// by MaxMind (used by AppNexus for geotargeting).
        /// </remarks>
        /// <seealso href="https://wiki.appnexus.com/display/api/Profile+Service"/>
        /// <seealso href="http://www.maxmind.com/app/iso3166_2"/>
        private const string RegionCsvResourceName = "AppNexusActivities.Measures.Resources.regions.csv";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 3;

        /// <summary>Name for the regions measure source</summary>
        private const string MeasureSourceName = "regions";

        /// <summary>Initializes a new instance of the RegionMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public RegionMeasureSource(IEntity[] entities)
            : base(MeasureIdPrefix, MeasureSourceName, entities)
        {
        }

        /// <summary>
        /// Gets a value indicating whether cache updates should be made asynchronously
        /// </summary>
        public override bool AsyncUpdate
        {
            get { return false; }
        }

        /// <summary>Gets the targeting subtype for city measures</summary>
        protected override string MeasureSubType
        {
            get { return TargetingSubType; }
        }

        /// <summary>Gets the display name for the measure source subcategory</summary>
        protected override string SubCategoryDisplayName
        {
            get { return "Regions"; }
        }

        /// <summary>Fetch the latest region measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            const int RegionCountry = 0;
            const int RegionName = 1;
            const int RegionCode = 2;
            var assembly = Assembly.GetExecutingAssembly();
            using (var regionsCsv = assembly.GetManifestResourceStream(RegionCsvResourceName))
            {
                if (regionsCsv == null)
                {
                    var message = "Unable to load embedded region table '{0}' from assembly '{1}'"
                        .FormatInvariant(RegionCsvResourceName, assembly.FullName);
                    throw new InvalidOperationException(message);
                }

                // Create an array of strings from the CSV consisting of the ISO 3166
                // country, region name and ISO 3166 country:region pair.
                // Trigger evaluation with IEnumerable.ToArray().
                var regions = CsvParser.Parse(regionsCsv)
                    .Select(
                        record => new[]
                        {
                            record["iso 3166 country"],
                            record["name"],
                            "{0}:{1}".FormatInvariant(
                                record["iso 3166 country"],
                                record["iso 3166-2 region"])
                        })
                    .ToArray();

                // Sort by region code and create a measure map using
                // the sorted index to create the measure id.
                var measures = regions
                    .OrderBy(region => region[RegionCode])
                    .Zip(Enumerable.Range(1, regions.Count()))
                    .ToDictionary(
                        region => this.GetMeasureId(region.Item2),
                        region => this.CreateAppNexusGeotargetingMeasure(
                            region.Item1[RegionCode],
                            region.Item1[RegionCountry],
                            region.Item1[RegionName]));
                
                return new MeasureMapCacheEntry
                {
                    Expiry = DateTime.MaxValue,
                    MeasureMapJson = JsonConvert.SerializeObject(measures)
                };
            }
        }
    }
}
