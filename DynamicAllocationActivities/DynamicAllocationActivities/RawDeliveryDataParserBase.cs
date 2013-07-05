// -----------------------------------------------------------------------
// <copyright file="RawDeliveryDataParserBase.cs" company="Rare Crowds Inc">
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
using System.Linq;
using DataAccessLayer;
using Diagnostics;

namespace DynamicAllocationActivities
{
    /// <summary>Helper class to provide common delivery data parsing functionality.</summary>
    internal abstract class RawDeliveryDataParserBase : IRawDeliveryDataParser
    {
        ////Canonical property names

        /// <summary>Name of hour field in delivery data dictionary record</summary>
        public const string HourFieldName = "Hour";

        /// <summary>Name of campaign Id field in delivery data dictionary record</summary>
        public const string CampaignIdFieldName = "CampaignId";

        /// <summary>Name of allocation Id field in delivery data dictionary record</summary>
        public const string AllocationIdFieldName = "AllocationId";

        /// <summary>Name of impressions field in delivery data dictionary record</summary>
        public const string ImpressionsFieldName = "Impressions";

        /// <summary>Name of ecpm field in delivery data dictionary record</summary>
        public const string EcpmFieldName = "Ecpm";

        /// <summary>Name of media spend field in delivery data dictionary record</summary>
        public const string MediaSpendFieldName = "Spend";

        /// <summary>Name of clicks field in delivery data dictionary record</summary>
        public const string ClicksFieldName = "Clicks";
        
        /// <summary>Default field canonizer</summary>
        protected static readonly Func<string, string> DefaultFieldCanonizer = s => s;

        /// <summary>Gets the field names that appear in a canonical delivery data record.</summary>
        internal static string[] CanonicalKeys
        {
            get
            {
                return new[]
                {
                    CampaignIdFieldName, 
                    AllocationIdFieldName, 
                    HourFieldName, 
                    ImpressionsFieldName, 
                    EcpmFieldName, 
                    MediaSpendFieldName, 
                    ClicksFieldName
                };
            }
        }

        /// <summary>
        /// Gets FieldMap 
        /// A light weight schema for mapping data source name to an internal name and serialization type.
        /// EntityProperty is used as a way to capture name and type (values are dummies)
        /// </summary>
        protected abstract Dictionary<string, Tuple<EntityProperty, bool, Func<string, string>>> FieldMap { get; }

        /// <summary>Parse raw delivery data records. Tolerant of multiple header records from concatonated reports.</summary>
        /// <param name="records">An array of records (one record per array element).</param>
        /// <returns>A canonical representation of the delivery data - one dictionary of name/value pairs per record.</returns>
        public virtual IList<Dictionary<string, PropertyValue>> ParseRawRecords(string[] records)
        {
            return this.ParseRawCsvRecords(records);
        }

        /// <summary>Split individual records of a raw delivery data file into a list of one string per record.</summary>
        /// <param name="rawDeliveryData">The raw delivery data string.</param>
        /// <returns>A list of strings - one per record</returns>
        public virtual IList<string> SplitRecords(string rawDeliveryData)
        {
            return SplitAndTrim(rawDeliveryData, "\r\n");
        }

        /// <summary>Split and Trim...</summary>
        /// <param name="source">The source.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns>A list of strings</returns>
        protected static List<string> SplitAndTrim(string source, string delimiter)
        {
            return source.Split(new[] { delimiter }, StringSplitOptions.None)
                .Select(r => r.Trim()).ToList();
        }

        /// <summary>Parse raw delivery data in CSV form. Tolerant of multiple header records from concatonated reports.</summary>
        /// <param name="records">An array of CSV records (one comma delimited record per array element).</param>
        /// <returns>A canonical representation of the delivery data - one dictionary of name/value pairs per record.</returns>
        protected List<Dictionary<string, PropertyValue>> ParseRawCsvRecords(string[] records)
        {
            if (records == null || records.Length == 0)
            {
                return new List<Dictionary<string, PropertyValue>>();
            }

            // Build a dictionary with our delivery data
            var deliveryData = new Dictionary<string, Dictionary<string, PropertyValue>>();

            // Header treated case-insensitive
            var header = SplitAndTrim(records[0], ",").Select(h => h.ToLowerInvariant()).ToList();

            var colCount = header.Count;

            // Ignore the first record (header)
            for (int rowIndex = 1; rowIndex < records.Length; rowIndex++)
            {
                var colValues = SplitAndTrim(records[rowIndex], ",");

                // Blank row or bad row
                if (colValues.Count != colCount)
                {
                    LogManager.Log(
                        LogLevels.Trace,
                        "DfpReportCsvParser.ParseRawRecords - Invalid row, column count does not match header count. Row = {0}",
                        records[rowIndex]);

                    continue;
                }

                // Skip subsequent headers (from concatenating multiple delivery data CSVs)
                if (colValues.All(col => header.Contains(col.ToLowerInvariant())))
                {
                    continue;
                }

                // Populate a dictionary with the values for this row
                var record = new Dictionary<string, PropertyValue>();
                for (int colIndex = 0; colIndex < colCount; colIndex++)
                {
                    var col = colValues[colIndex];

                    // If we encounter a key we don't understand ignore it
                    if (!this.FieldMap.ContainsKey(header[colIndex]))
                    {
                        LogManager.Log(
                            LogLevels.Trace,
                            "DfpReportCsvParser.ParseRawRecords - Ignoring column: {0}",
                            header[colIndex]);
                        continue;
                    }

                    var template = this.FieldMap[header[colIndex]];

                    // If we encounter a type that is not compatible with what we expect fail out with null
                    try
                    {
                        // The template maps source name to target name and a tuple. The first item in the tuple contains a default
                        // propertyValue with expected type and default value. The second contains a flag indicating whether a value is required or a
                        // reasonable default exists.
                        var entityProperty = template.Item1;
                        var fieldRequired = template.Item2;
                        var propertyName = entityProperty.Name;
                        var expectedType = entityProperty.Value.DynamicType;
                        var propertyValue = entityProperty.Value;
                        var canonizer = template.Item3;

                        // Special handling for fields that need it
                        col = canonizer(col);

                        // If the column value is empty and defaults are not allowed, fail out
                        if (string.IsNullOrEmpty(col) && fieldRequired)
                        {
                            LogManager.Log(
                                LogLevels.Trace,
                                "DfpReportCsvParser.ParseRawRecords - Requirded value is empty: {0}",
                                header[colIndex]);
                            return null;
                        }

                        // Now just build our property value
                        if (!string.IsNullOrEmpty(col))
                        {
                            propertyValue = new PropertyValue(expectedType, col);
                        }

                        record.Add(propertyName, propertyValue);
                    }
                    catch (ArgumentException)
                    {
                        LogManager.Log(
                            LogLevels.Trace,
                            "DfpReportCsvParser.ParseRawRecords - Failed to deserialize value: {0}, {1}",
                            header[colIndex],
                            col);
                        return null;
                    }
                }

                record = this.CanonizeRecord(record);

                var key = "{0}:{1}".FormatInvariant(
                    record[AllocationIdFieldName],
                    record[HourFieldName]);
                deliveryData[key] = record;
            }

            return new List<Dictionary<string, PropertyValue>>(deliveryData.Values);
        }

        /// <summary>Build a record that contains only canonical delivery data field names.</summary>
        /// <param name="sourceRecord">The source record.</param>
        /// <returns>A new canonical record.</returns>
        protected virtual Dictionary<string, PropertyValue> CanonizeRecord(IDictionary<string, PropertyValue> sourceRecord)
        {
            return sourceRecord.ToDictionary();
        }
    }
}