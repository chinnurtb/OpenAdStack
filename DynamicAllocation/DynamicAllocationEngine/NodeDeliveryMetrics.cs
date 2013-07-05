// -----------------------------------------------------------------------
// <copyright file="NodeDeliveryMetrics.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Diagnostics;

namespace DynamicAllocation
{
    /// <summary>The delivery metrics element for a single node.</summary>
    public class NodeDeliveryMetrics : IEffectiveNodeMetrics
    {
        /// <summary>Cache of calculated impression metrics.</summary>
        private readonly Dictionary<int, long> impressionCache = new Dictionary<int, long>();

        /// <summary>Cache of calculated impression rate metrics.</summary>
        private readonly Dictionary<int, decimal> impressionRateCache = new Dictionary<int, decimal>();
        
        /// <summary>Cache of calculated media spend metrics.</summary>
        private readonly Dictionary<int, decimal> mediaSpendCache = new Dictionary<int, decimal>();
        
        /// <summary>Cache of calculated media spend rate metrics.</summary>
        private readonly Dictionary<int, decimal> mediaSpendRateCache = new Dictionary<int, decimal>();

        /// <summary>Cache of calculated media spend rate metrics.</summary>
        private readonly DateTime minEligibleHour = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeDeliveryMetrics"/> class.
        /// </summary>
        public NodeDeliveryMetrics()
        {
            this.LastProcessedEligibilityHour = this.minEligibleHour;
            this.LastProcessedDeliveryHour = this.minEligibleHour;
            this.TotalImpressions = 0;
            this.TotalMediaSpend = 0;
            this.TotalSpend = 0;
            this.TotalEligibleHours = 0;
            this.DeliveryProfile = new Dictionary<int, NodeHourMetrics>();
        }

        /// <summary>
        /// Gets or sets last eligible hour reflected in this node element.
        /// </summary>
        public DateTime LastProcessedEligibilityHour { get; set; }

        /// <summary>
        /// Gets or sets last delivery hour reflected in this node element.
        /// </summary>
        public DateTime LastProcessedDeliveryHour { get; set; }

        /// <summary>
        /// Gets or sets total lifetime impressions for the node.
        /// </summary>
        public long TotalImpressions { get; set; }

        /// <summary>
        /// Gets or sets total lifetime media spend for the node.
        /// </summary>
        public decimal TotalMediaSpend { get; set; }

        /// <summary>
        /// Gets or sets total eligible hours for the node.
        /// </summary>
        public override sealed long TotalEligibleHours { get; set; }
            
        /// <summary>
        /// Gets or sets TotalSpend.
        /// </summary>
        public decimal TotalSpend { get; set; }

        /// <summary>
        /// Gets the delivery profile for the node.
        /// </summary>
        public Dictionary<int, NodeHourMetrics> DeliveryProfile { get; private set; }

        /// <summary>Calculate EffectiveMediaSpendRate.</summary>
        /// <returns>Effective media spend rate.</returns>
        public override decimal CalcEffectiveMediaSpendRate()
        {
            return this.CalculateEffectiveRate(lookBack => this.CalcMediaSpendRate(lookBack));
        }

        /// <summary>Calculate EffectiveImpressionRate.</summary>
        /// <returns>Effective impression rate.</returns>
        public override decimal CalcEffectiveImpressionRate()
        {
            return this.CalculateEffectiveRate(lookBack => this.CalcImpressionRate(lookBack));
        }

        /// <summary>Get the effective media spend for specified look-back duration.</summary>
        /// <param name="lookBack">The look-back duration.</param>
        /// <returns>Effective media spend.</returns>
        public override decimal CalcEffectiveMediaSpend(int lookBack)
        {
            if (lookBack == LifetimeLookBack)
            {
                return this.TotalMediaSpend;
            }

            return this.CalcEffectiveMediaSpendRate() * lookBack;
        }

        /// <summary>Get the effective impressions for specified look-back duration.</summary>
        /// <param name="lookBack">The look-back duration.</param>
        /// <returns>Effective impressions.</returns>
        public override long CalcEffectiveImpressions(int lookBack)
        {
            if (lookBack == LifetimeLookBack)
            {
                return this.TotalImpressions;
            }

            return (long)(this.CalcEffectiveImpressionRate() * lookBack);
        }

