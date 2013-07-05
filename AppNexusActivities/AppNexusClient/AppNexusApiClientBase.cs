//-----------------------------------------------------------------------
// <copyright file="AppNexusApiClientBase.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using ConfigManager;
using DeliveryNetworkUtilities;
using Diagnostics;
using Utilities;
using Utilities.Net;

namespace AppNexusClient
{
    /// <summary>Base for the AppNexus API Client</summary>
    internal class AppNexusApiClientBase : DeliveryNetworkClientBase, IDeliveryNetworkClient
    {
        /// <summary>JavaScript (JSON) Serializer</summary>
        protected static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>Default page size for TryGetCollection</summary>
        private const int DefaultGetCollectionPageSize = 100;

        /// <summary>Backing field for RestClient. DO NOT USE DIRECTLY.</summary>
        private IAppNexusRestClient restClient;

        /// <summary>Gets or sets the rest client used to call the service apis</summary>
        internal IAppNexusRestClient RestClient
        {
            get
            {
                return this.restClient =
                    this.restClient ??
                    (this.IsAppNexusApp ?
                        (IAppNexusRestClient)new AppNexusAppRestClient(this.Config) :
                        (IAppNexusRestClient)new AppNexusRestClient(this.Config));
            }
            
            // for testing purposes.
            set
            {
                this.restClient = value;
            }
        }

        /// <summary>Gets a value indicating whether this is running as an AppNexus App</summary>
        protected bool IsAppNexusApp
        {
            get { return this.Config.GetBoolValue("AppNexus.IsApp"); }
        }

        /// <summary>Retrieve the specified report from AppNexus</summary>
        /// <param name="reportId">URI to retrieve the report</param>
        /// <returns>If available, the report CSV data; otherwise, null.</returns>
        public string RetrieveReport(string reportId)
        {
            // Check if the report is ready yet
            var response = this.RestClient.Get(Uris.RetrieveReportStatus, reportId);
            var responseValues = this.RestClient.TryGetResponseValues(response);
            if (responseValues == null || (string)responseValues[AppNexusValues.ExecutionStatus] != AppNexusValues.ExecutionStatusReady)
            {
                return null;
            }

            // Download and return the report
            return this.RestClient.Get(Uris.DownloadReport, reportId);
        }

