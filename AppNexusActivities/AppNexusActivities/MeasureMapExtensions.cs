//-----------------------------------------------------------------------
// <copyright file="MeasureMapExtensions.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using AppNexusClient;
using ConfigManager;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;

namespace AppNexusActivities
{
    /// <summary>
    /// Extensions for getting/setting AppNexus specific properties from DA measures
    /// </summary>
    public static class MeasureMapExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the AppNexus endpoint is the sandbox
        /// </summary>
        private static bool AppNexusSandbox
        {
            get { return Config.GetBoolValue("AppNexus.Sandbox"); }
        }

        /// <summary>Gets the AppNexus demographic age range</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The age range</returns>
        public static AppNexusAgeRange GetAppNexusAgeRange(this MeasureMap @this, MeasureSet measureSet)
        {
            var ageMeasures = measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.AgeRange);
            var ageMeasure = ageMeasures.FirstOrDefault();
            if (ageMeasures.Count() > 1)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Multiple age ranges defined. Using {0}.",
                    @this.GetDisplayNameForMeasure(ageMeasure));
            }

            var ageRange = AppNexusAgeRange.None;
            if (ageMeasures.Count() > 0)
            {
                var ageRangeForMeasure = @this.GetStringValueForMeasure(ageMeasure, MeasureTargetingCategory.AgeRange, Values.AppNexusId);
                if (!Enum.TryParse<AppNexusAgeRange>(ageRangeForMeasure, out ageRange))
                {
                    LogManager.Log(
                        LogLevels.Error,
                        "Invalid AppNexus Age Range Value: {0}",
                        ageRangeForMeasure);
                }
            }

            return ageRange;
        }

        /// <summary>Gets the AppNexus demographic maximum age</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The maximum age</returns>
        public static string GetAppNexusGender(this MeasureMap @this, MeasureSet measureSet)
        {
            var measures = measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.Gender);
            if (measures.Count() > 1)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Multiple genders defined. Using {0}.",
                    @this.GetDisplayNameForMeasure(measures.FirstOrDefault()));
            }

            return measures.Count() > 0 ?
                @this.GetStringValueForMeasure(
                    measures.First(),
                    MeasureTargetingCategory.Gender,
                    Values.AppNexusId) :
                null;
        }

        /// <summary>Gets the AppNexus segments from the measure set</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus segments</returns>
        public static IDictionary<int, string> GetAppNexusSegments(this MeasureMap @this, MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.Segment)
                .ToDictionary(
                    measure => (int)@this.GetNumericValueForMeasure(
                        measure,
                        MeasureTargetingCategory.Segment,
                        AppNexusSandbox ? Values.AppNexusIdSandbox : Values.AppNexusId),
                    measure => @this.GetDisplayNameForMeasure(measure));
        }

        /// <summary>Gets the AppNexus DMA metro codes from the measure set</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus metro codes</returns>
        public static IDictionary<int, string> GetAppNexusMetroCodes(this MeasureMap @this, MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.Dma)
                .ToDictionary(
                    measure => (int)@this.GetNumericValueForMeasure(
                        measure,
                        MeasureTargetingCategory.Dma,
                        Values.AppNexusId),
                    measure => @this.GetDisplayNameForMeasure(measure));
        }

        /// <summary>Gets the AppNexus region codes from the measure set</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus region codes</returns>
        public static IEnumerable<string> GetAppNexusRegions(this MeasureMap @this, MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.State)
                .Select(
                    measure => @this.GetStringValueForMeasure(
                        measure,
                        MeasureTargetingCategory.State,
                        Values.AppNexusId));
        }

        /// <summary>Gets the creative page location from the measure set</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The page location</returns>
        public static PageLocation GetPageLocation(this MeasureMap @this, MeasureSet measureSet)
        {
            var locationMeasures = measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.Position)
                .Select(
                    measure => (int)@this.GetNumericValueForMeasure(
                        measure,
                        MeasureTargetingCategory.Position,
                        Values.AppNexusId));

            if (locationMeasures.Count() == 0)
            {
                return PageLocation.Any;
            }

            var location = locationMeasures.First();
            var locations = Enum.GetValues(typeof(PageLocation))
                .Cast<int>()
                .ToArray();
            if (location == 0 || !locations.Contains(location))
            {
                throw new ArgumentException(
                    "Invalid AppNexus page location value: {0}"
                    .FormatInvariant(location),
                    "measureSet");
            }

            return (PageLocation)location;
        }

        /// <summary>Gets the inventory attributes from the measure set</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The inventory attribute values</returns>
        public static IEnumerable<int> GetInventoryAttributes(this MeasureMap @this, MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.Inventory)
                .Select(
                    measure => (int)@this.GetNumericValueForMeasure(
                        measure,
                        MeasureTargetingCategory.Inventory,
                        Values.AppNexusId));
        }

        /// <summary>Gets the content category from the measure set</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The content category ids and whether to include/exclude them</returns>
        public static IDictionary<int, bool> GetContentCategories(this MeasureMap @this, MeasureSet measureSet)
        {
            var measureTargetingContentCategories = new[] { MeasureTargetingCategory.ContentCategoryInclude, MeasureTargetingCategory.ContentCategoryExclude };
            return measureSet
                .Where(measure =>
                    measureTargetingContentCategories.Contains(@this.GetTargetingCategoryForMeasure(measure)))
                .ToDictionary(
                    measure => (int)@this.GetNumericValueForMeasure(
                        measure,
                        @this.GetTargetingCategoryForMeasure(measure),
                        AppNexusSandbox ? Values.AppNexusIdSandbox : Values.AppNexusId),
                    measure =>
                        @this.GetTargetingCategoryForMeasure(measure) == MeasureTargetingCategory.ContentCategoryInclude);
        }

        /// <summary>Measure value names</summary>
        private static class Values
        {
            /// <summary>AppNexus Id</summary>
            public const string AppNexusId = "APNXId";

            /// <summary>AppNexus Sandbox Id</summary>
            public const string AppNexusIdSandbox = "APNXId_Sandbox";
        }
    }
}
