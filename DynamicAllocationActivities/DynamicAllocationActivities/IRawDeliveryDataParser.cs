// -----------------------------------------------------------------------
// <copyright file="IRawDeliveryDataParser.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
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