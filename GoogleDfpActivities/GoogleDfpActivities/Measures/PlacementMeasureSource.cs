//-----------------------------------------------------------------------
// <copyright file="PlacementMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;
using DynamicAllocation;
using GoogleDfpClient;
using Newtonsoft.Json;
using Utilities.Storage;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpActivities.Measures
{
    /// <summary>Measure source for Google DFP Placements</summary>
    internal sealed class PlacementMeasureSource : DfpMeasureSourceBase, IMeasureSource
    {
        /// <summary>Measure type for technology measures</summary>
        public const string TargetingType = "placement";

        /// <summary>Measure id network prefix for Google DFP measures</summary>
        public const byte PlacementMeasureIdPrefix = 2;

        /// <summary>Name for the Placement measure source</summary>
        public const string PlacementMeasureSourceName = "placements";

        /// <summary>Longevity of the Placement measures cache</summary>
        internal static readonly TimeSpan PlacementCacheLongevity = new TimeSpan(6, 0, 0);

        /// <summary>Initializes a new instance of the PlacementMeasureSource class</summary>
        /// <param name="companyEntity">CompanyEntity (for config)</param>
        /// <param name="campaignEntity">CampaignEntity (for config)</param>
        public PlacementMeasureSource(CompanyEntity companyEntity, CampaignEntity campaignEntity)
            : base(PlacementMeasureIdPrefix, PlacementMeasureSourceName, companyEntity, campaignEntity)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Placements"; }
        }

        /// <summary>Gets the targeting type</summary>
        protected override string MeasureType
        {
            get { return TargetingType; }
        }

        /// <summary>Fetch the latest Placement measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var placements = this.DfpClient.GetAllPlacements();
            var measures = placements
                .ToDictionary(
                    placement => this.GetMeasureId(placement.id),
                    placement => this.CreateDfpMeasure(placement.name, placement.id));
            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.UtcNow + PlacementCacheLongevity,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }
    }
}
