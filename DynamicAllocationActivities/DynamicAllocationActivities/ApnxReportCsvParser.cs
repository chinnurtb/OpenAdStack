// -----------------------------------------------------------------------
// <copyright file="ApnxReportCsvParser.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>Helper class to parse APNX report csv data.</summary>
    internal class ApnxReportCsvParser : RawDeliveryDataParserBase
    {
        /// <summary>Eastern Standard Time timezone info</summary>
        public static readonly TimeZoneInfo ReportTimeZone = TimeZoneInfo.FindSystemTimeZoneById("UTC");

        /// <summary>hour field canonizer or schema dictionary</summary>
        private static readonly Func<string, string> hourFieldCanonizer = s => TryApnxHourToUtcCanonicalDateString(s);

        /// <summary>campaign code field canonizer or schema dictionary</summary>
        private static readonly Func<string, string> campaigncodeFieldCanonizer = s => TryApnxCampaignCodeToCanonicalAllocationId(s);

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
                    { "campaign_id", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(CampaignIdFieldName, new PropertyValue(PropertyType.Int64, 0)), true, DefaultFieldCanonizer) },
                    { "hour", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(HourFieldName, new PropertyValue(PropertyType.Date, DateTime.MinValue)), true, hourFieldCanonizer) },
                    { "campaign_code", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(AllocationIdFieldName, new PropertyValue(PropertyType.String, "00000000000000000000000000000000")), true, campaigncodeFieldCanonizer) },
                    { "imps", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(ImpressionsFieldName, new PropertyValue(PropertyType.Int64, 0)), false, DefaultFieldCanonizer) },
                    { "ecpm", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(EcpmFieldName, new PropertyValue(PropertyType.Double, 0m)), false, DefaultFieldCanonizer) },
                    { "spend", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(MediaSpendFieldName, new PropertyValue(PropertyType.Double, 0m)), false, DefaultFieldCanonizer) },
                    { "clicks", new Tuple<EntityProperty, bool, Func<string, string>>(new EntityProperty(ClicksFieldName, new PropertyValue(PropertyType.Int64, 0)), false, DefaultFieldCanonizer) }
                };
            }
        }

        /// <summary>Convert apnx campaign code, which may contain decoration, to canonical allocation id</summary>
        /// <param name="col">The campaign code string</param>
        /// <returns>Allocation Id</returns>
        private static string TryApnxCampaignCodeToCanonicalAllocationId(string col)
        {
            // If we can't get 32 characters out of it to try to turn into a guid return empty string
            if (string.IsNullOrEmpty(col) || col.Length < 32)
            {
                return string.Empty;
            }

            // Grab the first 32 characters (a guid)
            return col.Substring(0, 32);
        }

        /// <summary>Convert to a canonical UTC date string.</summary>
        /// <param name="apnxDateString">The apnx date string.</param>
        /// <returns>UTC date string</returns>
        private static string TryApnxHourToUtcCanonicalDateString(string apnxDateString)
        {
            // TODO: This can be made more robust however...assuming it is supposed to be parsable by
            // .Net, this might be least brittle approach after all
            DateTime apnxDate;
            if (!DateTime.TryParse(apnxDateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out apnxDate))
            {
                return string.Empty;
            }

            var utcHourDate = TimeZoneInfo.ConvertTimeToUtc(apnxDate, ReportTimeZone);
            var canonicalDateString = utcHourDate.ToString("o", CultureInfo.InvariantCulture);
            return canonicalDateString;
        }
    }
}