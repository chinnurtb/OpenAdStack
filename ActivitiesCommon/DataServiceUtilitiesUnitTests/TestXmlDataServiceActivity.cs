//-----------------------------------------------------------------------
// <copyright file="TestXmlDataServiceActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Activities;
using DataServiceUtilities;

namespace DataServiceUtilitiesUnitTests
{
    /// <summary>DataServiceActivityBase derived class for testing</summary>
    [Name("TestDataServiceActivity")]
    internal class TestXmlDataServiceActivity : DataServiceActivityBase<int[]>
    {
        /// <summary>Gets the last set of results returned</summary>
        public int[][] Results { get; private set; }

        /// <summary>Gets or sets the breadth of the results to return</summary>
        public int Breadth { get; set; }

        /// <summary>Gets or sets the depth of the results to return</summary>
        public int Depth { get; set; }

        /// <summary>Gets the results path separator</summary>
        protected override char ResultPathSeparator
        {
            get { return ':'; }
        }

        /// <summary>Gets the path for a result</summary>
        /// <param name="result">The result</param>
        /// <returns>The result's path</returns>
        protected override string GetResultPath(int[] result)
        {
            return string.Join(this.ResultPathSeparator.ToString(), result);
        }

        /// <summary>Gets whether the result is loaded</summary>
        /// <param name="result">The result</param>
        /// <returns>Whether the result is loaded</returns>
        protected override bool IsResultLoaded(int[] result)
        {
            return !result.Any(i => i < 0);
        }

        /// <summary>Gets results based upon the request values</summary>
        /// <param name="requestValues">The request values</param>
        /// <returns>The results</returns>
        protected override int[][] GetResults(IDictionary<string, string> requestValues)
        {
            var count = (int)Math.Pow(this.Breadth, this.Depth);
            return this.Results =
                Enumerable.Range(0, count)
                .Select(i =>
                    Enumerable.Range(0, this.Depth)
                        .Select(x => (int)Math.Pow(this.Breadth, x))
                        .Select(x => (i / x) % this.Breadth)
                        .ToArray())
                .Concat(new[]
                {
                    Enumerable.Range(100, this.Depth - 1).Concat(new[] { -1 }).ToArray(),
                    Enumerable.Range(200, this.Depth - 1).Concat(new[] { -1 }).ToArray(),
                    Enumerable.Range(300, this.Depth - 1).Concat(new[] { -1 }).ToArray(),
                })
                .ToArray();
        }

        /// <summary>Formats the results as XML</summary>
        /// <param name="results">Set of results to format</param>
        /// <param name="subtreePath">Path of the root of the results</param>
        /// <returns>The XML formatted results</returns>
        protected override string FormatResultsAsXml(
            IDictionary<string, object> results,
            string subtreePath)
        {
            return DataServiceActivityUtilities.FormatResultsAsDhtmlxTreeGridXml<int[]>(
                results,
                subtreePath,
                r => r.Sum().ToString(CultureInfo.InvariantCulture),
                this.ResultPathSeparator);
        }
    }
}