//-----------------------------------------------------------------------
// <copyright file="MeasureMapExtensions.cs" company="Emerging Media Group">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationUtilities;
using GoogleDfpActivities.Measures;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpActivities
{
    /// <summary>
    /// Extensions for getting/setting AppNexus specific properties from DA measures
    /// </summary>
    public static class MeasureMapExtensions
    {
        /// <summary>Gets the Google DFP AdUnitIds</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The include AdUnitIds</returns>
        public static string[] GetDfpAdUnitIds(this MeasureMap @this, MeasureSet measureSet)
        {
            return GetDfpMeasures(@this, measureSet, AdUnitMeasureSource.TargetingType)
                .Select(measure =>
                    @this.GetNumericValueForMeasure(measure, DfpMeasureValues.DfpId))
                .Select(adUnitId => adUnitId.ToString(CultureInfo.InvariantCulture))
                .ToArray();
        }

        /// <summary>Gets the Google DFP placement ids</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The placement ids</returns>
        public static long[] GetDfpPlacementIds(this MeasureMap @this, MeasureSet measureSet)
        {
            return GetDfpMeasures(@this, measureSet, PlacementMeasureSource.TargetingType)
                .Select(measure =>
                    @this.GetNumericValueForMeasure(measure, DfpMeasureValues.DfpId))
                .ToArray();
        }

        /// <summary>Gets the Google DFP targeted locations</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The location ids</returns>
        public static long[] GetDfpLocationsIds(this MeasureMap @this, MeasureSet measureSet)
        {
            return GetDfpMeasures(@this, measureSet, LocationMeasureSource.TargetingType)
                .Select(measure =>
                    @this.GetNumericValueForMeasure(measure, DfpMeasureValues.DfpId))
                .ToArray();
        }

        /// <summary>Gets the Google DFP technology targeting</summary>
        /// <param name="this">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>The TechnologyTargeting</returns>
        public static Dfp.TechnologyTargeting GetDfpTechnologyTargeting(this MeasureMap @this, MeasureSet measureSet)
        {
            // Get technology targets as a sequence of technology targeting subtypes and ids
            var technologyTargets = GetTechnologyTargetsFromMeasures(@this, measureSet);
            var targeting = new Dfp.TechnologyTargeting
            {
                bandwidthGroupTargeting = new Dfp.BandwidthGroupTargeting
                {
                    bandwidthGroups = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.Bandwidth)
                },
                browserTargeting = new Dfp.BrowserTargeting
                {
                    browsers = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.Browser)
                },
                browserLanguageTargeting = new Dfp.BrowserLanguageTargeting
                {
                    browserLanguages = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.BrowserLanguage)
                },
                deviceCapabilityTargeting = new Dfp.DeviceCapabilityTargeting
                {
                    targetedDeviceCapabilities = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.DeviceCapability)
                },
                deviceManufacturerTargeting = new Dfp.DeviceManufacturerTargeting
                {
                    deviceManufacturers = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.DeviceManufacturer)
                },
                mobileCarrierTargeting = new Dfp.MobileCarrierTargeting
                {
                    mobileCarriers = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.MobileCarrier)
                },
                mobileDeviceSubmodelTargeting = new Dfp.MobileDeviceSubmodelTargeting
                {
                    targetedMobileDeviceSubmodels = GetTechnologySubTypeTargets(
                       technologyTargets,
                       TechnologyMeasureSubTypes.DeviceSubModel)
                },
                mobileDeviceTargeting = new Dfp.MobileDeviceTargeting
                {
                    targetedMobileDevices = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.DeviceModel)
                },
                operatingSystemTargeting = new Dfp.OperatingSystemTargeting
                {
                    operatingSystems = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.OperatingSystem)
                },
                operatingSystemVersionTargeting = new Dfp.OperatingSystemVersionTargeting
                {
                    targetedOperatingSystemVersions = GetTechnologySubTypeTargets(
                        technologyTargets,
                        TechnologyMeasureSubTypes.OperatingSystemVersion)
                }
            };

            return targeting;
        }

        /// <summary>
        /// Gets the DFP targeting measures of the specified DFP targeting type
        /// </summary>
        /// <param name="measureMap">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <param name="dfpMeasureType">DFP measure type</param>
        /// <returns>The DFP measures</returns>
        private static IEnumerable<long> GetDfpMeasures(
            MeasureMap measureMap,
            MeasureSet measureSet,
            string dfpMeasureType)
        {
            return measureSet
                .Where(measure => measureMap.GetMeasureType(measure) == dfpMeasureType);
        }

        /// <summary>
        /// Gets a sequence of DFP technology subtype/id tuples from a measure map and measure set
        /// </summary>
        /// <param name="measureMap">The MeasureMap</param>
        /// <param name="measureSet">The MeasureSet</param>
        /// <returns>Sequence of DFP technology subtype/id tuples</returns>
        private static IEnumerable<Tuple<string, long>> GetTechnologyTargetsFromMeasures(
            MeasureMap measureMap,
            MeasureSet measureSet)
        {
            return GetDfpMeasures(measureMap, measureSet, TechnologyMeasureSource.TargetingType)
                .Select(measure =>
                    new Tuple<string, long>(
                        measureMap.GetStringValueForMeasure(measure, MeasureValues.SubType),
                        measureMap.GetNumericValueForMeasure(measure, DfpMeasureValues.DfpId)));
        }

        /// <summary>Gets an array of DFP Technology targets for the specified subtype</summary>
        /// <param name="technologyTargets">Sequence of subtype/id technology target tuples</param>
        /// <param name="technologySubType">The subtype to include</param>
        /// <returns>Array of DFP Technology targets</returns>
        private static Dfp.Technology[] GetTechnologySubTypeTargets(
            IEnumerable<Tuple<string, long>> technologyTargets,
            string technologySubType)
        {
            return technologyTargets
                .Where(techTarget => techTarget.Item1 == technologySubType)
                .Select(techTarget => new Dfp.Technology { id = techTarget.Item2 })
                .ToArray();
        }
    }
}
