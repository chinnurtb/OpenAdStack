//-----------------------------------------------------------------------
// <copyright file="AppNexusThrottle.cs" company="Rare Crowds Inc">
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
using System.Runtime.Serialization;
using System.Threading;
using Diagnostics;
using Microsoft.Http;
using Utilities.Net;
using Utilities.Storage;

namespace AppNexusClient
{
    /// <summary>Throttle for the AppNexus client</summary>
    internal class AppNexusThrottle
    {
        /// <summary>Seconds by which to buffer the throttle period</summary>
        public const int PeriodBufferSeconds = 15;

        /// <summary>Singleton instance</summary>
        private static AppNexusThrottle instance = new AppNexusThrottle();

        /// <summary>When the throttle period started</summary>
        private DateTime periodStart;

        /// <summary>Information about read requests</summary>
        private ThrottleInfo readInfo;

        /// <summary>Information about write requests</summary>
        private ThrottleInfo writeInfo;

        /// <summary>
        /// Prevents a default instance of the AppNexusThrottle class from being created
        /// </summary>
        private AppNexusThrottle()
        {
            this.readInfo = new ThrottleInfo();
            this.writeInfo = new ThrottleInfo();
            this.periodStart = DateTime.UtcNow;
        }

        /// <summary>Gets when the next throttle quota period starts</summary>
        public static DateTime NextPeriodStart
        {
            get
            {
                // ReadInfo and WriteInfo RequestLimitSeconds should be the same.
                // However, take the max just to be safe.
                var requestLimitSeconds = Math.Max(
                    instance.readInfo.RequestLimitSeconds,
                    instance.writeInfo.RequestLimitSeconds);
                return instance.periodStart
                    .AddSeconds(requestLimitSeconds)
                    .AddSeconds(PeriodBufferSeconds);
            }
        }

        /// <summary>
        /// Inspects the HttpResponseMessage for information related to throttling
        /// </summary>
        /// <param name="httpMethod">The method of the request</param>
        /// <param name="debugInfo">Debug info from the API call</param>
        public static void UpdateThrottleInfo(string httpMethod, IDictionary<string, object> debugInfo)
        {
            try
            {
                // Reset the limit period start and
                // update the read or write info
                if (httpMethod == HttpMethod.GET.ToString())
                {
                    UpdateThrottleInfo(
                        ref instance.readInfo,
                        (int)debugInfo["reads"],
                        (int)debugInfo["read_limit"],
                        (int)debugInfo["read_limit_seconds"]);
                }
                else
                {
                    UpdateThrottleInfo(
                        ref instance.writeInfo,
                        (int)debugInfo["writes"],
                        (int)debugInfo["write_limit"],
                        (int)debugInfo["write_limit_seconds"]);
                }
            }
            catch (KeyNotFoundException)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Response debug info did not contain throttle information:\n{0}",
                    debugInfo.ToString<string, object>());
                return;
            }
        }

        /// <summary>Updates the throttle information</summary>
        /// <param name="throttleInfo">Throttle info to update</param>
        /// <param name="requests">Numper of requests</param>
        /// <param name="requestLimit">Request limit</param>
        /// <param name="requestLimitSeconds">Request limit period</param>
        private static void UpdateThrottleInfo(ref ThrottleInfo throttleInfo, int requests, int requestLimit, int requestLimitSeconds)
        {
            lock (instance)
            {
                if (requests < throttleInfo.Requests)
                {
                    instance.periodStart = DateTime.UtcNow;
                }

                throttleInfo = new ThrottleInfo
                {
                    LastRequest = DateTime.UtcNow,
                    Requests = requests,
                    RequestLimit = requestLimit,
                    RequestLimitSeconds = requestLimitSeconds
                };
            }
        }

        /// <summary>Contains throttle information for requests</summary>
        [DataContract]
        internal class ThrottleInfo
        {
            /// <summary>
            /// Gets or sets the period (in seconds) to which the request limit applies
            /// </summary>
            [DataMember]
            public int RequestLimitSeconds { get; set; }

            /// <summary>
            /// Gets or sets the number of requests that may be made during the limit period
            /// </summary>
            [DataMember]
            public int RequestLimit { get; set; }

            /// <summary>
            /// Gets or sets the number of requests made in the current limit period
            /// </summary>
            [DataMember]
            public int Requests { get; set; }

            /// <summary>Gets or sets the time of the last request</summary>
            [DataMember]
            public DateTime LastRequest { get; set; }

            /// <summary>Gets the number of requests remaining</summary>
            public int RequestsRemaining
            {
                get { return this.RequestLimit - this.Requests; }
            }

            /// <summary>Gets the percent of the limit reached</summary>
            public double LimitPercent
            {
                get { return (double)this.RequestsRemaining / (double)this.RequestLimit; }
            }
        }
    }
}
