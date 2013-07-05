// -----------------------------------------------------------------------
// <copyright file="MeasuresDataServiceActivity.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Activities;
using DataAccessLayer;
using DataServiceUtilities;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityUtilities;
using Utilities.Serialization;

namespace DynamicAllocationActivities
{
    /// <summary>DataServiceActivityBase derived class for testing</summary>
    [Name(DynamicAllocationActivityTasks.GetMeasures)]
    [RequiredValues(EntityActivityValues.CompanyEntityId, EntityActivityValues.CampaignEntityId)]
    public class MeasuresDataServiceActivity : DataServiceActivityBase<KeyValuePair<long, IDictionary<string, object>>>
    {
        /// <summary>Display name for unloaded placeholders</summary>
        private const string UnloadedPlaceholderDisplayName = "<loading>";

        /// <summary>Properties to include in JSON results</summary>
        private static readonly string[] JsonIncludeProperties = new[]
        {
            MeasureValues.DisplayName,
            MeasureValues.Type,
            MeasureValues.SubType,
            MeasureValues.DataProvider,
        };

        /// <summary>Gets the activity runtime category</summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.InteractiveFetch; }
        }

        /// <summary>Gets the results path separator</summary>
        protected override char ResultPathSeparator
        {
            get { return ':'; }
        }

        /// <summary>Gets the entity repository</summary>
        private IEntityRepository Repository
        {
            get { return (IEntityRepository)this.Context[typeof(IEntityRepository)]; }
        }

        /// <summary>Checks whether a measure matches the specified cost type</summary>
        /// <param name="costType">
        /// Cost type to check for. This is either one of the data providers, NoCost or Unknown.
        /// </param>
        /// <param name="measure">
        /// The measure to be checked. Not required to have type/subtype values.
        /// </param>
        /// <returns>
        /// True if the measure matches the specified type (and subtype, if specified);
        /// otherwise, false.
        /// </returns>
        internal static bool IsMeasureOfCostType(string costType, IDictionary<string, object> measure)
        {
            return !measure.ContainsKey(MeasureValues.DataProvider) || string.IsNullOrWhiteSpace((string)measure[MeasureValues.DataProvider]) ? false :
                costType.ToLowerInvariant() == ((string)measure[MeasureValues.DataProvider]).ToLowerInvariant();
        }

        /// <summary>Gets the path for a result</summary>
        /// <param name="result">The result</param>
        /// <returns>The result's path</returns>
        protected override string GetResultPath(
            KeyValuePair<long, IDictionary<string, object>> result)
        {
            return ((string)result.Value[MeasureValues.DisplayName])
                .Replace(": ", ":"); // Sanitization required for legacy measure map
        }

        /// <summary>Gets whether the result is loaded</summary>
        /// <param name="result">The result</param>
        /// <returns>Whether the result is loaded</returns>
        protected override bool IsResultLoaded(KeyValuePair<long, IDictionary<string, object>> result)
        {
            return result.Key > 0;
        }

        /// <summary>Gets results based upon the request values</summary>
        /// <param name="requestValues">The request values</param>
        /// <returns>The results</returns>
        protected override KeyValuePair<long, IDictionary<string, object>>[] GetResults(
            IDictionary<string, string> requestValues)
        {
            var companyEntityId = new EntityId(requestValues[EntityActivityValues.CompanyEntityId]);
            var campaignEntityId = new EntityId(requestValues[EntityActivityValues.CampaignEntityId]);

            var dac = new DynamicAllocationCampaign(this.Repository, companyEntityId, campaignEntityId);
            return GetMeasures(dac);
        }

        /// <summary>Formats the results as JSON</summary>
        /// <param name="results">Results to be formatted</param>
        /// <returns>The JSON formatted results</returns>
        protected override string FormatResultsAsJson(
            IDictionary<string, object> results)
        {
            var startTime = DateTime.UtcNow;
            var resultsJson = AppsJsonSerializer.SerializeObject(
            results.Values
            .Cast<KeyValuePair<long, IDictionary<string, object>>>()
            .Where(kvp => kvp.Key > 0)
            .Select(kvp => new KeyValuePair<long, IDictionary<string, object>>(
                kvp.Key,
                kvp.Value.Where(p => JsonIncludeProperties.Contains(p.Key)).ToDictionary()))
            .ToDictionary());
            LogManager.Log(
                LogLevels.Trace,
                "Formatted {0} measures as JSON (duration: {1}s)",
                results.Count,
                (DateTime.UtcNow - startTime).TotalSeconds);
            return resultsJson;
        }

        /// <summary>Formats the results as XML</summary>
        /// <remarks>Not supported by base class</remarks>
        /// <param name="results">Results to be formatted</param>
        /// <param name="subtreePath">Path of the root of the results</param>
        /// <returns>The XML formatted results</returns>
        protected override string FormatResultsAsXml(
            IDictionary<string, object> results,
            string subtreePath)
        {
            var startTime = DateTime.UtcNow;
            var resultsXml =
                DataServiceActivityUtilities.FormatResultsAsDhtmlxTreeGridXml<KeyValuePair<long, IDictionary<string, object>>>(
                results,
                subtreePath,
                r => r.Key.ToString(CultureInfo.InvariantCulture),
                this.ResultPathSeparator);
            LogManager.Log(
                LogLevels.Trace,
                "Formatted {0} measures as XML (duration: {1}s)",
                results.Count,
                (DateTime.UtcNow - startTime).TotalSeconds);
            return resultsXml;
        }

