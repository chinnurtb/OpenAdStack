//-----------------------------------------------------------------------
// <copyright file="AppNexusCampaignExporter.cs" company="Rare Crowds Inc">
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
    /// <summary>Exports dynamic allocation campaigns to AppNexus</summary>
    internal sealed class AppNexusCampaignExporter : AppNexusCampaignExporterBase
    {
        /// <summary>
        /// Initializes a new instance of the AppNexusCampaignExporter class.
        /// </summary>
        /// <param name="companyEntity">The advertiser company</param>
        /// <param name="campaignEntity">The campaign being exported</param>
        /// <param name="campaignOwner">Owner of the campaign being exported</param>
        public AppNexusCampaignExporter(
            CompanyEntity companyEntity,
            CampaignEntity campaignEntity,
            UserEntity campaignOwner)
            : base(1, companyEntity, campaignEntity, campaignOwner)
        {
        }

        /// <summary>Gets the AppNexus allow unknown age value</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>Whether to allow unknown age</returns>
        protected override bool GetAppNexusAllowUnknownAge(MeasureSet measureSet)
        {
            // Get the age measures (if any)
            var measures = this.FilterMeasures(
                measureSet,
                AgeRangeMeasureSource.TargetingType,
                AgeRangeMeasureSource.TargetingSubType);

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
            var measures = this.FilterMeasures(
                measureSet,
                AgeRangeMeasureSource.TargetingType,
                AgeRangeMeasureSource.TargetingSubType);

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
            var measures = this.FilterMeasures(measureSet, "demographic", "gender");
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
            var measures = this.FilterMeasures(
                measureSet,
                MetroCodeMeasureSource.TargetingType,
                MetroCodeMeasureSource.TargetingSubType);
            
            // It is possible for multiple metro code measures to have the same
            // metro code value (ex: listed under multiple states). This is valid
            // but requires the distinct filter before creating the dictionary.
            return measures
                .Select(measure =>
                    new KeyValuePair<int, string>(
                        (int)this.MeasureMap.GetNumericValueForMeasure(
                            measure,
                            AppNexusMeasureValues.AppNexusId),
                        this.MeasureMap.GetDisplayNameForMeasure(measure)))
                .Distinct(kvp => kvp.Key)
                .ToDictionary();
        }

        /// <summary>Gets the AppNexus region codes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus region codes</returns>
        protected override IEnumerable<string> GetAppNexusRegions(MeasureSet measureSet)
        {
            var measures = this.FilterMeasures(
                measureSet,
                RegionMeasureSource.TargetingType,
                RegionMeasureSource.TargetingSubType);
            return measures
                .Select(measure =>
                    this.MeasureMap.GetStringValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId));
        }

        /// <summary>Gets the creative page location from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The page location</returns>
        protected override PageLocation GetPageLocation(MeasureSet measureSet)
        {
            var measures = this.FilterMeasures(measureSet, "position");
            var locations = measures
                .Select(measure =>
                    (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId))
                .Cast<PageLocation>();                

            if (locations.Count() > 1)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "More than one page location selected. Using '{0}'.",
                    locations.First());
            }

            return locations.FirstOrDefault();
        }

        /// <summary>Gets the inventory attributes from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The inventory attribute values</returns>
        protected override IEnumerable<int> GetInventoryAttributes(MeasureSet measureSet)
        {
            var measures = this.FilterMeasures(
                measureSet,
                InventoryMeasureSource.TargetingType);
            return measures
                .Select(measure =>
                    (int)this.MeasureMap.GetNumericValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId));
        }

        /// <summary>Gets the content category from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The content category ids and whether to include/exclude them</returns>
        protected override IDictionary<int, bool> GetContentCategories(MeasureSet measureSet)
        {
            var measures = this.FilterMeasures(
                measureSet,
                CategoryMeasureSource.TargetingType);
            return measures
                .Select(measure =>
                    this.MeasureMap.GetStringValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId))
                .Select(id => id.Split(':'))
                .Select(values => new KeyValuePair<int, bool>(
                        Convert.ToInt32(values[1], CultureInfo.InvariantCulture),
                        values[0] == "Include"))
                .Distinct(kvp => kvp.Key)
                .ToDictionary();
        }

        /// <summary>Gets the AppNexus segments from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The AppNexus segments</returns>
        protected override IDictionary<int, string> GetAppNexusSegments(MeasureSet measureSet)
        {
            var measures = this.FilterMeasures(
                measureSet,
                SegmentMeasureSource.TargetingType);
            return measures
                .Select(measure => new KeyValuePair<int, string>(
                        (int)this.MeasureMap.GetNumericValueForMeasure(
                            measure,
                            AppNexusMeasureValues.AppNexusId),
                        this.MeasureMap.GetDisplayNameForMeasure(measure)))
                .Distinct(kvp => kvp.Key)
                .ToDictionary();
        }

        /// <summary>Gets the AppNexus frequency caps from the measure set</summary>
        /// <param name="measureSets">The MeasureSets</param>
        /// <returns>The frequency caps</returns>
        protected override IDictionary<AppNexusFrequencyType, int> GetFrequencyCaps(IEnumerable<MeasureSet> measureSets)
        {
            // TODO: Move all measure types to enum defined in AppNexusUtilities?
            // (currently defined as constants in each measure source)
            const string FrequencyCapMeasureType = "frequency";
            const string FrequencyMeasureValue = "value";

            var frequencyTypes = Enum.GetNames(typeof(AppNexusFrequencyType)).Select(s => s.ToLowerInvariant());

            var frequencyMeasures = measureSets
                .SelectMany(measureSet =>
                    this.FilterMeasures(measureSet, FrequencyCapMeasureType));
            return frequencyMeasures
                .Select(measure =>
                    new KeyValuePair<string, int>(
                        this.MeasureMap.GetMeasureSubType(measure).ToLowerInvariant(),
                        (int)this.MeasureMap.GetNumericValueForMeasure(measure, FrequencyMeasureValue)))
                .Where(kvp =>
                    frequencyTypes.Contains(kvp.Key))
                .Distinct(kvp => kvp.Key)
                .ToDictionary(
                    kvp => (AppNexusFrequencyType)Enum.Parse(typeof(AppNexusFrequencyType), kvp.Key, true),
                    kvp => kvp.Value);
        }

        /// <summary>Gets the AppNexus domain targets from the measure set</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The domain targets</returns>
        protected override IEnumerable<string> GetDomainTargets(MeasureSet measureSet)
        {
            // TODO: Move all measure types to enum defined in AppNexusUtilities?
            // (currently defined as constants in each measure source)
            const string DomainTargetMeasureType = "domains";
            const string DomainMeasureValue = "value";

            var domainMeasures = this.FilterMeasures(measureSet, DomainTargetMeasureType);
            return domainMeasures
                .Select(measure =>
                    this.MeasureMap.GetStringValueForMeasure(measure, DomainMeasureValue))
                .SelectMany(domains =>
                    domains.Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim()));
        }

        /// <summary>Gets the AppNexus domain-list targets from the measure sets</summary>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The domain-list targets</returns>
        protected override IDictionary<int, bool> GetDomainListTargets(MeasureSet measureSet)
        {
            var measures = this.FilterMeasures(
                measureSet,
                DomainListMeasureSource.TargetingType);
            return measures
                .Select(measure =>
                    this.MeasureMap.GetStringValueForMeasure(
                        measure,
                        AppNexusMeasureValues.AppNexusId))
                .Select(id => id.Split(':'))
                .Select(values => new KeyValuePair<int, bool>(
                        Convert.ToInt32(values[1], CultureInfo.InvariantCulture),
                        values[0] == "Include"))
                .Distinct(kvp => kvp.Key)
                .ToDictionary();
        }

        /// <summary>Filters the measures for the specified targeting type and subtype</summary>
        /// <param name="measureSet">The measure set</param>
        /// <param name="targetingType">The targeting type</param>
        /// <param name="targetingSubtype">The targeting subtype (optional)</param>
        /// <returns>The filtered measure set</returns>
        private IEnumerable<long> FilterMeasures(
            MeasureSet measureSet,
            string targetingType,
            string targetingSubtype = null)
        {
            return measureSet
                .Where(measure =>
                    this.MeasureMap.GetMeasureDeliveryNetwork(measure) == DeliveryNetworkDesignation.AppNexus &&
                    this.MeasureMap.GetMeasureType(measure).ToLowerInvariant() == targetingType.ToLowerInvariant() &&
                    (targetingSubtype == null || this.MeasureMap.GetMeasureSubType(measure).ToLowerInvariant() == targetingSubtype.ToLowerInvariant()));
        }
    }
}
