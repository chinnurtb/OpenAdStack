// -----------------------------------------------------------------------
// <copyright file="RawDeliveryDataItem.cs" company="Rare Crowds Inc">
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