// -----------------------------------------------------------------------
// <copyright file="DeliveryDataCoster.cs" company="Rare Crowds Inc">
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