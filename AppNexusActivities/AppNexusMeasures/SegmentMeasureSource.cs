//-----------------------------------------------------------------------
// <copyright file="SegmentMeasureSource.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using Newtonsoft.Json;
using Utilities.Serialization;
using Utilities.Storage;

namespace AppNexusActivities.Measures
{
    /// <summary>AppNexus segment measure source</summary>
    internal class SegmentMeasureSource : AppNexusMeasureSourceBase
    {
        /// <summary>Measure type for category measures</summary>
        public const string TargetingType = "segment";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 10;

        /// <summary>Name for the segment measure source</summary>
        private const string MeasureSourceName = "segments";

        /// <summary>Name of the persistent dictionary where data costs are stored</summary>
        private const string DataCostStoreName = "datacosts";

        /// <summary>Namespace where embedded data cost CSVs are located</summary>
        private const string EmbeddedResourceNamespace = "AppNexusActivities.Measures.Resources";

        /// <summary>Strings used to split raw segment names to create hierarchies</summary>
        private static readonly string[] RawSegmentNameSplits = new[] { ">", "-->", " - ", "\n", "\r", "\t", ":" };

        /// <summary>Measure columns containing data cost values</summary>
        private static readonly string[] DataCostColumns = new[]
        {
            MeasureValues.DataProvider,
            MeasureValues.DataCost,
            MeasureValues.MinCostPerMille,
            MeasureValues.PercentOfMedia,
            MeasureValues.SubType,
        };

        /// <summary>Measures columns which measures with datacosts are required to have values for at least one.</summary>
        private static readonly string[] RequiredDataCostColumns = new[]
        {
            MeasureValues.DataCost,
            MeasureValues.MinCostPerMille,
            MeasureValues.PercentOfMedia,
        };

        /// <summary>Supplementary columns from segment values</summary>
        private static readonly string[] SupplementaryColumns = new[]
        {
            SegmentValues.MemberId,
            SegmentValues.Provider,
            SegmentValues.Code,
        };

        /// <summary>Backing field for SegmentDataCosts</summary>
        private IDictionary<long, IDictionary<string, object>> segmentDataCosts;

        /// <summary>Backing field for SegmentNameOverrides</summary>
        private IDictionary<long, string> segmentNameOverrides;

        /// <summary>Backing field for DataCostsStore</summary>
        private IPersistentDictionary<string> dataCostStore;

        /// <summary>Backing field for DataProviders</summary>
        private string[] dataProviders;

        /// <summary>Initializes a new instance of the SegmentMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public SegmentMeasureSource(IEntity[] entities)
            : base(MeasureIdPrefix, MeasureSourceName, entities)
        {
        }

        /// <summary>Initializes a new instance of the SegmentMeasureSource class</summary>
        /// <param name="campaignOwner">Campaign Owner UserEntity (for config/auth)</param>
        /// <param name="config">Configuration to use</param>
        public SegmentMeasureSource(UserEntity campaignOwner, IConfig config)
            : base(MeasureIdPrefix, MeasureSourceName, campaignOwner, config)
        {
        }

        /// <summary>Gets the data providers to include segments from</summary>
        internal string[] DataProviders
        {
            get
            {
                return this.dataProviders = this.dataProviders ??
                    this.Config.GetValue("AppNexus.DataProviders")
                    .Split('|')
                    .Select(s => s.ToUpperInvariant().Trim())
                    .ToArray();
            }
        }

        /// <summary>Gets the persistent dictionary where data costs are stored</summary>
        internal IPersistentDictionary<string> DataCostStore
        {
            get
            {
                return this.dataCostStore =
                    this.dataCostStore ??
                    PersistentDictionaryFactory.CreateDictionary<string>(DataCostStoreName);
            }
        }

