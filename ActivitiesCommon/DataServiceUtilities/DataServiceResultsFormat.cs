//-----------------------------------------------------------------------
// <copyright file="DataServiceResultsFormat.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DataServiceUtilities
{
    /// <summary>
    /// Requestable resultSet formats for data service activities
    /// </summary>
    /// <remarks>
    /// The schema of the data within the results varies between
    /// different data services as appropriate to their consumers.
    /// </remarks>
    public enum DataServiceResultsFormat
    {
        /// <summary>
        /// JSON formatted list
        /// </summary>
        Json,

        /// <summary>
        /// XML representation of the results
        /// </summary>
        Xml,
    }
}
