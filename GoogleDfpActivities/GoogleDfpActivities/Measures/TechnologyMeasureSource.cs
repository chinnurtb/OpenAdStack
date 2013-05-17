//-----------------------------------------------------------------------
// <copyright file="TechnologyMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataAccessLayer;
using DynamicAllocation;
using GoogleDfpClient;
using Newtonsoft.Json;
using Utilities.Storage;
using Dfp = Google.Api.Ads.Dfp.v201206;
using DfpUtils = Google.Api.Ads.Dfp.Util.v201206;

namespace GoogleDfpActivities.Measures
{
    /// <summary>Measure source for Google DFP Technologies</summary>
    internal sealed class TechnologyMeasureSource : DfpMeasureSourceBase, IMeasureSource
    {
        /// <summary>Measure type for technology measures</summary>
        public const string TargetingType = "technology";

        /// <summary>Measure id network prefix for Google DFP measures</summary>
        public const byte TechnologyMeasureIdPrefix = 4;

        /// <summary>Name for the Technology measure source</summary>
        public const string TechnologyMeasureSourceName = "technologies";

        /// <summary>Longevity of the Technology measures cache</summary>
        internal static readonly TimeSpan TechnologyCacheLongevity = new TimeSpan(6, 0, 0);

        /// <summary>Measure provider id</summary>
        private const int DataProvider = -1;

        /// <summary>Initializes a new instance of the TechnologyMeasureSource class</summary>
        /// <param name="companyEntity">CompanyEntity (for config)</param>
        /// <param name="campaignEntity">CampaignEntity (for config)</param>
        public TechnologyMeasureSource(CompanyEntity companyEntity, CampaignEntity campaignEntity)
            : base(TechnologyMeasureIdPrefix, TechnologyMeasureSourceName, companyEntity, campaignEntity)
        {
        }

        /// <summary>Gets the category display name</summary>
        protected override string CategoryDisplayName
        {
            get { return "Technology"; }
        }

        /// <summary>Gets the targeting type</summary>
        protected override string MeasureType
        {
            get { return TargetingType; }
        }

        /// <summary>Gets the display name for a browser</summary>
        /// <param name="row">
        /// Result row containing browsername, majorversion, minorversion
        /// </param>
        /// <returns>Get the display name for a browser row</returns>
        internal static string GetBrowserVersionDisplayName(
            IDictionary<string, object> row)
        {
            var browserName = (string)row["browsername"];
            var majorVersion = ((string)row["majorversion"]).ToUpperInvariant();
            var minorVersion = ((string)row["minorversion"]).ToUpperInvariant();

            var sb = new StringBuilder(browserName);
            sb.Append(" ");

            switch (majorVersion)
            {
                case "ANY":
                    sb.Append("*");
                    break;
                case "OTHER":
                    sb.Append("(Other)");
                    break;
                default:
                    sb.Append(majorVersion);
                    break;
            }

            switch (minorVersion)
            {
                case "ANY":
                    sb.Append(".*");
                    break;
                case "OTHER":
                    if (majorVersion != "OTHER")
                    {
                        sb.Append(" (Other)");
                    }

                    break;
                default:
                    sb.AppendFormat(".{0}", minorVersion);
                    break;
            }

            return sb.ToString();
        }

        /// <summary>Gets the display name for an operating system version</summary>
        /// <param name="row">Result row containing os and major/minor/micro versions</param>
        /// <param name="operatingSystems">Dictionary of operating system names</param>
        /// <returns>The operating system version display name</returns>
        internal static string GetOperatingSystemVersionDisplayName(
            IDictionary<string, object> row,
            IDictionary<object, string> operatingSystems)
        {
            var operatingSystem = operatingSystems[(long)row["operatingsystemid"]];
            var majorVersion = (long)row["majorversion"];
            var minorVersion = (long)row["minorversion"];
            var microVersion = (long)row["microversion"];
            
            var sb = new StringBuilder(operatingSystem);
            sb.AppendFormat(":{0} {1}", operatingSystem, majorVersion);

            if (minorVersion >= 0)
            {
                sb.AppendFormat(".{0}", minorVersion);
            }

            if (microVersion >= 0)
            {
                sb.AppendFormat(".{0}", microVersion);
            }

            return sb.ToString();
        }

