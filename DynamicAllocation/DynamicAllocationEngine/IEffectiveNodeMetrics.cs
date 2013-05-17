// -----------------------------------------------------------------------
// <copyright file="IEffectiveNodeMetrics.cs" company="Emerging Media Group">
//  Copyright Rare Crowds Inc. All rights reserved.
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