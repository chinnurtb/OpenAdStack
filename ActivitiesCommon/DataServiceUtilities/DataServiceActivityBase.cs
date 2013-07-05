//-----------------------------------------------------------------------
// <copyright file="DataServiceActivityBase.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Activities;
using Diagnostics;
using Newtonsoft.Json;

namespace DataServiceUtilities
{
    /// <summary>
    /// Base class for activities that provide data to the data service
    /// </summary>
    /// <remarks>
    /// Optional Values:
    ///   Mode - The mode used to retrieve results (default is Paged)
    ///   ResultFormat - The format of the results (default is Json)
    ///   Offset - The offset from which to start returning paged results
    ///   MaxResults - The maximum number of results to return in a page
    ///   SubtreeBaseId - Id of the node at the root of the result subtree
    ///   Depth - The maximum depth of the result subtree
    /// Result Values:
    ///   Data - The data requested
    /// </remarks>
    /// <typeparam name="TResult">Type of the results</typeparam>
    [ResultValues(
        DataServiceActivityValues.Results,
        DataServiceActivityValues.ResultsFormat,
        DataServiceActivityValues.Total)]
    public abstract class DataServiceActivityBase<TResult> : Activity
    {
        /// <summary>Key for results that are still loading</summary>
        private const string LoadingResultKey = "<loading>";

        /// <summary>
        /// Gets the runtime category of the activity
        /// </summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.InteractiveFetch; }
        }

        /// <summary>
        /// Gets the default results format to use if none specified
        /// </summary>
        protected virtual DataServiceResultsFormat DefaultFormat
        {
            get { return DataServiceResultsFormat.Json; }
        }

        /// <summary>
        /// Gets the default mode to use if none specified
        /// </summary>
        protected virtual DataServiceMode DefaultMode
        {
            get { return DataServiceMode.All; }
        }

        /// <summary>
        /// Gets the result path separator
        /// </summary>
        protected virtual char ResultPathSeparator
        {
            get { return (char)0; }
        }

        /// <summary>
        /// Gets a string that represents a result's path in the hierarchy
        /// </summary>
        /// <param name="result">The result</param>
        /// <returns>The result's path</returns>
        protected abstract string GetResultPath(TResult result);

        /// <summary>
        /// Gets whether result represents a loaded result or not
        /// </summary>
        /// <param name="result">The result</param>
        /// <returns>Whether the result is loaded</returns>
        protected abstract bool IsResultLoaded(TResult result);

        /// <summary>Gets results based on the request values</summary>
        /// <remarks>
        /// The resultSet set for the activity resultSet will be selected
        /// from within these results.
        /// </remarks>
        /// <param name="requestValues">Request values</param>
        /// <returns>The full set of results</returns>
        protected abstract TResult[] GetResults(
            IDictionary<string, string> requestValues);

        /// <summary>Formats the results as JSON</summary>
        /// <param name="results">Results to be formatted</param>
        /// <returns>The JSON formatted results</returns>
        protected virtual string FormatResultsAsJson(
            IDictionary<string, object> results)
        {
            return JsonConvert.SerializeObject(results.Values.Cast<TResult>().ToArray());
        }

        /// <summary>Formats the results as XML</summary>
        /// <remarks>Not supported by base class</remarks>
        /// <param name="results">Results to be formatted</param>
        /// <param name="subtreePath">Path of the root of the results</param>
        /// <returns>The XML formatted results</returns>
        protected virtual string FormatResultsAsXml(
            IDictionary<string, object> results,
            string subtreePath)
        {
            throw new NotImplementedException();
        }

        /// <summary>Determine whether a result should be filtered</summary>
        /// <param name="result">Result to be filtered</param>
        /// <param name="requestValues">Request values</param>
        /// <returns>True if the result should be filtered out; otherwise, false</returns>
        protected virtual bool FilterResult(
            TResult result,
            IDictionary<string, string> requestValues)
        {
            // Default behavior is to not filter results
            return false;
        }

