//-----------------------------------------------------------------------
// <copyright file="AppNexusCampaignLegacyExporter.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Activities;
using AppNexusActivities.Measures;
using AppNexusClient;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using EntityActivities;
using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>
    /// Exports dynamic allocation campaigns to AppNexus using the legacy AppNexus measures
    /// </summary>
    /// <seealso cref="AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider"/>
    internal sealed class AppNexusCampaignLegacyExporter : AppNexusCampaignExporterBase
    {
        /// <summary>
        /// Initializes a new instance of the AppNexusCampaignLegacyExporter class.
        /// </summary>
        /// <param name="companyEntity">The advertiser company</param>
        /// <param name="campaignEntity">The campaign being exported</param>
        /// <param name="campaignOwner">Owner of the campaign being exported</param>
        public AppNexusCampaignLegacyExporter(
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner)
            : base(0, companyEntity, campaignEntity, campaignOwner)
        {
        }

        /// <summary>
        /// Gets a value indicating whether the AppNexus endpoint is the sandbox
        /// </summary>
        private bool AppNexusSandbox
        {
            get { return this.Config.GetBoolValue("AppNexus.Sandbox"); }
        }

        /// <summary>Gets the AppNexus allow unknown age value</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>Whether to allow unknown age</returns>
        protected override bool GetAppNexusAllowUnknownAge(MeasureSet measureSet)
        {
            // Get the age measures (if any)
            var measures = measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.AgeRange);

            // Find the allow unknown value (if any)
            return measures
                .Select(measure =>
                    this.MeasureMap.GetStringValueForMeasure(measure, AppNexusMeasureValues.AppNexusId))
                .Where(id =>
                    id.Contains("Unknown"))
                .Select(id =>
                    id.Contains("Allow"))
                .FirstOrDefault();
        }

        /// <summary>Gets the AppNexus demographic age range</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The age range</returns>
        protected override Tuple<int, int> GetAppNexusAgeRange(MeasureSet measureSet)
        {
            // Get the age measures (if any)
            var measures = measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.AgeRange);

            // Get the age ranges (if any)
            var ageRanges = measures
                .Select(measure =>
                    this.MeasureMap.GetStringValueForMeasure(measure, AppNexusMeasureValues.AppNexusId))
                .Where(id => id.Contains('-'))
                .Select(id => id.Split('-'))
                .Where(range => range.Length == 2)
                .Select(range => range
                    .Select(age => Convert.ToInt16(age, CultureInfo.InvariantCulture))
                    .ToArray());

            // If there are more than one, warn only using first
            if (ageRanges.Count() > 1)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Multiple age ranges defined. Using {0}.",
                    this.MeasureMap.GetDisplayNameForMeasure(measures.First()));
            }

            // Return a tuple of the range (or null)
            var ageRange = ageRanges.FirstOrDefault();
            if (ageRange == null)
            {
                return null;
            }

            return new Tuple<int, int>(ageRange[0], ageRange[1]);
        }

        /// <summary>Gets the AppNexus demographic maximum age</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The maximum age</returns>
        protected override string GetAppNexusGender(MeasureSet measureSet)
        {
            var measures = measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.Gender);
            if (measures.Count() > 1)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Multiple genders defined. Using {0}.",
                    this.MeasureMap.GetDisplayNameForMeasure(measures.FirstOrDefault()));
            }

            return measures.Count() > 0 ?
                this.MeasureMap.GetStringValueForMeasure(
                    measures.First(),
                    AppNexusMeasureValues.AppNexusId) :
                null;
        }

        /// <summary>Gets the AppNexus DMA metro codes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus metro codes</returns>
        protected override IDictionary<int, string> GetAppNexusMetroCodes(MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.Dma)
                .ToDictionary(
                    measure => (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId),
                    measure => this.MeasureMap.GetDisplayNameForMeasure(measure));
        }

        /// <summary>Gets the AppNexus region codes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus region codes</returns>
        protected override IEnumerable<string> GetAppNexusRegions(MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.State)
                .Select(
                    measure => this.MeasureMap.GetStringValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId));
        }

        /// <summary>Gets the creative page location from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The page location</returns>
        protected override PageLocation GetPageLocation(MeasureSet measureSet)
        {
            var locationMeasures = measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.Position)
                .Select(
                    measure => (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId));

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
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The inventory attribute values</returns>
        protected override IEnumerable<int> GetInventoryAttributes(MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.Inventory)
                .Select(
                    measure => (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId));
        }

        /// <summary>Gets the content category from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The content category ids and whether to include/exclude them</returns>
        protected override IDictionary<int, bool> GetContentCategories(MeasureSet measureSet)
        {
            var measureTargetingContentCategories = new[] { AppNexusMeasureTypes.ContentCategoryInclude, AppNexusMeasureTypes.ContentCategoryExclude };
            return measureSet
                .Where(measure =>
                    measureTargetingContentCategories.Contains(this.MeasureMap.GetMeasureType(measure)))
                .ToDictionary(
                    measure => (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        this.AppNexusSandbox ? AppNexusMeasureValues.AppNexusIdSandbox : AppNexusMeasureValues.AppNexusId),
                    measure =>
                        this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.ContentCategoryInclude);
        }

        /// <summary>Gets the AppNexus segments from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus segments</returns>
        protected override IDictionary<int, string> GetAppNexusSegments(MeasureSet measureSet)
        {
            return measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureType(measure) == AppNexusMeasureTypes.Segment)
                .ToDictionary(
                    measure => (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        this.AppNexusSandbox ? AppNexusMeasureValues.AppNexusIdSandbox : AppNexusMeasureValues.AppNexusId),
                    measure => this.MeasureMap.GetDisplayNameForMeasure(measure));
        }

        /// <summary>Gets the AppNexus frequency caps from the measure set</summary>
        /// <param name="measureSets">The MeasureSets</param>
        /// <returns>The frequency caps</returns>
        protected override IDictionary<AppNexusFrequencyType, int> GetFrequencyCaps(IEnumerable<MeasureSet> measureSets)
        {
            // Not supported in legacy exporter
            return new Dictionary<AppNexusFrequencyType, int>();
        }

        /// <summary>Gets the AppNexus domain targets from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The domain targets</returns>
        protected override IEnumerable<string> GetDomainTargets(MeasureSet measureSet)
        {
            // Not supported in legacy exporter
            return new string[0];
        }

        /// <summary>Gets the AppNexus domain-list targets from the measure sets</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The domain-list targets</returns>
        protected override IDictionary<int, bool> GetDomainListTargets(MeasureSet measureSet)
        {
            // Not supported in legacy exporter
            return null;
        }
    }
}
