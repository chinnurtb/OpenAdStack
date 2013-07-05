//-----------------------------------------------------------------------
// <copyright file="CategoryMeasureSource.cs" company="Rare Crowds Inc">
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
    /// <summary>AppNexus category measure source</summary>
    internal class CategoryMeasureSource : AppNexusMeasureSourceBase
    {
        /// <summary>Measure type for category measures</summary>
        public const string TargetingType = "category";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 8;

        /// <summary>Name for the categories measure source</summary>
        private const string MeasureSourceName = "categories";

        /// <summary>Initializes a new instance of the CategoryMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public CategoryMeasureSource(IEntity[] entities)
            : base(MeasureIdPrefix, MeasureSourceName, entities)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Categories"; }
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

        /// <summary>Fetch the latest category measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var categories = this.AppNexusClient.GetContentCategories();
            if (categories == null)
            {
                throw new InvalidOperationException("Unable to get categories from AppNexus.");
            }

            var measures =
                new[] { "Include", "Exclude" }
                .SelectMany(action =>
                    categories.ToDictionary(
                        category =>
                            this.GetMeasureId(
                                Convert.ToInt32(category["id"], CultureInfo.InvariantCulture) +
                                ((action == "Include" ? 1 : 2) * 1000000)),
                        category =>
                            this.CreateAppNexusMeasure(
                                new[]
                                {
                                    action,
                                    category["name"]
                                },
                                "{0}:{1}".FormatInvariant(action, category["id"]))))
                .ToDictionary();
                
            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.MaxValue,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }
    }
}
