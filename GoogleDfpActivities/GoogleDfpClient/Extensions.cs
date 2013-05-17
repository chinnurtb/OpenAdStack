//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using ConfigManager;
using Diagnostics;
using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.Util.v201206;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpClient
{
    /// <summary>Extensions useful for working with DFP</summary>
    internal static class Extensions
    {
        /// <summary>
        /// Converts a System.DateTime to its equivalent DFP DateTime
        /// </summary>
        /// <param name="dateTime">System.DateTime (UTC)</param>
        /// <returns>An equivalent DFP DateTime</returns>
        public static Dfp.DateTime ToDfpDateTime(this System.DateTime dateTime)
        {
            var dfpDateTime = DateTimeUtilities.FromDateTime(dateTime.ToUniversalTime());
            dfpDateTime.timeZoneID = "UTC";
            return dfpDateTime;
        }

        /// <summary>Converts a DFP DateTime to its equivalent System.DateTime</summary>
        /// <param name="dateTime">DFP DateTime (local time)</param>
        /// <param name="timeZoneId">TimeZone ID for the DFP DateTime</param>
        /// <returns>An equivalent System.DateTime (UTC)</returns>
        public static System.DateTime ToSystemDateTime(this Dfp.DateTime dateTime, string timeZoneId)
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return ToSystemDateTime(dateTime, timeZoneInfo);
        }

        /// <summary>Converts a DFP DateTime to its equivalent System.DateTime</summary>
        /// <param name="dateTime">DFP DateTime (local time)</param>
        /// <param name="timeZoneInfo">TimeZoneInfo for the DFP DateTime</param>
        /// <returns>An equivalent System.DateTime (UTC)</returns>
        public static System.DateTime ToSystemDateTime(this Dfp.DateTime dateTime, TimeZoneInfo timeZoneInfo)
        {
            var systemDateTime = new System.DateTime(
                dateTime.date.year,
                dateTime.date.month,
                dateTime.date.day,
                dateTime.hour,
                dateTime.minute,
                dateTime.second,
                dateTime.timeZoneID == "UTC" ? DateTimeKind.Utc : DateTimeKind.Unspecified);
            if (systemDateTime.Kind == DateTimeKind.Utc)
            {
                return systemDateTime;
            }

            return TimeZoneInfo.ConvertTimeToUtc(systemDateTime, timeZoneInfo);
        }

        /// <summary>Gets a copy of a DateTime floored to seconds</summary>
        /// <param name="dateTime">The DateTime</param>
        /// <returns>The floored DateTime</returns>
        public static System.DateTime FloorToSeconds(this System.DateTime dateTime)
        {
            return new DateTime(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second,
                dateTime.Kind);
        }

        /// <summary>Invoke a lambda using a DFP service.</summary>
        /// <remarks>Logs any exceptions and rethrows DfpExceptions as GoogleDfpClientExceptions.</remarks>
        /// <typeparam name="TDfpService">Type of the DFP service</typeparam>
        /// <typeparam name="TResult">Type of the result of the lambda</typeparam>
        /// <param name="service">The service</param>
        /// <param name="lambda">The lambda to execute</param>
        /// <returns>The result of the lambda</returns>
        public static TResult Invoke<TDfpService, TResult>(this TDfpService service, Func<TDfpService, TResult> lambda)
            where TDfpService : DfpSoapClient
        {
            try
            {
                return lambda(service);
            }
            catch (DfpException dfpe)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "A Google DFP error occured while executing service call using '{0}': {1}",
                    typeof(TDfpService).FullName,
                    dfpe);
                throw new GoogleDfpClientException(dfpe);
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "An unknown error occured while executing service call '{0}': {1}",
                    typeof(TDfpService).FullName,
                    e);
                throw;
            }
        }

        /// <summary>
        /// Checks whether the TechnologyTargeting contains mobile targets
        /// </summary>
        /// <param name="technologyTargeting">The TechnologyTargeting</param>
        /// <returns>True if the TechnologyTargeting contains mobile targets; otherwise, false.</returns>
        public static bool ContainsMobileTargets(this Dfp.TechnologyTargeting technologyTargeting)
        {
            return
                (technologyTargeting.deviceCapabilityTargeting != null && technologyTargeting.deviceCapabilityTargeting.targetedDeviceCapabilities.Length > 0) ||
                (technologyTargeting.deviceManufacturerTargeting != null && technologyTargeting.deviceManufacturerTargeting.deviceManufacturers.Length > 0) ||
                (technologyTargeting.mobileCarrierTargeting != null && technologyTargeting.mobileCarrierTargeting.mobileCarriers.Length > 0) ||
                (technologyTargeting.mobileDeviceSubmodelTargeting != null && technologyTargeting.mobileDeviceSubmodelTargeting.targetedMobileDeviceSubmodels.Length > 0) ||
                (technologyTargeting.mobileDeviceTargeting != null && technologyTargeting.mobileDeviceTargeting.targetedMobileDevices.Length > 0);
        }
    }
}
