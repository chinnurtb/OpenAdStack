//-----------------------------------------------------------------------
// <copyright file="AgeRangeMeasureSource.cs" company="Rare Crowds Inc">
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
    /// <summary>AppNexus age range measure source</summary>
    internal class AgeRangeMeasureSource : AppNexusMeasureSourceBase
    {
        /// <summary>Measure type for age range measures</summary>
        public const string TargetingType = "demographic";

        /// <summary>Measure subtype for age range measures</summary>
        public const string TargetingSubType = "agerange";

        /// <summary>Name of the ageranges.csv resource</summary>
        private const string AgeRangeCsvResourceName = "AppNexusActivities.Measures.Resources.ageranges.csv";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 7;

        /// <summary>Name for the age range measure source</summary>
        private const string MeasureSourceName = "ageranges";

        /// <summary>Initializes a new instance of the AgeRangeMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public AgeRangeMeasureSource(IEntity[] entities)
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

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Demographic"; }
        }

        /// <summary>Gets the subcategory display name</summary>
        protected override string SubCategoryDisplayName
        {
            get { return "Age Ranges"; }
        }

        /// <summary>Gets the targeting type</summary>
        protected override string MeasureType
        {
            get { return TargetingType; }
        }

        /// <summary>Fetch the latest age range measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var ageRangesCsv = assembly.GetManifestResourceStream(AgeRangeCsvResourceName))
            {
                if (ageRangesCsv == null)
                {
                    var message = "Unable to load embedded age range table '{0}' from assembly '{1}'"
                        .FormatInvariant(AgeRangeCsvResourceName, assembly.FullName);
                    throw new InvalidOperationException(message);
                }

                // Parse the age range CSV first to tuples of min/max ages,
                // then to a dictionary of age range ids and age range strings
                // and finally to a dictionary of measure ids and measures.
                // Age range ids are 6 digits: 3 for min and 3 for max.
                // Example: 35-39 -> 035039
                var measures = CsvParser.Parse(ageRangesCsv)
                    .Select(record =>
                        new Tuple<int, int>(
                            Convert.ToInt16(record["min age"], CultureInfo.InvariantCulture),
                            Convert.ToInt16(record["max age"], CultureInfo.InvariantCulture)))
                    .ToDictionary(
                        range => (range.Item1 * 1000) + range.Item2,
                        range => "{0}-{1}".FormatInvariant(range.Item1, range.Item2))
                    .ToDictionary(
                        kvp => this.GetMeasureId(kvp.Key),
                        kvp => this.CreateAppNexusMeasure(
                            new[]
                            {
                                "Ages {0}".FormatInvariant(kvp.Value)
                            },
                            kvp.Value,
                            TargetingSubType));

                // Add special values for allow/exclude unknown
                var unknownAgeId = 1;
                measures.Add(
                    new[] { "Allow", "Exclude" }
                    .ToDictionary(
                        unknown => this.GetMeasureId(unknownAgeId++),
                        unknown => this.CreateAppNexusMeasure(
                            new[]
                            {
                                "Age Ranges",
                                "{0} Unknown".FormatInvariant(unknown)
                            },
                            "{0}Unknown".FormatInvariant(unknown),
                            TargetingSubType)));

                return new MeasureMapCacheEntry
                {
                    Expiry = DateTime.MaxValue,
                    MeasureMapJson = JsonConvert.SerializeObject(measures)
                };
            }
        }
    }
}