        /// <summary>Fetch the latest Technology measure map</summary>
        /// <returns>The latest MeasureMap</returns>
        protected override MeasureMapCacheEntry FetchLatestMeasureMap()
        {
            var technologies =
                this.BandwidthGroups()
                .Concat(this.Browsers())
                .Concat(this.BrowserLanguage())
                .Concat(this.DeviceCapability())
                .Concat(this.DeviceCategory())
                .Concat(this.MobileDevices())
                .Concat(this.MobileCarrier())
                .Concat(this.OperatingSystem());

            var measures = technologies
                .ToDictionary(
                    row => this.GetMeasureId((long)row[DfpMeasureValues.DfpId]),
                    row => row);                

            return new MeasureMapCacheEntry
            {
                Expiry = DateTime.UtcNow + TechnologyCacheLongevity,
                MeasureMapJson = JsonConvert.SerializeObject(measures)
            };
        }

        /// <summary>Gets bandwidth group technology meaures</summary>
        /// <returns>Bandwidth group technology measures</returns>
        private IEnumerable<IDictionary<string, object>> BandwidthGroups()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, bandwidthname from bandwidth_group")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Bandwidth Groups",
                        row["bandwidthname"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.Bandwidth));
        }

        /// <summary>Gets browser technology meaures</summary>
        /// <returns>Browser technology measures</returns>
        private IEnumerable<IDictionary<string, object>> Browsers()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, browsername, majorversion, minorversion from browser")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Browsers",
                        row["browsername"],
                        GetBrowserVersionDisplayName(row)
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.Browser));
        }

        /// <summary>Gets browser technology meaures</summary>
        /// <returns>Browser technology measures</returns>
        private IEnumerable<IDictionary<string, object>> BrowserLanguage()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, browserlanguagename from browser_language")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Browser Languages",
                        row["browserlanguagename"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.BrowserLanguage));
        }

        /// <summary>Gets device capability technology meaures</summary>
        /// <returns>Device capability technology measures</returns>
        private IEnumerable<IDictionary<string, object>> DeviceCapability()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, devicecapabilityname from device_capability")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Mobile Device",
                        "Capability",
                        row["devicecapabilityname"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.DeviceCapability));
        }

        /// <summary>Gets device category technology meaures</summary>
        /// <returns>Device category technology measures</returns>
        private IEnumerable<IDictionary<string, object>> DeviceCategory()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, devicecategoryname from device_category")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Mobile Device",
                        "Category",
                        row["devicecategoryname"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.DeviceCategory));
        }

        /// <summary>Gets mobile device technology meaures</summary>
        /// <remarks>
        /// Rolls up mobile device manufacturers, models and submodels
        /// </remarks>
        /// <returns>Mobile device technology measures</returns>
        private IEnumerable<IDictionary<string, object>> MobileDevices()
        {
            var manufacturers = this.MobileDeviceManufacturers();
            var models = this.MobileDeviceModels(manufacturers);
            var submodels = this.MobileDeviceSubmodels(models);
            return manufacturers.Concat(models).Concat(submodels);
        }

        /// <summary>Gets device manufacturer technology meaures</summary>
        /// <remarks>Rolled up by MobileDevices(), do not include directly.</remarks>
        /// <returns>Device manufacturer technology measures</returns>
        private IEnumerable<IDictionary<string, object>> MobileDeviceManufacturers()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, mobiledevicemanufacturername from device_manufacturer")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Mobile Device",
                        "Manufacturer",
                        row["mobiledevicemanufacturername"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.DeviceManufacturer));
        }

        /// <summary>Gets device model technology meaures</summary>
        /// <remarks>Rolled up by MobileDevices(), do not include directly.</remarks>
        /// <param name="manufacturerMeasures">Manufacturer measures</param>
        /// <returns>Device manufacturer technology measures</returns>
        private IEnumerable<IDictionary<string, object>> MobileDeviceModels(
            IEnumerable<IDictionary<string, object>> manufacturerMeasures)
        {
            var manufacturers = manufacturerMeasures.ToDictionary(
                mfr => mfr[DfpMeasureValues.DfpId],
                mfr => ((string)mfr[MeasureValues.DisplayName]).Split(':').Last());
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, mobiledevicemanufacturerid, mobiledevicename from mobile_device")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Mobile Device",
                        "Model",
                        manufacturers.ContainsKey(row["mobiledevicemanufacturerid"]) ?
                            manufacturers[row["mobiledevicemanufacturerid"]] :
                            "(Unknown Manufacturer)",
                        row["mobiledevicename"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.DeviceModel));
        }

        /// <summary>Gets device submodel technology meaures</summary>
        /// <remarks>Rolled up by MobileDevices(), do not include directly.</remarks>
        /// <param name="modelMeasures">Model measures</param>
        /// <returns>Device manufacturer technology measures</returns>
        private IEnumerable<IDictionary<string, object>> MobileDeviceSubmodels(
            IEnumerable<IDictionary<string, object>> modelMeasures)
        {
            var models = modelMeasures.ToDictionary(
                mfr => mfr[DfpMeasureValues.DfpId],
                mfr =>
                {
                    var modelNameParts = ((string)mfr[MeasureValues.DisplayName]).Split(':');
                    return "{0} {1}"
                        .FormatInvariant(
                            modelNameParts[modelNameParts.Length - 2],
                            modelNameParts[modelNameParts.Length - 1]);
                });

            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, mobiledeviceid, mobiledevicesubmodelname from mobile_device_submodel")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Mobile Device",
                        "Submodel",
                        models.ContainsKey(row["mobiledeviceid"]) ?
                            models[row["mobiledeviceid"]] :
                            "(Unknown Model)",
                        row["mobiledevicesubmodelname"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.DeviceSubModel));
        }

        /// <summary>Gets mobile carrier technology meaures</summary>
        /// <returns>Mobile carrier technology measures</returns>
        private IEnumerable<IDictionary<string, object>> MobileCarrier()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, mobilecarriername, countrycode from mobile_carrier")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Mobile Carrier",
                        row["countrycode"],
                        row["mobilecarriername"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.MobileCarrier));
        }

        /// <summary>Gets operating system technology meaures</summary>
        /// <returns>Operating system technology measures</returns>
        private IEnumerable<IDictionary<string, object>> OperatingSystem()
        {
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, operatingsystemname from operating_system")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            var operatingSystemMeasures = results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Operating System",
                        row["operatingsystemname"]
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.OperatingSystem));
            return
                operatingSystemMeasures
                .Concat(this.OperatingSystemVersion(operatingSystemMeasures));
        }

        /// <summary>Gets operating system technology meaures</summary>
        /// <remarks>Rolled up by OperatingSystem(). Do not include directly.</remarks>
        /// <param name="operatingSystemMeasures">Operating system measures</param>
        /// <returns>Operating system technology measures</returns>
        private IEnumerable<IDictionary<string, object>> OperatingSystemVersion(
            IEnumerable<IDictionary<string, object>> operatingSystemMeasures)
        {
            var operatingSystems = operatingSystemMeasures.ToDictionary(
                os => os[DfpMeasureValues.DfpId],
                os => ((string)os[MeasureValues.DisplayName]).Split(':').Last());
            var statement = new DfpUtils.StatementBuilder(
                "SELECT id, operatingsystemid, majorversion, minorversion, microversion from operating_system_version")
                .ToStatement();
            var results = this.DfpClient.SelectQuery(statement);
            return results
                .Select(row => CreateDfpMeasure(
                    new[]
                    {
                        "Operating System Version",
                        GetOperatingSystemVersionDisplayName(row, operatingSystems)
                    },
                    (long)row["id"],
                    TechnologyMeasureSubTypes.OperatingSystemVersion));
        }
    }
}
