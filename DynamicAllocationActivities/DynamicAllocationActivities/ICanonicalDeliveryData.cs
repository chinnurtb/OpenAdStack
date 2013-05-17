// -----------------------------------------------------------------------
// <copyright file="ICanonicalDeliveryData.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataAccessLayer;
using DynamicAllocation;

namespace DynamicAllocationActivities
{
    /// <summary>Interface definition for CanonicalDeliveryData</summary>
    public interface ICanonicalDeliveryData
    {
        /// <summary>
        /// Gets or sets Network the delivery data comes from.
        /// </summary>
        DeliveryNetworkDesignation Network { get; set; }

        /// <summary>
        ///  Gets DeliveryDataForNetwork.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "It's a collection of rows from a csv.")]
        IList<Dictionary<string, PropertyValue>> DeliveryDataForNetwork { get; }

        /// <summary>
        /// Gets or sets LatestDeliveryReportDate (last report pull time).
        /// </summary>
        DateTime LatestDeliveryReportDate { get; set; }

        /// <summary>
        /// Gets or sets LatestDeliveryDataDate (last reported hour bucket in the raw data).
        /// </summary>
        DateTime LatestDeliveryDataDate { get; set; }

        /// <summary>
        /// Gets or sets EarliestDeliveryDataDate (first reported hour bucket in the raw data).
        /// </summary>
        DateTime EarliestDeliveryDataDate { get; set; }

        /// <summary>
        /// Parse and merge additional raw delivery data into the canonical delivery data.
        /// </summary>
        /// <param name="rawDeliveryData">A raw delivery data string.</param>
        /// <param name="deliveryReportDate">Timestamp when report was retrieved.</param>
        /// <param name="parser">Raw delivery data parser.</param>
        /// <returns>True if raw data successfully added.</returns>
        bool AddRawData(string rawDeliveryData, DateTime deliveryReportDate, IRawDeliveryDataParser parser);

        /// <summary>Apply a lookback making sure it is not an invalid DateTime</summary>
        /// <param name="lookBackDuration">The look back duration.</param>
        /// <param name="lookBackStart">The lookback start.</param>
        /// <returns>The new date with lookback subtracted, or DateTime.MinValue.</returns>
        DateTime ApplyLookBack(TimeSpan lookBackDuration, DateTime lookBackStart);
    }
}