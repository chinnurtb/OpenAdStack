// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FakeEffectiveNodeMetrics.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using DynamicAllocation;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Implementation of IEffectiveNodeMetrics for testing.
    /// </summary>
    internal class FakeEffectiveNodeMetrics : IEffectiveNodeMetrics
    {
        /// <summary>
        /// Gets or sets total eligible hours for the node.
        /// </summary>
        public override long TotalEligibleHours { get; set; }

        /// <summary>
        /// Gets or sets EffectiveMediaSpendRate.
        /// </summary>
        internal decimal EffectiveMediaSpendRate { get; set; }

        /// <summary>
        /// Gets or sets EffectiveImpressionRate.
        /// </summary>
        internal decimal EffectiveImpressionRate { get; set; }

        /// <summary>
        /// Gets or sets EffectiveMediaSpend.
        /// </summary>
        internal decimal EffectiveMediaSpend { get; set; }

        /// <summary>
        /// Gets or sets EffectiveImpressions.
        /// </summary>
        internal long EffectiveImpressions { get; set; }

        /// <summary>
        /// Gets or sets EffectiveTotalSpend.
        /// </summary>
        internal decimal EffectiveTotalSpend { get; set; }

        /// <summary>Calculate EffectiveMediaSpendRate.</summary>
        /// <returns>Effective media spend rate.</returns>
        public override decimal CalcEffectiveMediaSpendRate()
        {
            return this.EffectiveMediaSpendRate;
        }

        /// <summary>Calculate EffectiveImpressionRate.</summary>
        /// <returns>Effective impression rate.</returns>
        public override decimal CalcEffectiveImpressionRate()
        {
            return this.EffectiveImpressionRate;
        }

        /// <summary>Get the effective media spend for specified look-back duration.</summary>
        /// <param name="lookBack">The look-back duration.</param>
        /// <returns>Effective media spend.</returns>
        public override decimal CalcEffectiveMediaSpend(int lookBack)
        {
            return this.EffectiveMediaSpend;
        }

        /// <summary>Get the effective impressions for specified look-back duration.</summary>
        /// <param name="lookBack">The look-back duration.</param>
        /// <returns>Effective impressions.</returns>
        public override long CalcEffectiveImpressions(int lookBack)
        {
            return this.EffectiveImpressions;
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
        public override decimal CalcEffectiveTotalSpend(MeasureInfo measureInfo, MeasureSet measureSet, int lookBack, decimal margin, decimal perMilleFees)
        {
            return this.EffectiveTotalSpend;
        }
    }
}