// -----------------------------------------------------------------------
// <copyright file="RawDeliveryDataItem.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Class to contain a single raw delivery data payload
    /// </summary>
    public class RawDeliveryDataItem
    {
        /// <summary>Initializes a new instance of the <see cref="RawDeliveryDataItem"/> class.</summary>
        /// <param name="rawDeliveryData">The serialized raw delivery data.</param>
        /// <param name="deliveryReportDate">The time the data delivery data was saved.</param>
        public RawDeliveryDataItem(string rawDeliveryData, DateTime deliveryReportDate)
        {
            this.RawDeliveryData = rawDeliveryData;
            this.DeliveryDataReportDate = deliveryReportDate;
        }

        /// <summary>Gets the serialized raw delivery data.</summary>
        public string RawDeliveryData { get; private set; }

        /// <summary>Gets the time the data delivery data was saved.</summary>
        public DateTime DeliveryDataReportDate { get; private set; }
    }
}