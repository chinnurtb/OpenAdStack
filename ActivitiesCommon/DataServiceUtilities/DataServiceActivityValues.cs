//-----------------------------------------------------------------------
// <copyright file="DataServiceActivityValues.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DataServiceUtilities
{
    /// <summary>
    /// ActivityRequest/ActivityResult value keys for data service activities
    /// </summary>
    public static class DataServiceActivityValues
    {
        /// <summary>
        /// Mode used in formating the output (paged or tree)
        /// </summary>
        public const string Mode = "Mode";

        /// <summary>
        /// Offset from which paged results start.
        /// </summary>
        /// <remarks>
        /// If the offset is greater than the number of results then
        /// an empty resultSet set will be returned.
        /// </remarks>
        public const string Offset = "Offset";

        /// <summary>
        /// Maximum number of results to return in a single page.
        /// </summary>
        public const string MaxResults = "MaxResults";

        /// <summary>
        /// Path of the results subtree to return.
        /// </summary>
        public const string SubtreePath = "SubtreePath";

        /// <summary>
        /// Depth of the children to return in tree results.
        /// </summary>
        public const string Depth = "Depth";

        /// <summary>
        /// Keys to be returned exclusively
        /// </summary>
        /// <remarks>
        /// Filtering functionality is not included in the DataServiceActivityBase
        /// and must be implemented by the derived data service activities.
        /// </remarks>
        public const string Ids = "Ids";

        /// <summary>
        /// Criteria for results to be included.
        /// </summary>
        /// <remarks>
        /// Filtering functionality is not included in the DataServiceActivityBase
        /// and must be implemented by the derived data service activities.
        /// </remarks>
        public const string Include = "Include";

        /// <summary>
        /// Criteria for results to be excluded.
        /// </summary>
        /// <remarks>
        /// Filtering functionality is not included in the DataServiceActivityBase
        /// and must be implemented by the derived data service activities.
        /// </remarks>
        public const string Exclude = "Exclude";

        /// <summary>
        /// Count of the results returned in the resultSet set.
        /// </summary>
        public const string Count = "Count";

        /// <summary>
        /// Count of the total results available.
        /// </summary>
        public const string Total = "Total";

        /// <summary>
        /// The result format.
        /// </summary>
        /// <remarks>
        /// Either JSON or XML. Supported formats vary between different
        /// data service activities.
        /// </remarks>
        /// <seealso cref="DataServiceResultsFormat"/>
        public const string ResultsFormat = "ResultsFormat";

        /// <summary>
        /// The resultSet set in the format requested.
        /// </summary>
        public const string Results = "Results";
    }
}
