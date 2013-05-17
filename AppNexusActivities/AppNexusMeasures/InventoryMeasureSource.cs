//-----------------------------------------------------------------------
// <copyright file="InventoryMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
    /// <summary>AppNexus inventory measure source</summary>
    internal class InventoryMeasureSource : AppNexusMeasureSourceBase
    {
        /// <summary>Measure type for inventory measures</summary>
        public const string TargetingType = "inventory";

        /// <summary>Measure id network prefix for AppNexus measures</summary>
        private const byte MeasureIdPrefix = 9;

        /// <summary>Name for the inventory measure source</summary>
        private const string MeasureSourceName = "inventory";

        /// <summary>Initializes a new instance of the InventoryMeasureSource class</summary>
        /// <param name="entities">Entities (for config)</param>
        public InventoryMeasureSource(IEntity[] entities)
            : base(MeasureIdPrefix, MeasureSourceName, entities)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Inventory Attributes"; }
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

        /// <summary>Fetch the latest inventory measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var inventoryAttributes = this.AppNexusClient.GetInventoryAttributes();
            if (inventoryAttributes == null)
            {
                throw new InvalidOperationException("Unable to get inventory sources from AppNexus.");
            }

            var measures =
                inventoryAttributes
                    .ToDictionary(
                        attribute =>
                            this.GetMeasureId(
                                Convert.ToInt32(attribute["id"], CultureInfo.InvariantCulture)),
                        attribute =>
                            this.CreateAppNexusMeasure(
                                new[]
                                {
                                    attribute["name"]
                                },
                                attribute["id"]));

            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.MaxValue,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }
    }
}
