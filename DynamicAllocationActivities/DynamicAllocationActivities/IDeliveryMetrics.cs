// -----------------------------------------------------------------------
// <copyright file="IDeliveryMetrics.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Interface for DeliveryMetrics members</summary>
    public interface IDeliveryMetrics
    {
        /// <summary>
        /// Gets helper object to encapsulate data costing
        /// </summary>
        IDeliveryDataCost DeliveryDataCost { get; }

        /// <summary>
        /// Gets RemainingBudget.
        /// </summary>
        decimal RemainingBudget { get; }

        /// <summary>
        /// Gets LifetimeMediaBudgetCap.
        /// </summary>
        decimal LifetimeMediaBudgetCap { get; }

        /// <summary>
        /// Gets NodeMetricsCollection.
        /// </summary>
        Dictionary<MeasureSet, NodeDeliveryMetrics> NodeMetricsCollection { get; }

        /// <summary>
        /// Gets PreviousLatestCampaignDeliveryHour.
        /// </summary>
        DateTime PreviousLatestCampaignDeliveryHour { get; }

        /// <summary>Calculate lifetime metrics that require iteration of the campaign delivery history.</summary>
        /// <param name="canonicalDeliveryData">The delivery data lookback set for this DA Campaign as a dictionary.</param>
        /// <param name="eligibilityHistoryBuilder">The eligibility history lookback set.</param>
        /// <param name="nodeMap">The of allocationIds to measureSets.</param>
        /// <param name="totalBudget">budget for campaign.</param>
        void CalculateNodeMetrics(
            ICanonicalDeliveryData canonicalDeliveryData,
            IEligibilityHistoryBuilder eligibilityHistoryBuilder,
            Dictionary<string, MeasureSet> nodeMap,
            decimal totalBudget);
    }
}