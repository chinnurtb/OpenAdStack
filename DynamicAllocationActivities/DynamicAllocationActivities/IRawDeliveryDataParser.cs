// -----------------------------------------------------------------------
// <copyright file="IRawDeliveryDataParser.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataAccessLayer;

namespace DynamicAllocationActivities
{
    /// <summary>Interface for objects that convert raw delivery data from various sources to a canonical form.</summary>
    public interface IRawDeliveryDataParser
    {
        /// <summary>Parse raw delivery data records. Tolerant of multiple header records from concatonated reports.</summary>
        /// <param name="records">An array of records (one record per array element).</param>
        /// <returns>A canonical representation of the delivery data - one dictionary of name/value pairs per record.</returns>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "It's a collection of csv rows.")]
        IList<Dictionary<string, PropertyValue>> ParseRawRecords(string[] records);

        /// <summary>Split individual records of a raw delivery data file into a list of one string per record.</summary>
        /// <param name="rawDeliveryData">The raw delivery data string.</param>
        /// <returns>A list of strings - one per record</returns>
        IList<string> SplitRecords(string rawDeliveryData);
    }
}