        /// <summary>Gets the name of the segment data costs CSV</summary>
        internal string SegmentDataCostsCsvName
        {
            get
            {
                return "SegmentDataCosts-{0}.csv".FormatInvariant(this.AppNexusClient.Id);
            }
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Segments"; }
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

        /// <summary>Gets the default data provider</summary>
        protected override string DefaultDataProvider
        {
            get { return MeasureInfo.DataProviderUnknown; }
        }

        /// <summary>
        /// Gets how log until cached segment measures are expired
        /// </summary>
        private TimeSpan CacheExpiryTime
        {
            get { return this.Config.GetTimeSpanValue("AppNexus.SegmentCacheExpiry"); }
        }

        /// <summary>
        /// Gets a value indicating whether segment data costs are required
        /// </summary>
        private bool DataCostsRequired
        {
            get { return this.Config.GetBoolValue("AppNexus.SegmentDataCostsRequired"); }
        }

        /// <summary>Gets the segment data costs from the segment data csv</summary>
        /// <remarks>Overrides provider data costs for individual segments</remarks>
        private IDictionary<long, IDictionary<string, object>> SegmentDataCosts
        {
            get
            {
                if (this.segmentDataCosts == null)
                {
                    var segmentData = this.GetSegmentDataCsv(this.SegmentDataCostsCsvName);
                    if (segmentData == null)
                    {
                        return null;
                    }

                    decimal value;
                    this.segmentDataCosts = segmentData
                        .ToDictionary(
                            dataCost =>
                                Convert.ToInt64(
                                    dataCost[AppNexusMeasureValues.AppNexusId],
                                    CultureInfo.InvariantCulture),
                            dataCost =>
                                (IDictionary<string, object>)
                                DataCostColumns.ToDictionary(
                                    col => col,
                                    col =>
                                        (!dataCost.ContainsKey(col) ||
                                        string.IsNullOrWhiteSpace(dataCost[col]) ||
                                        ((string)dataCost[col]).ToLowerInvariant() == "null") ? null :
                                        decimal.TryParse((string)dataCost[col], out value) ? (object)value : dataCost[col]));
                }

                return this.segmentDataCosts;
            }
        }

        /// <summary>Gets the segment name overrides from the segment data csv</summary>
        private IDictionary<long, string> SegmentNameOverrides
        {
            get
            {
                if (this.segmentNameOverrides == null)
                {
                    var segmentData = this.GetSegmentDataCsv(this.SegmentDataCostsCsvName);
                    if (segmentData == null)
                    {
                        return null;
                    }

                    this.segmentNameOverrides = segmentData
                        .Where(segment =>
                            !string.IsNullOrWhiteSpace(segment[MeasureValues.DisplayName]))
                        .ToDictionary(
                            segment =>
                                Convert.ToInt64(
                                    segment[AppNexusMeasureValues.AppNexusId],
                                    CultureInfo.InvariantCulture),
                            segment =>
                                (string)segment[MeasureValues.DisplayName]);
                }

                return this.segmentNameOverrides;
            }
        }

        /// <summary>Create CSV template for segment data costs</summary>
        /// <param name="clean">Whether to include existing data cost/name override values</param>
        /// <returns>Segment data costs CSV template</returns>
        public string CreateSegmentDataCostCsvTemplate(bool clean)
        {
            // Get the measures for all segments (data costs not required)
            var measures = this.CreateMeasuresFromSegments(false, !clean);

            // Compose the CSV
            var columns = new[]
                {
                    AppNexusMeasureValues.AppNexusId,
                    MeasureValues.DisplayName,
                    MeasureValues.DataProvider,
                    MeasureValues.DataCost,
                    MeasureValues.MinCostPerMille,
                    MeasureValues.PercentOfMedia,
                    SegmentValues.MemberId,
                    SegmentValues.Provider,
                    SegmentValues.Code,
                    MeasureValues.SubType,
                };
            var values =
                new[] { columns }
                .Concat(
                    measures.Values
                    .Select(measure => columns
                        .Select(col =>
                            measure.ContainsKey(col) ?
                            measure[col] : null)
                        .Select(val => val ?? string.Empty)
                        .Select(val => val.ToString().Replace(',', ' ').Trim())
                        .ToArray()));
            var rows = values.Select(row =>
                string.Join(",", row));
            return string.Join("\n", rows);
        }

        /// <summary>Checks if the measure has data costs</summary>
        /// <param name="measure">The measure</param>
        /// <returns>
        /// True if the measure has a data provider specified and
        /// at least one data cost value; otherwise, false.
        /// </returns>
        internal static bool HasDataCosts(IDictionary<string, object> measure)
        {
            if (!measure.ContainsKey(MeasureValues.DataProvider) ||
                measure[MeasureValues.DataProvider] as string == null ||
                ((string)measure[MeasureValues.DataProvider]) == MeasureInfo.DataProviderUnknown)
            {
                return false;
            }

            try
            {
                return RequiredDataCostColumns
                    .Any(cost =>
                        measure.ContainsKey(cost) &&
                        Convert.ToDouble(measure[cost], CultureInfo.InvariantCulture) != 0.0);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>Creates a measure that represents an AppNexus segment</summary>
        /// <param name="segment">The AppNexus segment</param>
        /// <param name="dataCosts">Lookup table of segments data costs</param>
        /// <param name="nameOverrides">Lookup table of segment name overrides</param>
        /// <param name="dataCostRequired">Whether to only include segments with data costs</param>
        /// <returns>The measure</returns>
        internal KeyValuePair<long, IDictionary<string, object>> CreateSegmentMeasure(
            IDictionary<string, object> segment,
            IDictionary<long, IDictionary<string, object>> dataCosts,
            IDictionary<long, string> nameOverrides,
            bool dataCostRequired)
        {
            var appNexusId = Convert.ToInt64(segment[SegmentValues.Id], CultureInfo.InvariantCulture);
            if (!segment.ContainsKey(SegmentValues.Name) || string.IsNullOrWhiteSpace(segment[SegmentValues.Name] as string))
            {
                // Segments without names will be ignored.
                return new KeyValuePair<long, IDictionary<string, object>>(0, null);
            }

            var segmentName = (string)segment[SegmentValues.Name];
            var measureId = this.GetMeasureId(appNexusId);

            IDictionary<string, object> segmentDataCost = null;
            if (dataCosts == null || !dataCosts.ContainsKey(appNexusId))
            {
                // No data costs available.
                if (dataCostRequired)
                {
                    // Only use segments with known data costs.
                    // Null value will be filtered out.
                    return new KeyValuePair<long, IDictionary<string, object>>(measureId, null);
                }
            }
            else
            {
                segmentDataCost = dataCosts[appNexusId];
            }

            // Use the name from the data costs if available.
            // Otherwise, break down the segment name to build a hierarchy
            var nameParts =
                (nameOverrides != null && nameOverrides.ContainsKey(appNexusId) ?
                    this.segmentNameOverrides[appNexusId] : segmentName)
                .Split(RawSegmentNameSplits, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());

            // Get the subtype from the data costs "SubType" column
            var subType =
                segmentDataCost != null && segmentDataCost.ContainsKey(MeasureValues.SubType) ?
                segmentDataCost[MeasureValues.SubType] as string : null;

            // Create the AppNexus measure and add additional data
            var measure = this.CreateAppNexusMeasure(
                (segmentDataCost != null ? new[] { segmentDataCost[MeasureValues.DataProvider] } : new string[0])
                .Concat(nameParts)
                .ToArray(),
                segment[SegmentValues.Id],
                subType);
            measure[AppNexusMeasureValues.AppNexusCode] = segment[SegmentValues.Code];

            // Copy the data costs for the segment/data provider
            foreach (var column in DataCostColumns)
            {
                measure[column] =
                    segmentDataCost != null ? segmentDataCost[column] : // Value from data costs
                    measure.ContainsKey(column) ? measure[column] :     // Value from defaults
                    null;                                               // Null
            }

            // Require at least one data cost column to be non-null if data costs required
            if (dataCostRequired && !HasDataCosts(measure))
            {
                return new KeyValuePair<long, IDictionary<string, object>>(measureId, null);
            }

            // Add supplementary columns (where present)
            measure.Add(SupplementaryColumns
                .Where(col => segment.ContainsKey(col) && !(segment[col] is string && string.IsNullOrWhiteSpace((string)segment[col])))
                .Select(col => new KeyValuePair<string, object>(col, segment[col])));

            // Return the measure with its measure id
            return new KeyValuePair<long, IDictionary<string, object>>(measureId, measure);
        }

        /// <summary>Fetch the latest segment measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var measures = this.CreateMeasuresFromSegments(this.DataCostsRequired, true);
            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.UtcNow + this.CacheExpiryTime,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }

        /// <summary>Creates measures from AppNexus segments</summary>
        /// <param name="dataCostsRequired">
        /// Whether to only include segments with data costs defined.
        /// </param>
        /// <param name="includeDataCosts">
        /// Whether to include name overrides and datacosts.
        /// </param>
        /// <returns>the segment measures</returns>
        private IDictionary<long, IDictionary<string, object>> CreateMeasuresFromSegments(
            bool dataCostsRequired,
            bool includeDataCosts)
        {
            var segments = this.AppNexusClient.GetMemberSegments();
            if (segments == null)
            {
                throw new InvalidOperationException("Unable to get segments from AppNexus.");
            }

            if (dataCostsRequired && this.SegmentDataCosts == null)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "No segment data costs found for '{0}' ({1}). Segment measures will not be available.",
                    this.AppNexusClient.Id,
                    this.SegmentDataCostsCsvName);
                return new Dictionary<long, IDictionary<string, object>>();
            }

            var dataCosts = includeDataCosts ? this.SegmentDataCosts : new Dictionary<long, IDictionary<string, object>>();
            var nameOverrides = includeDataCosts ? this.SegmentNameOverrides : new Dictionary<long, string>();
            return segments
                .Select(segment =>
                    this.CreateSegmentMeasure(
                        segment,
                        dataCosts,
                        nameOverrides,
                        dataCostsRequired))
                .Where(measure =>
                    measure.Key > 0 &&
                    measure.Value != null)
                .ToDictionary();
        }

