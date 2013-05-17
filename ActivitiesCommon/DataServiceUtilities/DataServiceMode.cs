//-----------------------------------------------------------------------
// <copyright file="DataServiceMode.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DataServiceUtilities
{
    /// <summary>Request mode for data service activities</summary>
    public enum DataServiceMode
    {
        /// <summary>All results are returned.</summary>
        All,

        /// <summary>Results are returned in pages.</summary>
        /// <remarks>Uses the Offset and MaxResults request values.</remarks>
        Paged,

        /// <summary>Results are returned as tree nodes.</summary>
        /// <remarks>
        /// ResultPathSeparator and GetResultPath(TResult) must be
        /// overridden in order to get subtree results.
        /// Uses SubtreePath and Depth request values.
        /// </remarks>
        Tree,
    }
}
