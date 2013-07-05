// -----------------------------------------------------------------------
// <copyright file="NodeHourMetrics.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Linq;

namespace DynamicAllocation
{
    /// <summary>
    /// Node delivery metrics for a single hour in a seven day delivery profile
    /// </summary>
    public class NodeHourMetrics
    {
        /// <summary>
        /// Maximum number of elements included in LastN members
        /// </summary>
        public const int LastNMax = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeHourMetrics"/> class.
        /// </summary>
        public NodeHourMetrics()
        {
            this.AverageImpressions = 0;
            this.AverageMediaSpend = 0;
            this.EligibilityCount = 0;
            this.LastNImpressions = new List<long>();
            this.LastNMediaSpend = new List<decimal>();
            this.LastAddedHour = DateTime.MinValue;
        }

        /// <summary>
        /// Gets or sets LastAddedHour.
        /// </summary>
        public DateTime LastAddedHour { get; set; }

        /// <summary>
        /// Gets or sets AverageImpressions.
        /// </summary>
        public decimal AverageImpressions { get; set; }

        /// <summary>
        /// Gets or sets AverageMediaSpend.
        /// </summary>
        public decimal AverageMediaSpend { get; set; }

        /// <summary>
        /// Gets or sets EligibilityCount.
        /// </summary>
        public int EligibilityCount { get; set; }

        /// <summary>
        /// Gets LastNImpressions.
        /// </summary>
        public IList<long> LastNImpressions { get; private set; }

        /// <summary>
        /// Gets LastNMediaSpend.
        /// </summary>
        public IList<decimal> LastNMediaSpend { get; private set; }

        /// <summary>Add an impression value to a fifo with a limit of LastNMax.</summary>
        /// <param name="valueToAdd">The value to add.</param>
        public void AddToLastNImpressions(long valueToAdd)
        {
            this.LastNImpressions = AddToLastNHistory(this.LastNImpressions, valueToAdd);
        }

        /// <summary>Add a media spend value to a fifo with a limit of LastNMax.</summary>
        /// <param name="valueToAdd">The value to add.</param>
        public void AddToLastNMediaSpend(decimal valueToAdd)
        {
            this.LastNMediaSpend = AddToLastNHistory(this.LastNMediaSpend, valueToAdd);
        }

        /// <summary>Add a value to a fifo with a limit of LastNMax.</summary>
        /// <param name="oldList">The old list.</param>
        /// <param name="valueToAdd">The value to add.</param>
        /// <typeparam name="T">The type of the value to add.</typeparam>
        /// <returns>The updated list.</returns>
        private static IList<T> AddToLastNHistory<T>(IList<T> oldList, T valueToAdd)
        {
            oldList.Insert(0, valueToAdd);
            return oldList.Take(LastNMax).ToList();
        }
    }
}