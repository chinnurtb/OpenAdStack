//-----------------------------------------------------------------------
// <copyright file="LocationMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataAccessLayer;
using DynamicAllocation;
using GoogleDfpClient;
using Newtonsoft.Json;
using Utilities.Storage;
using Dfp = Google.Api.Ads.Dfp.v201206;
using DfpUtils = Google.Api.Ads.Dfp.Util.v201206;

namespace GoogleDfpActivities.Measures
{
    /// <summary>Measure source for Google DFP Locations</summary>
    internal sealed class LocationMeasureSource : DfpMeasureSourceBase, IMeasureSource
    {
        /// <summary>Measure type for technology measures</summary>
        public const string TargetingType = "geoTargeting";

        /// <summary>Measure id network prefix for Google DFP measures</summary>
        public const byte LocationMeasureIdPrefix = 1;

        /// <summary>Name for the Location measure source</summary>
        public const string LocationMeasureSourceName = "locations";

        /// <summary>Longevity of the Location measures cache</summary>
        internal static readonly TimeSpan LocationCacheLongevity = new TimeSpan(6, 0, 0);

        /// <summary>Initializes a new instance of the LocationMeasureSource class</summary>
        /// <param name="companyEntity">CompanyEntity (for config)</param>
        /// <param name="campaignEntity">CampaignEntity (for config)</param>
        public LocationMeasureSource(CompanyEntity companyEntity, CampaignEntity campaignEntity)
            : base(LocationMeasureIdPrefix, LocationMeasureSourceName, companyEntity, campaignEntity)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected sealed override string CategoryDisplayName
        {
            get { return "Geotargeting"; }
        }

        /// <summary>Gets the targeting type</summary>
        protected override string MeasureType
        {
            get { return TargetingType; }
        }

        /// <summary>Fetch the latest Location measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var locations =
                this.Countries()
                .Concat(this.Regions())
                .Concat(this.Cities())
                .Concat(this.MetroCodes())
                .Concat(this.PostalCodes());

            var measures = locations
                .ToDictionary(
                    row => this.GetMeasureId((long)row[DfpMeasureValues.DfpId]),
                    row => row);                

            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.UtcNow + LocationCacheLongevity,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }

        /// <summary>Gets country location meaures</summary>
        /// <returns>Country location measures</returns>
        private IEnumerable<IDictionary<string, object>> Countries()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, countryname, countrycode from country WHERE targetable = :targetable")
                .AddValue("targetable", true)
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => this.CreateDfpMeasure(
                    new[]
                    {
                        "Countries",
                        "{0} ({1})".FormatInvariant(row["countryname"], row["countrycode"]),
                    },
                    (long)row["id"],
                    "country"));
        }

        /// <summary>Gets regions location meaures</summary>
        /// <returns>Regions location measures</returns>
        private IEnumerable<IDictionary<string, object>> Regions()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, regionname, regioncode from region WHERE countrycode = :countrycode AND targetable = :targetable")
                .AddValue("countrycode", "US")
                .AddValue("targetable", true)
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => this.CreateDfpMeasure(
                    new[]
                    {
                        "Regions",
                        "{0} ({1})".FormatInvariant(row["regionname"], row["regioncode"])
                    },
                    (long)row["id"],
                    "region"));
        }

        /// <summary>Gets city location meaures</summary>
        /// <returns>City location measures</returns>
        private IEnumerable<IDictionary<string, object>> Cities()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, cityname, regionname from city WHERE countrycode = :countrycode AND targetable = :targetable")
                .AddValue("countrycode", "US")
                .AddValue("targetable", true)
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => this.CreateDfpMeasure(
                    new[]
                    {
                        "Cities",
                        row["regionname"],
                        row["cityname"]
                    },
                    (long)row["id"],
                    "city"));
        }

        /// <summary>Gets metro (dma) location meaures</summary>
        /// <returns>Metro (dma) location measures</returns>
        private IEnumerable<IDictionary<string, object>> MetroCodes()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, metroname, metrocode from metro WHERE countrycode = :countrycode AND targetable = :targetable")
                .AddValue("countrycode", "US")
                .AddValue("targetable", true)
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => this.CreateDfpMeasure(
                    new[]
                    {
                        "Metros",
                        ((string)row["metroname"]).Split(' ').Last(),
                        "{0} ({1})".FormatInvariant(row["metroname"], row["metrocode"]),
                    },
                    (long)row["id"],
                    "dma"));
        }

        /// <summary>Gets postal code location meaures</summary>
        /// <returns>Metro (dma) location measures</returns>
        private IEnumerable<IDictionary<string, object>> PostalCodes()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, postalcode from postal_code WHERE countrycode = :countrycode AND targetable = :targetable")
                .AddValue("countrycode", "US")
                .AddValue("targetable", true)
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => this.CreateDfpMeasure(
                    new[]
                    {
                        "Postal Codes",
                        ((string)row["postalcode"]).Left(2),
                        ((string)row["postalcode"]).Left(3),
                        ((string)row["postalcode"])
                    },
                    (long)row["id"],
                    "postalcode"));
        }
    }
}
