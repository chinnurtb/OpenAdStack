//-----------------------------------------------------------------------
// <copyright file="CityMeasureSource.cs" company="Rare Crowds Inc">
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
    /// <summary>AppNexus city measure source</summary>
    internal class CityMeasureSource : GeotargetingMeasureSourceBase
    {
        /// <summary>Measure subtype for metro code measures</summary>
        public const string TargetingSubType = "city";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 6;

        /// <summary>Name for the cities measure source</summary>
        private const string MeasureSourceName = "cities";

        /// <summary>Initializes a new instance of the CityMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public CityMeasureSource(IEntity[] entities)
            : base(MeasureIdPrefix, MeasureSourceName, entities)
        {
        }

        /// <summary>Gets the targeting subtype for city measures</summary>
        protected override string MeasureSubType
        {
            get { return TargetingSubType; }
        }

        /// <summary>Gets the display name for the measure source subcategory</summary>
        protected override string SubCategoryDisplayName
        {
            get { return "Cities"; }
        }

        /// <summary>Gets a value indicating whether this measure source is "online"</summary>
        /// <remarks>Only Online sources can use the AppNexusClient</remarks>
        protected override bool Online
        {
            get { return true; }
        }

        /// <summary>Fetch the latest city measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            const string Filter = "US/ALL";
            var cities = this.AppNexusClient.GetCities(Filter);
            if (cities == null)
            {
                var message = "Unable to get cities from AppNexus for '{0}'"
                    .FormatInvariant(Filter);
                throw new InvalidOperationException(message);
            }

            var measures = cities
                .ToDictionary(
                    city => this.GetMeasureId(Convert.ToInt32(city["id"], CultureInfo.InvariantCulture)),
                    city => this.CreateAppNexusGeotargetingMeasure(
                        city["id"],
                        city["region_name"],
                        city["city"]));
                
            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.MaxValue,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }
    }
}