        /// <summary>Processes the data service activity request</summary>
        /// <param name="request">
        /// Request containing resultSet range values as well as any additional
        /// values required by the derived class to retrieve the data.
        /// </param>
        /// <returns>Activity resultSet containing the requested data.</returns>
        protected sealed override ActivityResult ProcessRequest(ActivityRequest request)
        {
            // Get the resultSet set range/format values from the request
            DataServiceResultsFormat format;
            DataServiceMode mode;

            // Get the result format
            if (!request.TryGetEnumValue<DataServiceResultsFormat>(
                DataServiceActivityValues.ResultsFormat,
                out format))
            {
                format = this.DefaultFormat;
            }

            // Get the result filter mode
            if (!request.TryGetEnumValue<DataServiceMode>(
                DataServiceActivityValues.Mode,
                out mode))
            {
                mode = this.DefaultMode;
            }

            // Get the result subtree path (optional)
            string resultPath;
            request.TryGetValue(DataServiceActivityValues.SubtreePath, out resultPath);
            resultPath = resultPath ?? string.Empty;

            // Get the results from the derived class as a dictionary
            // Also applies a customizable filter based on request values
            var filteredResults = this.ResultsToDictionary(
                this.GetResults(request.Values)
                .Where(result =>
                    !this.FilterResult(result, request.Values)));

            // Filter the results by page or subtree
            IDictionary<string, object> resultSet;
            ActivityResult error;
            if (mode == DataServiceMode.All)
            {
                resultSet = filteredResults
                    .Where(result =>
                        this.IsResultLoaded(result.Value))
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => (object)kvp.Value);
            }
            else if (mode == DataServiceMode.Paged)
            {
                var maxResults = this.TryGetRequiredIntegerRequestValue(
                    request,
                    DataServiceActivityValues.MaxResults,
                    mode,
                    out error);
                if (maxResults < 0)
                {
                    return error;
                }

                var offset = this.TryGetRequiredIntegerRequestValue(
                    request,
                    DataServiceActivityValues.Offset,
                    mode,
                    out error);
                if (offset < 0)
                {
                    return error;
                }

                resultSet = this.GetResultPage(filteredResults, offset, maxResults);
            }
            else
            {
                var depth = this.TryGetRequiredIntegerRequestValue(
                    request,
                    DataServiceActivityValues.Depth,
                    mode,
                    out error);
                if (depth < 0)
                {
                    return error;
                }

                resultSet = this.GetResultSubtree(filteredResults, resultPath, depth);
            }

            // Format and return the results
            var formattedResults =
                format == DataServiceResultsFormat.Json ?
                    this.FormatResultsAsJson(resultSet) :
                format == DataServiceResultsFormat.Xml ?
                    this.FormatResultsAsXml(resultSet, resultPath) :
                    string.Empty;

