//-----------------------------------------------------------------------
// <copyright file="MetroCodeMeasureSource.cs" company="Rare Crowds Inc">
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
    /// <summary>AppNexus metro code measure source</summary>
    internal class MetroCodeMeasureSource : GeotargetingMeasureSourceBase
    {
        /// <summary>Measure subtype for metro code measures</summary>
        public const string TargetingSubType = "dma";

        /// <summary>Name of the metrocodes.csv resource</summary>
        /// <remarks>
        /// Metro codes supported by MaxMind (used by AppNexus for geotargeting).
        /// </remarks>
        /// <seealso href="https://wiki.appnexus.com/display/api/Profile+Service"/>
        /// <seealso href="http://www.maxmind.com/app/metro_code"/>
        /// <seealso href="https://developers.google.com/adwords/api/docs/appendix/metrocodes.csv"/>
        private const string MetroCodeCsvResourceName = "AppNexusActivities.Measures.Resources.metrocodes.csv";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 2;

        /// <summary>Name for the metro code measure source</summary>
        private const string MeasureSourceName = "metrocodes";

        /// <summary>Initializes a new instance of the MetroCodeMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public MetroCodeMeasureSource(IEntity[] entities)
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
            get { return "Metro Codes"; }
        }

        /// <summary>Fetch the latest metro code measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var metroCodesCsv = assembly.GetManifestResourceStream(MetroCodeCsvResourceName))
            {
                if (metroCodesCsv == null)
                {
                    var message = "Unable to load embedded metro code table '{0}' from assembly '{1}'"
                        .FormatInvariant(MetroCodeCsvResourceName, assembly.FullName);
                    throw new InvalidOperationException(message);
                }

                var measures = new Dictionary<long, IDictionary<string, object>>();
                foreach (var record in CsvParser.Parse(metroCodesCsv))
                {
                    var metroCode = Convert.ToInt16(record["MetroCode"], CultureInfo.InvariantCulture);

                    // Metro codes may occur multiple times listed under different provinces.
                    // Add a 3 digit serial number after the metro code for unique measure ids.
                    var prefix = metroCode * 1000;
                    var serial = 0;
                    long measureId;
                    do
                    {
                        measureId = this.GetMeasureId(prefix + serial++);
                    }
                    while (measures.ContainsKey(measureId));

                    // Create a measure from the record
                    var measure = this.CreateAppNexusGeotargetingMeasure(
                        metroCode,
                        record["ProvinceName"],
                        record["MetroName"]);

                    // Add to the metro code measures
                    measures.Add(measureId, measure);
                }
                
                return new MeasureMapCacheEntry
                {
                    Expiry = DateTime.MaxValue,
                    MeasureMapJson = JsonConvert.SerializeObject(measures)
                };
            }
        }
    }
}