        /// <summary>Gets the specified segment data CSV</summary>
        /// <remarks>
        /// Attempts to get the data from the segment data store. If no data exists
        /// for the CSV then attempts to populate the store from embedded resources.
        /// </remarks>
        /// <param name="segmentDataCsvName">Name of the segment data CSV to get</param>
        /// <returns>The data costs values</returns>
        private IEnumerable<IDictionary<string, string>> GetSegmentDataCsv(string segmentDataCsvName)
        {
            try
            {
                if (!this.DataCostStore.ContainsKey(segmentDataCsvName))
                {
                    // Upload the embedded data cost CSV (if one exists)
                    this.UploadEmbeddedDataCosts(segmentDataCsvName);
                }

                var dataCostsCsv = this.DataCostStore[segmentDataCsvName];
                return CsvParser.Parse(dataCostsCsv);
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Unable to get AppNexus segment data from '{0}': {1}",
                    segmentDataCsvName,
                    e);
                throw;
            }
        }

        /// <summary>Uploads the embedded data costs CSV to the data cost store</summary>
        /// <param name="dataCostCsvName">Name of the data costs CSV to upload</param>
        private void UploadEmbeddedDataCosts(string dataCostCsvName)
        {
            var resourceName = "{0}.{1}".FormatInvariant(EmbeddedResourceNamespace, dataCostCsvName);
            using (var res = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (res == null)
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Embedded data costs for '{0}' not found ({1})",
                        dataCostCsvName,
                        resourceName);
                    return;
                }

                using (var reader = new StreamReader(res))
                {
                    this.DataCostStore.Add(
                        dataCostCsvName,
                        reader.ReadToEnd());
                }
            }
        }

        /// <summary>Names of AppNexus segment values</summary>
        internal static class SegmentValues
        {
            /// <summary>segment id</summary>
            public const string Id = "id";

            /// <summary>segment code</summary>
            public const string Code = "code";

            /// <summary>segment name</summary>
            public const string Name = "short_name";

            /// <summary>segment member id</summary>
            public const string MemberId = "member_id";

            /// <summary>segment subtype</summary>
            public const string SubType = "subType";

            /// <summary>segment provider</summary>
            public const string Provider = "provider";
        }
    }
}