        /// <summary>Determine whether a result should be filtered</summary>
        /// <param name="result">Result to be filtered</param>
        /// <param name="requestValues">Request values</param>
        /// <returns>True if the result should be filtered out; otherwise, false</returns>
        protected override bool FilterResult(
            KeyValuePair<long, IDictionary<string, object>> result,
            IDictionary<string, string> requestValues)
        {
            if (requestValues.ContainsKey(DataServiceActivityValues.Ids))
            {
                // Filter results not matching one of the included ids
                var includeIds = requestValues[DataServiceActivityValues.Ids].Split(',')
                    .Select(id => Convert.ToInt64(id, CultureInfo.InvariantCulture));
                if (!includeIds.Contains(result.Key))
                {
                    return true;
                }
            }

            if (requestValues.ContainsKey(DataServiceActivityValues.Exclude))
            {
                // Filter results matching any of the exclude types
                var excludeTypes = requestValues[DataServiceActivityValues.Exclude].Split(';', ',');
                if (excludeTypes.Any(type => IsMeasureOfType(type, result.Value)))
                {
                    return true;
                }
            }

            if (requestValues.ContainsKey(DynamicAllocationActivityValues.ExcludeCostTypes))
            {
                // Filter results matching any of the exclude cost types
                var excludeCostTypes = requestValues[DynamicAllocationActivityValues.ExcludeCostTypes].Split(';', ',');
                if (excludeCostTypes.Any(costType => IsMeasureOfCostType(costType, result.Value)))
                {
                    return true;
                }
            }

            if (requestValues.ContainsKey(DataServiceActivityValues.Include))
            {
                // Filter results not matching any of the include types
                var includeTypes = requestValues[DataServiceActivityValues.Include].Split(';', ',');
                if (!includeTypes.Any(type => IsMeasureOfType(type, result.Value)))
                {
                    return true;
                }
            }

            if (requestValues.ContainsKey(DynamicAllocationActivityValues.IncludeCostTypes))
            {
                // Filter results not matching any of the include cost types
                var includeCostTypes = requestValues[DynamicAllocationActivityValues.IncludeCostTypes].Split(';', ',');
                if (!includeCostTypes.Any(costType => IsMeasureOfCostType(costType, result.Value)))
                {
                    return true;
                }
            }

            // Do not filter results passing both filters (or if no filters are defined)
            return false;
        }

        /// <summary>Checks whether a measure matches the specified type</summary>
        /// <param name="type">
        /// Type to check for. This may also include an optional subtype.
        /// Ex: "Geotargeting" or "Geotargeting.DMA"
        /// </param>
        /// <param name="measure">
        /// The measure to be checked. Not required to have type/subtype values.
        /// </param>
        /// <returns>
        /// True if the measure matches the specified type (and subtype, if specified);
        /// otherwise, false.
        /// </returns>
        private static bool IsMeasureOfType(string type, IDictionary<string, object> measure)
        {
            // Get the measure type and subtype (if specified)
            var measureType = measure.ContainsKey(MeasureValues.Type) ?
                ((string)measure[MeasureValues.Type] ?? string.Empty).ToLowerInvariant() :
                string.Empty;
            var measureSubType = measure.ContainsKey(MeasureValues.SubType) ?
                ((string)measure[MeasureValues.SubType] ?? string.Empty).ToLowerInvariant() :
                string.Empty;

            if (type.Contains('.'))
            {
                var typeAndSubtype = type.ToLowerInvariant().Split('.');
                return typeAndSubtype[0] == measureType &&
                       typeAndSubtype[1] == measureSubType;
            }
            else
            {
                return type.ToLowerInvariant() == measureType;
            }
        }

        /// <summary>Gets the measures for the provided company and campaign</summary>
        /// <param name="dac">An IDynamicAllocationCampaign instance.</param>
        /// <returns>The measures</returns>
        private static KeyValuePair<long, IDictionary<string, object>>[] GetMeasures(
            IDynamicAllocationCampaign dac)
        {
            var startTime = DateTime.UtcNow;

            // Get the delivery network designation
            var deliveryNetwork = dac.DeliveryNetwork;

            // Get the exporter version
            int version = dac.CampaignEntity.GetExporterVersion();

            // Get the measures sources
            var measureSources = MeasureSourceFactory.CreateMeasureSources(
                deliveryNetwork,
                version,
                dac.CampaignOwner,
                dac.CompanyEntity,
                dac.CampaignEntity);

            // Get all the measures from the sources
            var measuresBySource =
                measureSources
                .AsParallel()
                .Select(source =>
                    new KeyValuePair<IMeasureSource, IDictionary<long, IDictionary<string, object>>>(
                        source,
                        source.Measures))
                .ToDictionary();

            // Get the loaded measures
            var loadedMeasures =
                measuresBySource
                .Where(mbs => mbs.Value != null)
                .SelectMany(mbs => mbs.Value);

            // Add placeholders for unloaded network sources
            var placeholderMeasures =
                measuresBySource
                .Where(mbs => mbs.Value == null)
                .Select(mbs => mbs.Key)
                .OfType<NetworkMeasureSource>()
                .Select(source =>
                {
                    var placeholderMeasure = new Dictionary<string, object>
                    {
                        { MeasureValues.DisplayName, source.MakeMeasureDisplayName(UnloadedPlaceholderDisplayName) }
                    };
                    return new KeyValuePair<long, IDictionary<string, object>>(-1, placeholderMeasure);
                });

            var measures = loadedMeasures
                .Concat(placeholderMeasures)
                .ToArray();

            LogManager.Log(
                LogLevels.Trace,
                "Loaded measures for campaign '{0}' ({1}) (duration: {2}s - {3} measures from {4} sources)",
                dac.CampaignEntity.ExternalName,
                dac.CampaignEntity.ExternalEntityId,
                (DateTime.UtcNow - startTime).TotalSeconds,
                measures.Length,
                measureSources.Count());

            return measures;
        }
    }
}