        /// <summary>
        /// Calculate hourly total spend based on the effective impression rate and effective media spend rate.
        /// </summary>
        /// <param name="measureInfo">The measure info object to use for cost calculation.</param>
        /// <param name="measureSet">The measure set.</param>
        /// <param name="lookBack">The look-back duration.</param>
        /// <param name="margin">The margin.</param>
        /// <param name="perMilleFees">The per mille fees.</param>
        /// <returns>Hourly total spend.</returns>
        public override decimal CalcEffectiveTotalSpend(
            MeasureInfo measureInfo, 
            MeasureSet measureSet, 
            int lookBack, 
            decimal margin, 
            decimal perMilleFees)
        {
            // Use the effective impression and media spend rates to get the effective total spend
            // for an hour of delivery
            var proxyImpressions = (long)Math.Round(this.CalcEffectiveImpressionRate(), 0);
            var proxyMediaSpend = this.CalcEffectiveMediaSpendRate();
            var hourlyTotalSpend = measureInfo.CalculateTotalSpend(
                measureSet, proxyImpressions, proxyMediaSpend, margin, perMilleFees);

            // Return the effective total spend for the period
            return hourlyTotalSpend * lookBack;
        }

        /// <summary>Calculate the media spend for a given look-back (-1 for lifetime)</summary>
        /// <param name="lookBack">Duration of look-back in hours.</param>
        /// <returns>Media spend.</returns>
        public decimal CalcMediaSpend(int lookBack)
        {
            Func<decimal> lifeTimeFunc = () => this.TotalMediaSpend;

            Func<Dictionary<int, NodeHourMetrics>, decimal> rangeFunc = hoursInRange =>
                {
                    var validHoursInRange = hoursInRange.Where(
                        bucket => bucket.Value.LastNMediaSpend.Count > 0 && bucket.Value.LastNImpressions.Count > 0).ToDictionary();

                    // TODO: Try to make this impossible - This shouldn't happen, but we will exclude records where it does.
                    if (validHoursInRange.Count() != hoursInRange.Count)
                    {
                        LogManager.Log(LogLevels.Warning, "DeliveryProfile contains invalid history elements.");
                    }

                    return validHoursInRange.Sum(bucket => bucket.Value.LastNMediaSpend[0]);
                };

            return this.CalcNodeDimension(this.mediaSpendCache, lifeTimeFunc, rangeFunc, lookBack);
        }

        /// <summary>Calculate the impressions for a given look-back (-1 for lifetime)</summary>
        /// <param name="lookBack">Duration of look-back in hours.</param>
        /// <returns>Impression count.</returns>
        public long CalcImpressions(int lookBack)
        {
            Func<long> lifeTimeFunc = () => this.TotalImpressions;

            Func<Dictionary<int, NodeHourMetrics>, long> rangeFunc = hoursInRange =>
            {
                var validHoursInRange = hoursInRange.Where(
                    bucket => bucket.Value.LastNMediaSpend.Count > 0 && bucket.Value.LastNImpressions.Count > 0).ToDictionary();

                // TODO: Try to make this impossible - This shouldn't happen, but we will exclude records where it does.
                if (validHoursInRange.Count() != hoursInRange.Count)
                {
                    LogManager.Log(LogLevels.Warning, "DeliveryProfile contains invalid history elements.");
                }

                return validHoursInRange.Sum(bucket => bucket.Value.LastNImpressions[0]);
            };

            return this.CalcNodeDimension(this.impressionCache, lifeTimeFunc, rangeFunc, lookBack);
        }

        /// <summary>Calculate the media spend rate for a given look-back (-1 for lifetime)</summary>
        /// <param name="lookBack">Duration of look-back in hours.</param>
        /// <returns>Media spend rate.</returns>
        public decimal CalcMediaSpendRate(int lookBack)
        {
            Func<decimal> lifeTimeFunc = () => 
                this.TotalEligibleHours > 0 ? Math.Round(this.TotalMediaSpend / this.TotalEligibleHours, 6) : 0;
            
            Func<Dictionary<int, NodeHourMetrics>, decimal> rangeFunc = hoursInRange =>
            {
                var mediaSpend = hoursInRange.Sum(bucket => bucket.Value.AverageMediaSpend);
                long hours = hoursInRange.Count(bucket => bucket.Value.EligibilityCount > 0);
                return hours > 0 ? Math.Round(mediaSpend / hours, 6) : 0;
            };

            return this.CalcNodeDimension(this.mediaSpendRateCache, lifeTimeFunc, rangeFunc, lookBack);
        }

        /// <summary>Calculate the impression rate for a given look-back (-1 for lifetime)</summary>
        /// <param name="lookBack">Duration of look-back in hours.</param>
        /// <returns>Impression rate.</returns>
        public decimal CalcImpressionRate(int lookBack)
        {
            Func<decimal> lifeTimeFunc = () => 
                this.TotalEligibleHours > 0 ? Math.Round((decimal)this.TotalImpressions / this.TotalEligibleHours, 6) : 0;

            Func<Dictionary<int, NodeHourMetrics>, decimal> rangeFunc = hoursInRange =>
            {
                var impressions = hoursInRange.Sum(bucket => bucket.Value.AverageImpressions);
                long hours = hoursInRange.Count(bucket => bucket.Value.EligibilityCount > 0);
                return hours > 0 ? Math.Round(impressions / hours, 6) : 0;
            };

            return this.CalcNodeDimension(this.impressionRateCache, lifeTimeFunc, rangeFunc, lookBack);
        }
        
