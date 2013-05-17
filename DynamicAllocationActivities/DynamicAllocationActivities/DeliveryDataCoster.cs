// -----------------------------------------------------------------------
// <copyright file="DeliveryDataCoster.cs" company="Emerging Media Group">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Wrap delivery data cost calculations helpers.</summary>
    internal class DeliveryDataCoster : IDeliveryDataCost
    {
        /// <summary>Initializes a new instance of the <see cref="DeliveryDataCoster"/> class.</summary>
        /// <param name="measureMap">The measure map.</param>
        /// <param name="margin">The margin.</param>
        /// <param name="perMilleFees">The per mille fees.</param>
        internal DeliveryDataCoster(MeasureMap measureMap, decimal margin, decimal perMilleFees)
        {
            this.MeasureMap = measureMap;
            this.Margin = margin;
            this.PerMilleFees = perMilleFees;
        }

        /// <summary>Gets MeasureMap.</summary>
        internal MeasureMap MeasureMap { get; private set; }

        /// <summary>Gets Margin.</summary>
        internal decimal Margin { get; private set; }

        /// <summary>Gets PerMilleFees.</summary>
        internal decimal PerMilleFees { get; private set; }

        /// <summary>Calculate the costs associated with data delivery for a single hour.</summary>
        /// <param name="impressions">Impression count.</param>
        /// <param name="mediaSpend">Media spend.</param>
        /// <param name="measureSet">Measure set of node.</param>
        /// <returns>Delivery costs.</returns>
        public decimal CalculateHourCost(long impressions, decimal mediaSpend, MeasureSet measureSet)
        {
            var measureInfo = new MeasureInfo(this.MeasureMap);
            return measureInfo.CalculateTotalSpend(measureSet, impressions, mediaSpend, this.Margin, this.PerMilleFees);
        }
    }
}