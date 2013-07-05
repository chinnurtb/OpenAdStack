//-----------------------------------------------------------------------
// <copyright file="DataServiceMode.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

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
