// -----------------------------------------------------------------------
// <copyright file="IDeliveryDataCost.cs" company="Emerging Media Group">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Data coster interface.</summary>
    public interface IDeliveryDataCost
    {
        /// <summary>Calculate the costs associated with data delivery for a single hour.</summary>
        /// <param name="impressions">Impression count.</param>
        /// <param name="mediaSpend">Media spend.</param>
        /// <param name="measureSet">Measure set of node.</param>
        /// <returns>Delivery costs.</returns>
        decimal CalculateHourCost(long impressions, decimal mediaSpend, MeasureSet measureSet);
    }
}