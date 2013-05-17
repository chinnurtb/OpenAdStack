// -----------------------------------------------------------------------
// <copyright file="DfpReportCsvParser.cs" company="Emerging Media Group">
//  Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>Helper class to parse Google DFP report csv data.</summary>
    internal class DfpReportCsvParser : RawDeliveryDataParserBase
    {
        /// <summary>Eastern Standard Time timezone info</summary>
        //// TODO: This needs to be updated for DFP
        public static readonly TimeZoneInfo ReportTimeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");

        /// <summary>Name of date field in delivery data dictionary record</summary>
        public const string DateFieldName = "Date";

        /// <summary>Name of hour of day field in delivery data dictionary record</summary>
        public const string HourOfDayFieldName = "HourOfDay";

        /// <summary>date field canonizer or schema dictionary</summary>
        private static readonly Func<string, string> dateFieldCanonizer = s => TryDfpDateToUtcCanonicalDateString(s);

        /// <summary>external line item id field canonizer or schema dictionary</summary>
        private static readonly Func<string, string> externalIdFieldCanonizer = s => TryDfpExternalIdToCanonicalAllocationId(s);

        /// <summary>
        /// Gets FieldMap 
        /// A light weight schema for mapping data source name to an internal name and serialization type.
        /// EntityProperty is used as a way to capture name and type (values are dummies)
        /// </summary>
        protected override Dictionary<string, Tuple<EntityProperty, bool, Func<string, string>>> FieldMap 
        { 
            get
            {
                return new Dictionary<string, Tuple<EntityProperty, bool, Func<string, string>>>
                    {
                        { "dimension.line_item_id", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(CampaignIdFieldName, new PropertyValue(PropertyType.Int64, 0)), true, DefaultFieldCanonizer) },
                        { "dimension.date", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(DateFieldName, new PropertyValue(PropertyType.Date, DateTime.MinValue)), true, dateFieldCanonizer) },
                        { "dimension.hour", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(HourOfDayFieldName, new PropertyValue(PropertyType.Int32, 0)), true, DefaultFieldCanonizer) },
                        { "dimensionattribute.line_item_external_id", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(AllocationIdFieldName, new PropertyValue(PropertyType.String, "00000000000000000000000000000000")), true, externalIdFieldCanonizer) },
                        { "column.ad_server_impressions", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(ImpressionsFieldName, new PropertyValue(PropertyType.Int64, 0)), false, DefaultFieldCanonizer) },
                        { "column.ad_server_average_ecpm", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(EcpmFieldName, new PropertyValue(PropertyType.Double, 0m)), false, DefaultFieldCanonizer) },
                        { "column.ad_server_cpm_and_cpc_revenue", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(MediaSpendFieldName, new PropertyValue(PropertyType.Double, 0m)), false, DefaultFieldCanonizer) },
                        { "column.ad_server_clicks", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(ClicksFieldName, new PropertyValue(PropertyType.Int64, 0)), false, DefaultFieldCanonizer) }
                    };
            }
        }

        /// <summary>Build a record that contains only canonical delivery data field names.</summary>
        /// <param name="sourceRecord">The source record.</param>
        /// <returns>A new canonical record.</returns>
        protected override Dictionary<string, PropertyValue> CanonizeRecord(IDictionary<string, PropertyValue> sourceRecord)
        {
            // DFP Date and Hour-of-Day need to be combined to a canonical Hour field
            var canonicalHour = ((DateTime)sourceRecord[DateFieldName]).AddHours((int)sourceRecord[HourOfDayFieldName]);
            sourceRecord[HourFieldName] = canonicalHour;

            // Filter the non-canonical fields
            var canonicalRecord = sourceRecord.Where(k => CanonicalKeys.Contains(k.Key)).ToDictionary();

            return canonicalRecord;
        }

        /// <summary>Convert DFP Line Item External Id to canonical allocation id</summary>
        /// <param name="externalId">The external id as string</param>
        /// <returns>Allocation Id</returns>
        private static string TryDfpExternalIdToCanonicalAllocationId(string externalId)
        {
            // If we can't get 32 characters out of it to try to turn into a guid return empty string
            if (string.IsNullOrEmpty(externalId) || externalId.Length < 32)
            {
                return string.Empty;
            }

            // Grab the first 32 characters (a guid)
            return externalId.Substring(0, 32);
        }

        /// <summary>Convert to a canonical UTC date string.</summary>
        /// <param name="dfpDateString">The DFP date string.</param>
        /// <returns>UTC date string</returns>
        private static string TryDfpDateToUtcCanonicalDateString(string dfpDateString)
        {
            // TODO: This can be made more robust however...assuming it is supposed to be parsable by
            // .Net, this might be least brittle approach after all
            DateTime dfpDate;
            if (!DateTime.TryParse(dfpDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out dfpDate))
            {
                return string.Empty;
            }

            var utcHourDate = TimeZoneInfo.ConvertTimeToUtc(dfpDate, ReportTimeZone);
            var canonicalDateString = utcHourDate.ToString("o", CultureInfo.InvariantCulture);
            return canonicalDateString;
        }
    }
}