        /// <summary>Get an index into a 168 hour array representing seven days of delivery starting 00:00 Sunday.</summary>
        /// <param name="deliveryHour">The utc delivery hour.</param>
        /// <returns>An index into the array.</returns>
        internal static int GetProfileHourIndex(DateTime deliveryHour)
        {
            var profileHour = ((int)deliveryHour.DayOfWeek * 24) + deliveryHour.Hour;
            return profileHour;
        }

        /// <summary>Get the hour metrics in the lookback period of interest</summary>
        /// <param name="startHour">Save date of the latest raw delivery data.</param>
        /// <param name="lookbackDuration">Time to look back.</param>
        /// <returns>Hour metrics within the lookback period.</returns>
        internal Dictionary<int, NodeHourMetrics> GetHourMetricsInRange(DateTime startHour, TimeSpan lookbackDuration)
        {
            var hourMetricsInRange = new Dictionary<int, NodeHourMetrics>();

            // If we have no delivery data, nothing to do
            if (this.DeliveryProfile.Count == 0)
            {
                return hourMetricsInRange;
            }

            // lastProcessedEligibleHour is only updated for non-zero delivery to be more
            // robust in high report latency scenarios. So we could have eligiblity
            // without advancing the lastProcessEligibleHour.
            if (this.minEligibleHour + lookbackDuration >= startHour)
            {
                return hourMetricsInRange;
            }

            // This is a circular buffer and we need to wrap around by walking back in time rather
            // than doing a simple Where statement.
            for (var hour = startHour; hour > (startHour - lookbackDuration); hour = hour.AddHours(-1))
            {
                var bucket = GetProfileHourIndex(hour);
                if (this.DeliveryProfile.ContainsKey(bucket))
                {
                    hourMetricsInRange[bucket] = this.DeliveryProfile[bucket];
                }
            }

            return hourMetricsInRange;
        }

        /// <summary>Method to calculate a parameterized dimension of a node.</summary>
        /// <param name="metricCache">The metric cache for the dimension.</param>
        /// <param name="lifeTimeFunc">The function for calculating for the dimension for life-time.</param>
        /// <param name="rangeFunc">The function for the calculating the dimension for look-back range.</param>
        /// <param name="lookBack">The look back (-1 for lifetime).</param>
        /// <typeparam name="T">The type of the dimension.</typeparam>
        /// <returns>The calculated dimension.</returns>
        private T CalcNodeDimension<T>(
            Dictionary<int, T> metricCache,
            Func<T> lifeTimeFunc,
            Func<Dictionary<int, NodeHourMetrics>, T> rangeFunc,
            int lookBack)
        {
            if (this.mediaSpendCache.ContainsKey(lookBack))
            {
                return metricCache[lookBack];
            }

            if (lookBack == LifetimeLookBack)
            {
                var lifeTimeValue = lifeTimeFunc();
                metricCache[lookBack] = lifeTimeValue;
                return lifeTimeValue;
            }

            var startingHour = this.LastProcessedEligibilityHour;
            var lookbackDuration = new TimeSpan(lookBack, 0, 0);
            var hourMetricsInRange = this.GetHourMetricsInRange(startingHour, lookbackDuration);

            var rangeValue = rangeFunc(hourMetricsInRange);
            metricCache[lookBack] = rangeValue;
            return rangeValue;
        }

        /// <summary>Calculate a delivery rate dimension for a given duration.</summary>
        /// <param name="metricsFunc">The rate calculation function.</param>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <returns>The calculated delivery rate dimension.</returns>
        private T CalculateEffectiveRate<T>(Func<int, T> metricsFunc)
        {
            // Whether we are calculating impression rate or media spend rate we use the impression
            // rate to determine the appropriate look-back
            var impressionRateWeek = this.CalcImpressionRate(OneWeekLookBack);
            if (impressionRateWeek == 0)
            {
                return metricsFunc(LifetimeLookBack);
            }

            var impressionRateTwoDay = this.CalcImpressionRate(TwoDayLookBack);
            if (impressionRateWeek > impressionRateTwoDay)
            {
                return metricsFunc(OneWeekLookBack);
            }

            return metricsFunc(TwoDayLookBack);
        }
    }
}