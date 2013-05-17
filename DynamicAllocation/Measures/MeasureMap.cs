//-----------------------------------------------------------------------
// <copyright file="MeasureMap.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Diagnostics;
using Newtonsoft.Json;

namespace DynamicAllocation
{
    /// <summary>
    /// Provides a map of measures to segments and data costs
    /// </summary>
    public class MeasureMap
    {
        /// <summary>Sources used to initialize the map</summary>
        private IEnumerable<IMeasureSource> measureSources;

        /// <summary>Backing field for Map</summary>
        private IDictionary<long, IDictionary<string, object>> map;

        /// <summary>Initializes a new instance of the MeasureMap class</summary>
        /// <param name="measureSources">Measure sources</param>
        public MeasureMap(IEnumerable<IMeasureSource> measureSources)
        {
            this.measureSources = measureSources;
        }

        /// <summary>Initializes a new instance of the MeasureMap class</summary>
        /// <remarks>
        /// Allows for tests to directly create measure maps without using sources.
        /// </remarks>
        /// <param name="measureMap">The measure map.</param>
        internal MeasureMap(IDictionary<long, IDictionary<string, object>> measureMap)
        {
            this.map = measureMap;
        }

        /// <summary>Gets the measure mappings</summary>
        internal IDictionary<long, IDictionary<string, object>> Map
        {
            get
            {
                return this.map =
                    this.map ??
                    this.measureSources
                        .AsParallel()
                        .SelectMany(source => source.Measures)
                        .ToDictionary();
            }
        }

        /// <summary>Gets the targeting type of the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The delivery network</returns>
        public DeliveryNetworkDesignation GetMeasureDeliveryNetwork(long measure)
        {
            var network = DeliveryNetworkDesignation.Unknown;
            var value = this.TryGetValue(measure, MeasureValues.DeliveryNetwork);

            if (value != null)
            {
                if (!Enum.TryParse<DeliveryNetworkDesignation>((string)value, out network))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Invalid delivery network designation: '{0}' for measure {1}",
                        value,
                        measure);
                }
            }

            return network;
        }

        /// <summary>Gets the targeting type of the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The targeting type</returns>
        public string GetMeasureType(long measure)
        {
            var value = this.TryGetValue(measure, MeasureValues.Type);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the targeting sub-type of the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The targeting sub-type</returns>
        public string GetMeasureSubType(long measure)
        {
            var value = this.TryGetValue(measure, MeasureValues.SubType);
            return value != null ? (string)value : null;
        }

        /// <summary>Gets the data cost for the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The data cost</returns>
        public decimal GetDataCostForMeasure(long measure)
        {
            return Convert.ToDecimal(
                this.GetMeasureMapping(measure)[MeasureValues.DataCost],
                CultureInfo.InvariantCulture);
        }

        /// <summary>Gets the data provider for the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The provider</returns>
        public string TryGetDataProviderForMeasure(long measure)
        {
            return (string)this.TryGetValue(measure, MeasureValues.DataProvider);
        }

        /// <summary>Gets the historical volume for the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The historical volume</returns>
        public long GetHistoricalVolumeForMeasure(long measure)
        {
            var value = this.GetMeasureMapping(measure)[MeasureValues.HistoricalVolume];
            if (value == null)
            {
                return -1;
            }

            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }
        
        /// <summary>Gets the display name for the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The display name</returns>
        public string GetDisplayNameForMeasure(long measure)
        {
            return (string)this.GetMeasureMapping(measure)[MeasureValues.DisplayName];
        }

        /// <summary>
        /// Get MinCPm if present, otherwise return null
        /// </summary>
        /// <param name="measure">the measureId</param>
        /// <returns>the minCPM or null</returns>
        public decimal? TryGetMinCostPerMille(long measure)
        {
            var value = this.TryGetValue(measure, MeasureValues.MinCostPerMille);
            if (value == null)
            {
                return null;
            }

            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Get PercentOfMedia if present, otherwise return null
        /// </summary>
        /// <param name="measure">the measureId</param>
        /// <returns>the PercentOfMedia or null</returns>
        public decimal? TryGetPercentOfMedia(long measure)
        {
            var value = this.TryGetValue(measure, MeasureValues.PercentOfMedia);
            if (value == null)
            {
                return null;
            }

            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        /// <summary>Gets a numeric value from the measure</summary>
        /// <param name="measure">The measure</param>
        /// <param name="measureValueName">The measure value name</param>
        /// <returns>The measure value</returns>
        public long GetNumericValueForMeasure(long measure, string measureValueName)
        {
            var value = this.GetValueForMeasure(measure, measureValueName);
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        }

        /// <summary>Gets a string value from the measure, checking its category</summary>
        /// <param name="measure">The measure</param>
        /// <param name="measureValueName">The measure value name</param>
        /// <returns>The measure value</returns>
        public string GetStringValueForMeasure(long measure, string measureValueName)
        {
            var value = this.GetValueForMeasure(measure, measureValueName);
            return value as string;
        }

        /// <summary>
        /// Get DataCost if present, otherwise return null
        /// </summary>
        /// <param name="measure">the measureId</param>
        /// <returns>the DataCost or null</returns>
        internal decimal? TryGetDataCost(long measure)
        {
            var value = this.TryGetValue(measure, MeasureValues.DataCost);
            if (value == null)
            {
                return null;
            }

            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Helper method to try get values
        /// </summary>
        /// <param name="measure">the measureId</param>
        /// <param name="fieldName">the fieldName</param>
        /// <returns>an object</returns>
        private object TryGetValue(long measure, string fieldName)
        {
            var mapRecord = this.GetMeasureMapping(measure);
            return !mapRecord.ContainsKey(fieldName) ? null : mapRecord[fieldName];
        }

        /// <summary>Gets the mappings for the measure</summary>
        /// <param name="measure">The measure</param>
        /// <returns>The mappings</returns>
        /// <exception cref="KeyNotFoundException">
        /// The measure was not found
        /// </exception>
        private IDictionary<string, object> GetMeasureMapping(long measure)
        {
            if (!this.Map.ContainsKey(measure))
            {
                var message = "The measure '{0}' was not found".FormatInvariant(measure);
                throw new KeyNotFoundException(message);
            }

            return this.Map[measure];
        }

        /// <summary>Gets a value for the measure, checking its category</summary>
        /// <param name="measure">The measure</param>
        /// <param name="measureValueName">The measure value name</param>
        /// <returns>The measure value</returns>
        private object GetValueForMeasure(long measure, string measureValueName)
        {
            var mapping = this.GetMeasureMapping(measure);
            if (!mapping.ContainsKey(measureValueName))
            {
                throw new ArgumentException(
                    "Measure {0} does not contain a value for '{1}'"
                    .FormatInvariant(measure, measureValueName),
                    "measure");
            }

            return mapping[measureValueName];
        }
    }
}