        /// <summary>Escapes a string for use as a JSON string value</summary>
        /// <param name="content">String to escape</param>
        /// <returns>The escaped string</returns>
        internal static string JsonEscape(string content)
        {
            // TODO: Something less hacky?
            var dictionary = new Dictionary<string, object>
            {
                { "content", content }
            };

            var json = JsonSerializer.Serialize(dictionary);
            json = json.Substring(@"{""content"":""".Length);
            return json.Substring(0, json.Length - @"""}".Length);
        }

        /// <summary>Requests a report for the specified line item from AppNexus</summary>
        /// <param name="advertiserId">AppNexus advertiser id</param>
        /// <param name="reportRequestJson">AppNexus report request JSON</param>
        /// <returns>The AppNexus report id</returns>
        internal string RequestReport(int advertiserId, string reportRequestJson)
        {
            var response = this.RestClient.Post(reportRequestJson, Uris.RequestReport, advertiserId);
            var responseValues = this.RestClient.TryGetResponseValues(response);
            return (string)responseValues[AppNexusValues.ReportId];
        }

        /// <summary>POSTs an object to AppNexus and returns the ID</summary>
        /// <param name="objectJson">JSON for the object</param>
        /// <param name="postUri">URI to post the object to</param>
        /// <param name="args">Args for the URI format string</param>
        /// <returns>The new item's AppNexus id</returns>
        /// <exception cref="AppNexusClient.AppNexusClientException">
        /// An error occurred posting the object to the AppNexus service
        /// </exception>
        internal int CreateObject(string objectJson, string postUri, params object[] args)
        {
            try
            {
                var response = this.RestClient.Post(objectJson, postUri, args);
                var responseValues = this.RestClient.TryGetResponseValues(response);

                LogManager.Log(
                    LogLevels.Trace,
                    "POST {0}\nObject JSON:{1}\nResponse:\n{2}",
                    postUri.FormatInvariant(args),
                    objectJson,
                    response);

                if (responseValues == null || this.RestClient.IsErrorResponse(responseValues))
                {
                    throw new AppNexusClientException(
                        "Create object failed: POST {0}\nObject JSON:{1}"
                            .FormatInvariant(postUri.FormatInvariant(args), objectJson),
                        response,
                        responseValues);
                }

                return (int)responseValues[AppNexusValues.Id];
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nPOST {1}\nObject JSON:{2}\nResponse:\n{3}",
                    hrce,
                    hrce.Uri,
                    objectJson,
                    hrce.ResponseContent);
                throw new AppNexusClientException(hrce.Message, hrce);
            }
        }

        /// <summary>PUTs an object to AppNexus and returns the ID</summary>
        /// <param name="objectJson">JSON for the object</param>
        /// <param name="putUri">URI to put the object to</param>
        /// <param name="args">Args for the URI format string</param>
        /// <exception cref="AppNexusClient.AppNexusClientException">
        /// An error occurred posting the object to the AppNexus service
        /// </exception>
        internal void UpdateObject(string objectJson, string putUri, params object[] args)
        {
            try
            {
                var response = this.RestClient.Put(objectJson, putUri, args);
                var responseValues = this.RestClient.TryGetResponseValues(response);
                if (responseValues == null || this.RestClient.IsErrorResponse(responseValues))
                {
                    throw new AppNexusClientException(
                        "Update object failed: PUT {0}\nObject JSON:{1}"
                            .FormatInvariant(putUri.FormatInvariant(args), objectJson),
                        response,
                        responseValues);
                }

                LogManager.Log(
                    LogLevels.Trace,
                    "PUT {0}\nObject JSON:{1}\nResponse:\n{2}",
                    putUri.FormatInvariant(args),
                    objectJson,
                    string.Join("\n", responseValues.Select(kvp => "\t{0}: {1}".FormatInvariant(kvp.Key, kvp.Value)).ToArray()));
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nPUT {1}\nObject JSON:{2}\nResponse:\n{3}",
                    hrce,
                    hrce.Uri,
                    objectJson,
                    hrce.ResponseContent);
                throw new AppNexusClientException(hrce.Message, hrce);
            }
        }

        /// <summary>DELETEs an object</summary>
        /// <param name="deleteUri">URI of the object to delete</param>
        /// <param name="args">Format arguments</param>
        /// <returns>True if the object was deleted; otherwise, false</returns>
        internal bool DeleteObject(string deleteUri, params object[] args)
        {
            try
            {
                this.RestClient.Delete(deleteUri, args);
                return true;
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nDELETE {1}\nResponse:\n{2}",
                    hrce,
                    hrce.Uri,
                    hrce.ResponseContent);
                return false;
            }
        }

        /// <summary>GETs a collection of objects (possibly requiring multiple calls)</summary>
        /// <param name="collectionName">Name of the object in the response</param>
        /// <param name="getUri">URI to get the object from</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The values of the object if it exists; otherwise, null.</returns>
        internal IDictionary<string, object>[] TryGetCollection(
            string collectionName,
            string getUri,
            params object[] args)
        {
            return this.TryGetCollection(collectionName, null, DefaultGetCollectionPageSize, getUri, args);
        }

        /// <summary>GETs a collection of objects (possibly requiring multiple calls)</summary>
        /// <param name="collectionName">Name of the object in the response</param>
        /// <param name="filters">Filters to apply to objects in the collection or null for unfiltered</param>
        /// <param name="pageSize">Number of to fetch in each call to AppNexus</param>
        /// <param name="getUri">URI to get the object from</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The values of the object if it exists; otherwise, null.</returns>
        internal IDictionary<string, object>[] TryGetCollection(
            string collectionName,
            IDictionary<string, string> filters,
            int pageSize,
            string getUri,
            params object[] args)
        {
            try
            {
                var objects = new List<object>();
                var startElement = 0;
                var count = 0;
                do
                {
                    // Get the next page of objects
                    var pageObjects = this.TryGetCollection(startElement, pageSize, out count, collectionName, getUri, args);
                    if (pageObjects == null || pageObjects.Length == 0)
                    {
                        break;
                    }

                    // Add page objects to the result
                    if (filters == null)
                    {
                        objects.AddRange(pageObjects);
                    }
                    else
                    {
                        objects.AddRange(
                            pageObjects
                            .Where(o =>
                                filters.All(f =>
                                    o[f.Key] as string == f.Value)));
                    }

                    // set start element for next page
                    startElement += pageObjects.Length;
                }
                while (startElement < count);

                return objects.Cast<IDictionary<string, object>>().ToArray();
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nGET {1}\nResponse:\n{2}",
                    hrce,
                    hrce.Uri,
                    hrce.ResponseContent);
                throw new AppNexusClientException(hrce.Message, hrce);
            }
        }

        /// <summary>GETs a collection of objects (possibly requiring multiple calls)</summary>
        /// <param name="startElement">The index of the first element to get</param>
        /// <param name="maxElements">The maximum number of elements to get</param>
        /// <param name="count">The number of available elements</param>
        /// <param name="collectionName">Name of the object in the response</param>
        /// <param name="getUri">URI to get the object from</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The values of the object if it exists; otherwise, null.</returns>
        internal IDictionary<string, object>[] TryGetCollection(int startElement, int maxElements, out int count, string collectionName, string getUri, params object[] args)
        {
            try
            {
                // Add pagination values to query string
                var pagedGetUri = "{0}{1}start_element={2}&num_elements={3}".FormatInvariant(
                    getUri,
                    getUri.Contains("?") ? "&" : "?",
                    startElement,
                    maxElements);

                var response = this.RestClient.Get(pagedGetUri, args);
                var responseValues = this.RestClient.TryGetResponseValues(response);
                if (responseValues == null || this.RestClient.IsErrorResponse(responseValues))
                {
                    throw new AppNexusClientException(
                        "Error getting collection",
                        response,
                        responseValues);
                }

                count = (int)responseValues["count"];
                maxElements = (int)responseValues["num_elements"];
                startElement = (int)responseValues["start_element"];

                LogManager.Log(
                    LogLevels.Trace,
                    "GET {0}\nElements {1}-{2} of {3}\nResponse:\n{4}",
                    getUri.FormatInvariant(args),
                    startElement,
                    startElement + maxElements,
                    count,
                    string.Join("\n", responseValues.Select(kvp => "\t{0}: {1}".FormatInvariant(kvp.Key, kvp.Value)).ToArray()));

                var pageObjects = responseValues[collectionName] as object[];
                return pageObjects != null ?
                    pageObjects.Cast<IDictionary<string, object>>().ToArray() :
                    new IDictionary<string, object>[0];
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nGET {1}\nResponse:\n{2}",
                    hrce,
                    hrce.Uri,
                    hrce.ResponseContent);
                throw new AppNexusClientException(hrce.Message, hrce);
            }
        }

        /// <summary>GETs a collection of objects (not requiring multiple calls)</summary>
        /// <param name="collectionName">Name of the object in the response</param>
        /// <param name="getUri">URI to get the object from</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The values of the object if it exists; otherwise, null.</returns>
        internal IDictionary<string, object>[] TryGetUnpagedCollection(string collectionName, string getUri, params object[] args)
        {
            try
            {
                var response = this.RestClient.Get(getUri, args);
                var responseValues = this.RestClient.TryGetResponseValues(response);
                if (responseValues == null || this.RestClient.IsErrorResponse(responseValues))
                {
                    // TODO: Check more thoroughly that the error was actually due to it not existing
                    return null;
                }

                LogManager.Log(
                    LogLevels.Trace,
                    "GET {0}\nResponse:\n{1}",
                    getUri.FormatInvariant(args),
                    string.Join("\n", responseValues.Select(kvp => "\t{0}: {1}".FormatInvariant(kvp.Key, kvp.Value)).ToArray()));

                var collection = responseValues[collectionName] as object[];
                return collection != null ?
                    collection.Cast<IDictionary<string, object>>().ToArray() :
                    new IDictionary<string, object>[0];
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nGET {1}\nResponse:\n{2}",
                    hrce,
                    hrce.Uri,
                    hrce.ResponseContent);
                throw new AppNexusClientException(hrce.Message, hrce);
            }
        }

        /// <summary>GETs an object and returns its values</summary>
        /// <param name="objectName">Name of the object in the response</param>
        /// <param name="getUri">URI to get the object from</param>
        /// <param name="args">Format arguments</param>
        /// <returns>The values of the object if it exists; otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "TODO: Be more specific")]
        internal IDictionary<string, object> TryGetObject(string objectName, string getUri, params object[] args)
        {
            try
            {
                var response = this.RestClient.Get(getUri, args);
                var responseValues = this.RestClient.TryGetResponseValues(response);
                if (responseValues == null || this.RestClient.IsErrorResponse(responseValues))
                {
                    // TODO: Check more thoroughly that the error was actually due to it not existing
                    return null;
                }

                LogManager.Log(
                    LogLevels.Trace,
                    "GET {0}\nResponse:\n{1}",
                    getUri.FormatInvariant(args),
                    string.Join("\n", responseValues.Select(kvp => "\t{0}: {1}".FormatInvariant(kvp.Key, kvp.Value)).ToArray()));

                return responseValues[objectName] as IDictionary<string, object>;
            }
            catch (HttpRestClientException hrce)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "HttpRestClientException: {0}\nGET {1}\nResponse:\n{2}",
                    hrce,
                    hrce.Uri,
                    hrce.ResponseContent);
                throw new AppNexusClientException(hrce.Message, hrce);
            }
        }

        /// <summary>
        /// Gets the time stamp for the API.
        /// </summary>
        /// <param name="dateTime">The date/time (UTC)</param>
        /// <returns>The time stamp</returns>
        protected static string GetApiTimeStamp(DateTime dateTime)
        {
            return dateTime
                .ToString(AppNexusJson.TimeStampFormat, CultureInfo.InvariantCulture);
        }

        /// <summary>Cleans up unmanaged and unmanaged resources</summary>
        /// <param name="disposing">
        /// Whether to clean up managed resources as well as unmanaged
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.restClient != null)
                {
                    this.restClient.Dispose();
                    this.restClient = null;
                }
            }
        }

        /// <summary>URIs for interracting with the AppNexus service</summary>
        private static class Uris
        {
            /// <summary>
            /// Request report URI format
            /// 0: advertiser id
            /// </summary>
            public const string RequestReport = "report?advertiser_id={0}";

            /// <summary>
            /// Retrieve report status URI format
            /// 0: report id
            /// </summary>
            public const string RetrieveReportStatus = "report?id={0}&without_data";

            /// <summary>
            /// Download report URI format
            /// 0: report id
            /// </summary>
            public const string DownloadReport = "report-download?id={0}";
        }
    }
}