            return SuccessResult(
                new Dictionary<string, string>
                {
                    { DataServiceActivityValues.Results, formattedResults ?? string.Empty },
                    { DataServiceActivityValues.ResultsFormat, format.ToString() },
                    { DataServiceActivityValues.Total, filteredResults.Count.ToString(CultureInfo.InvariantCulture) },
                });
        }

        /// <summary>Gets a page of results</summary>
        /// <param name="results">Results from which to return a page</param>
        /// <param name="offset">Offset to start of the paged results</param>
        /// <param name="maxResults">Maximum number of results in the page</param>
        /// <returns>A page of results</returns>
        protected IDictionary<string, object> GetResultPage(
            IDictionary<string, TResult> results,
            int offset,
            int maxResults)
        {
            var sourceResults =
                results.Values
                .Where(result =>
                    this.IsResultLoaded(result))
                .ToArray();

            var resultPageLength =
                (offset + maxResults < sourceResults.Length) ?
                    Math.Min(maxResults, sourceResults.Length) :
                    sourceResults.Length - offset;

            TResult[] resultPage;
            if (resultPageLength > 0)
            {
                resultPage = new TResult[resultPageLength];
                Array.Copy(sourceResults, offset, resultPage, 0, resultPageLength);
            }
            else
            {
                resultPage = new TResult[0];
            }

            return resultPage
                .ToDictionary(
                    result => this.GetResultPath(result),
                    result => (object)result);
        }

        /// <summary>Gets a subtree of results</summary>
        /// <param name="results">Results from which to return a subtree</param>
        /// <param name="subtreePath">The base id of results to include in the subtree</param>
        /// <param name="depth">The maximum depth of the subtree</param>
        /// <returns>A subtree of results</returns>
        protected IDictionary<string, object> GetResultSubtree(
            IDictionary<string, TResult> results,
            string subtreePath,
            int depth)
        {
            // Select a sorted subset of the results with paths starting at the subtree path
            var subtreeBaseDepth = subtreePath
                .Count(c =>
                    c == this.ResultPathSeparator);
            var subtreeMaxDepth = subtreeBaseDepth + depth;
            var resultPrefix =
                !string.IsNullOrWhiteSpace(subtreePath) ?
                subtreePath + this.ResultPathSeparator :
                string.Empty;
            var sortedResultSet = results
                .Where(kvp =>
                    kvp.Key.StartsWith(resultPrefix, StringComparison.Ordinal))
                .Where(kvp =>
                    {
                        // Perform a more detailed check to prevent false positives
                        var resultPath = kvp.Key.Substring(0, subtreePath.Length);
                        var resultDepth = resultPath.Count(c =>
                            c == this.ResultPathSeparator);
                        return resultDepth <= subtreeMaxDepth;
                    })
                .ToDictionary();

            // Build an object graph from the sorted result subset
            var currentPath = subtreePath;
            var subtree = new Dictionary<string, object>();
            var stack = new Stack<IDictionary<string, object>>();
            var currentContainer = subtree as IDictionary<string, object>;
            foreach (var result in sortedResultSet)
            {
                // Get the result path. If the result path matches the current path
                // then it is a leaf with the same name as a branch. Leaving the name
                // alone creates a leaf under the branch with the same name.
                var resultPath =
                    result.Key != currentPath ?
                    result.Key.Left(Math.Max(0, result.Key.LastIndexOf(this.ResultPathSeparator))) :
                    result.Key;
                
                // Check if the path has changed
                if (resultPath != currentPath)
                {
                    // Back up the graph stack until a common path is found
                    while (!resultPath.StartsWith(currentPath, StringComparison.Ordinal))
                    {
                        // Make sure it is actually a match
                        currentContainer = stack.Pop();
                        currentPath = currentPath.Left(Math.Max(0, currentPath.LastIndexOf(this.ResultPathSeparator)));
                    }

                    // Build down the graph from the current path to the new path
                    var containerNames = resultPath
                        .Substring(currentPath.Length)
                        .Split(new[] { this.ResultPathSeparator }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToArray();

                    foreach (var containerName in containerNames)
                    {
                        if (stack.Count >= depth)
                        {
                            break;
                        }
                        
                        stack.Push(currentContainer);
                        currentPath = string.Join(
                            this.ResultPathSeparator.ToString(),
                            new[] { currentPath, containerName }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));

                        if (!currentContainer.ContainsKey(currentPath))
                        {
                            // Add a new container
                            currentContainer.Add(currentPath, new Dictionary<string, object>());
                        }
                        else if (currentContainer[currentPath] is TResult)
                        {
                            // A result already exists with the same name as the new container
                            // Create the container, move the result into it and then replace
                            var container = new Dictionary<string, object>();

                            if (stack.Count < depth)
                            {
                                container.Add(currentPath, currentContainer[currentPath]);
                            }

                            currentContainer[currentPath] = container;
                        }
                        else
                        {
                            LogManager.Log(
                                LogLevels.Warning,
                                "Duplicate container in subtree {0}: '{1}'",
                                subtreePath,
                                currentPath);
                        }

                        currentContainer = (IDictionary<string, object>)currentContainer[currentPath];
                    }
                }

                if (stack.Count < depth)
                {
                    if (this.IsResultLoaded(result.Value))
                    {
                        // Add the result to the container
                        currentContainer.Add(result.Key, result.Value);
                    }
                    else
                    {
                        // Add a placeholder to indicate these results are still loading
                        currentContainer.Add(LoadingResultKey, null);
                    }
                }
            }

            return subtree;
        }

        /// <summary>Gets a required integer request value</summary>
        /// <param name="request">The activity request</param>
        /// <param name="valueName">The value name</param>
        /// <param name="mode">The data service request mode</param>
        /// <param name="error">If successful, null; otherwise, the error.</param>
        /// <returns>If successful, the value; otherwise, -1.</returns>
        private int TryGetRequiredIntegerRequestValue(
            ActivityRequest request,
            string valueName,
            DataServiceMode mode,
            out ActivityResult error)
        {
            if (!request.Values.ContainsKey(valueName))
            {
                error = this.ErrorResult(
                    ActivityErrorId.GenericError,
                    "Request value {0} missing (required for {1} mode)",
                    valueName,
                    mode);
                return -1;
            }

            int value;
            if (!request.TryGetIntegerValue(valueName, out value))
            {
                error = this.ErrorResult(
                    ActivityErrorId.GenericError,
                    "Request value {0} is not a valid integer: '{1}'",
                    valueName,
                    request.Values[valueName]);
                return -1;
            }

            error = null;
            return value;
        }

        /// <summary>
        /// Gets a sorted dictionary of unique results keyed by their paths.
        /// </summary>
        /// <remarks>
        /// Results with duplicate keys are logged in a warning message
        /// and excluded from the dictionary.
        /// </remarks>
        /// <param name="results">The results</param>
        /// <returns>Dictionary of sorted, unique results</returns>
        private IDictionary<string, TResult> ResultsToDictionary(
            IEnumerable<TResult> results)
        {
            var keyedResults = results
                .Select(result =>
                    new KeyValuePair<string, TResult>(
                        this.GetResultPath(result),
                        result))
                .OrderBy(kvp => kvp.Key);

            var uniqueResults = keyedResults
                .Distinct(kvp => kvp.Key);

            /*
            var duplicateResultKeys = keyedResults
                .Except(uniqueResults)
                .Select(duplicate => duplicate.Key)
                .ToArray();
            if (duplicateResultKeys.Length > 0)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Excluding {0} results with duplicate paths",
                    duplicateResultKeys.Length);
            }
             */

            return uniqueResults.ToDictionary();
        }
    }
}
