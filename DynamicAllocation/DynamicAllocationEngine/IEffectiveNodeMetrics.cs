// -----------------------------------------------------------------------
// <copyright file="IEffectiveNodeMetrics.cs" company="Rare Crowds Inc">
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

namespace DynamicAllocation
{
    /// <summary>Interface for NodeDeliveryMetrics</summary>
    public abstract class IEffectiveNodeMetrics
    {
        /// <summary>Lookback value to get lifetime metrics.</summary>
        public const int LifetimeLookBack = -1;

        /// <summary>Lookback value to get two metrics.</summary>
        public const int TwoDayLookBack = 48;

        /// <summary>Lookback value to get one week metrics.</summary>
        public const int OneWeekLookBack = 168;

        /// <summary>
        /// Gets or sets total eligible hours for the node.
        /// </summary>
        public abstract long TotalEligibleHours { get; set; }

        /// <summary>Calculate EffectiveMediaSpendRate.</summary><returns>Effective media spend rate.</returns>
        public abstract decimal CalcEffectiveMediaSpendRate();

        /// <summary>Calculate EffectiveImpressionRate.</summary><returns>Effective impression rate.</returns>
        public abstract decimal CalcEffectiveImpressionRate();

        /// <summary>Get the effective media spend for specified look-back duration.</summary><param name="lookBack">The look-back duration.</param><returns>Effective media spend.</returns>
        public abstract decimal CalcEffectiveMediaSpend(int lookBack);

        /// <summary>Get the effective impressions for specified look-back duration.</summary><param name="lookBack">The look-back duration.</param><returns>Effective impressions.</returns>
        public abstract long CalcEffectiveImpressions(int lookBack);

        /// <summary>
        /// Calculate hourly total spend based on the effective impression rate and effective media spend rate.
        /// </summary><param name="measureInfo">The measure info object to use for cost calculation.</param><param name="measureSet">The measure set.</param><param name="lookBack">The look-back duration.</param><param name="margin">The margin.</param><param name="perMilleFees">The per mille fees.</param><returns>Hourly total spend.</returns>
        public abstract decimal CalcEffectiveTotalSpend(MeasureInfo measureInfo, MeasureSet measureSet, int lookBack, decimal margin, decimal perMilleFees);
    }
}