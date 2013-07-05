// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpExtensions.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestUtilities
{
    /// <summary>Http testing extensions</summary>
    public static class HttpExtensions
    {
        /// <summary>JSON Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>Assert the status code matches one of the expected values</summary>
        /// <param name="response">The response</param>
        /// <param name="expectedStatusCodes">The expected status codes</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertStatusCode(this HttpResponseMessage response, params HttpStatusCode[] expectedStatusCodes)
        {
            if (!expectedStatusCodes.Contains(response.StatusCode))
            {
                Fail(
                    response,
                    "The status code <{0}> was not one of the expected status codes <{1}>.",
                    response.StatusCode,
                    string.Join(", ", expectedStatusCodes));
            }

            return response;
        }

        /// <summary>Assert the status code matches the expected value</summary>
        /// <param name="response">The response</param>
        /// <param name="expectedContentPattern">The regex pattern the response content is expected to match</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertContentMatches(this HttpResponseMessage response, string expectedContentPattern)
        {
            var regex = new Regex(expectedContentPattern);
            var content = response.Content.ReadAsString();
            if (!regex.IsMatch(content))
            {
                Fail(
                    response,
                    "Response content did not match the expected pattern: \"{0}\".",
                    expectedContentPattern);
            }

            return response;
        }

        /// <summary>Assert the response content is valid JSON</summary>
        /// <param name="response">The response</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertContentIsJson(this HttpResponseMessage response)
        {
            if (response.TryDeserializeContentJson() == null)
            {
                Fail(response, "The response content is not valid JSON.");
            }

            return response;
        }

        /// <summary>Assert the response content is not valid JSON</summary>
        /// <param name="response">The response</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertContentIsNotJson(this HttpResponseMessage response)
        {
            if (response.TryDeserializeContentJson() != null)
            {
                Fail(response, "The response content is valid JSON (was expecting non-JSON response).");
            }

            return response;
        }

        /// <summary>Asserts the response content is valid JSON and contains the specified value</summary>
        /// <param name="response">The response</param>
        /// <param name="expectedValueName">The expected value name</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertContainsJsonValue(this HttpResponseMessage response, string expectedValueName)
        {
            if (string.IsNullOrWhiteSpace(expectedValueName))
            {
                throw new ArgumentNullException("expectedValueName");
            }

            response.AssertContentIsJson();

            var graph = response.TryDeserializeContentJson();
            if (GetValuesForKey(graph, expectedValueName).Count() == 0)
            {
                var content = response.Content.ReadAsString();
                Fail(
                    response,
                    "The response did not contain any values for '{0}'.",
                    expectedValueName,
                    content);
            }

            return response;
        }

        /// <summary>Asserts the response content is valid JSON and contains the specified value</summary>
        /// <typeparam name="TValue">Type of the expected value</typeparam>
        /// <param name="response">The response</param>
        /// <param name="expectedValueName">The expected value name</param>
        /// <param name="expectedValue">The expected value</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertContainsJsonValue<TValue>(this HttpResponseMessage response, string expectedValueName, TValue expectedValue)
        {
            if (string.IsNullOrWhiteSpace(expectedValueName))
            {
                throw new ArgumentNullException("expectedValueName");
            }

            if (expectedValue == null)
            {
                throw new ArgumentNullException("expectedValue");
            }

            response
                .AssertContentIsJson()
                .AssertContainsJsonValue(expectedValueName);

            var graph = response.TryDeserializeContentJson();
            var values = GetValuesForKey(graph, expectedValueName);
            if (!values.Contains(expectedValue))
            {
                Fail(
                    response,
                    "The response did not contain the expected value for '{0}' (expected: '{1}' actual: [{2}])",
                    expectedValueName,
                    expectedValue,
                    string.Join(",", values));
            }

            return response;
        }

        /// <summary>Assert the response contains a header with the expected name</summary>
        /// <param name="response">The response</param>
        /// <param name="expectedHeaderName">The expected header name</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertHasHeader(this HttpResponseMessage response, string expectedHeaderName)
        {
            if (!response.Headers.ContainsKey(expectedHeaderName))
            {
                Fail(
                    response,
                    "The response did not contain the expected header: '{0}'.",
                    expectedHeaderName);
            }

            return response;
        }

        /// <summary>Assert the response contains a header with the expected name and value</summary>
        /// <param name="response">The response</param>
        /// <param name="expectedHeaderName">The expected header name</param>
        /// <param name="expectedValue">The expected header value</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertHeaderEquals(this HttpResponseMessage response, string expectedHeaderName, string expectedValue)
        {
            response.AssertHasHeader(expectedHeaderName);
            if (expectedValue != response.Headers[expectedHeaderName])
            {
                Fail(
                    response,
                    "The response did not contain the expected header value for '{0}'. (expected: '{1}' actual: '{2}')",
                    expectedHeaderName,
                    expectedValue,
                    response.Headers[expectedHeaderName]);
            }

            return response;
        }

        /// <summary>Assert the response contains a header with the expected name with a value matching the expected regex pattern</summary>
        /// <param name="response">The response</param>
        /// <param name="expectedHeaderName">The expected header name</param>
        /// <param name="expectedValuePattern">The expected header value regex pattern</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertHeaderMatches(this HttpResponseMessage response, string expectedHeaderName, string expectedValuePattern)
        {
            response.AssertHasHeader(expectedHeaderName);
            var regex = new Regex(expectedValuePattern);
            var headerValue = response.Headers[expectedHeaderName];
            if (!regex.IsMatch(headerValue))
            {
                Fail(
                    response,
                    "The response header '{0}' did not match the expected pattern: \"{1}\".",
                    expectedHeaderName,
                    expectedValuePattern);
            }

            return response;
        }

        /// <summary>Asserts the response is a redirect</summary>
        /// <param name="response">The response</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage AssertIsValidRedirect(this HttpResponseMessage response)
        {
            if (!response.IsRedirect())
            {
                Fail(
                    response,
                    "The response status code was not a redirect (expected: '3xx' actual: '{0}')",
                    (int)response.StatusCode);
            }

            response.AssertHasHeader("Location");
            return response;
        }

        /// <summary>Attempts to deserialize the response content as JSON</summary>
        /// <param name="response">The response</param>
        /// <returns>If successful, the object graph deserialized from JSON; otherwise, null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Do not throw exceptions from try methods")]
        public static IDictionary<string, object> TryDeserializeContentJson(this HttpResponseMessage response)
        {
            try
            {
                return JsonSerializer.Deserialize<IDictionary<string, object>>(response.Content.ReadAsString());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Checks if the response is a redirect</summary>
        /// <param name="response">THe response</param>
        /// <returns>True if the response is a redirect; otherwise, false.</returns>
        public static bool IsRedirect(this HttpResponseMessage response)
        {
            return ((int)response.StatusCode)
                .ToString(CultureInfo.InvariantCulture)
                .StartsWith("3", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>If the response is a redirect, resubmits the request to the redirected location</summary>
        /// <param name="response">The response</param>
        /// <param name="client">Client to use to follow the redirect</param>
        /// <returns>
        /// If the original response is a redirect, the response from following it; otherwise, the original response.
        /// </returns>
        public static HttpResponseMessage FollowIfRedirect(this HttpResponseMessage response, RestTestClient client)
        {
            return response.FollowIfRedirect(client, 50);
        }

        /// <summary>If the response is a redirect, resubmits the request to the redirected location</summary>
        /// <param name="response">The response</param>
        /// <param name="client">Client to use to follow the redirect</param>
        /// <param name="maximumRedirects">Maximum automatic redirects allowed</param>
        /// <returns>
        /// If the original response is a redirect, the response from following it; otherwise, the original response.
        /// </returns>
        public static HttpResponseMessage FollowIfRedirect(this HttpResponseMessage response, RestTestClient client, int maximumRedirects)
        {
            return response.IsRedirect() ?
                client.SendRequest(HttpMethod.GET.ToString(), response.Headers.Location, HttpContent.CreateEmpty(), maximumRedirects) :
                response;
        }

        /// <summary>Sleep for the specified number of milliseconds</summary>
        /// <remarks>Useful for waiting before following redirects</remarks>
        /// <param name="response">The response</param>
        /// <param name="millisecondsTimeout">Time to sleep</param>
        /// <returns>The response (for chaining)</returns>
        public static HttpResponseMessage Sleep(this HttpResponseMessage response, int millisecondsTimeout)
        {
            Thread.Sleep(millisecondsTimeout);
            return response;
        }

        /// <summary>Recursively searches an object graph for values with the specified key</summary>
        /// <param name="graph">The object graph</param>
        /// <param name="key">The value key</param>
        /// <returns>All values for the specified keys</returns>
        internal static IEnumerable<object> GetValuesForKey(IEnumerable<KeyValuePair<string, object>> graph, string key)
        {
            var values = new List<object>();
            GetValuesForKeyRecursive(graph, key, ref values);
            return values;
        }

        /// <summary>Recursively searches an object graph for a match for the expected key and value</summary>
        /// <param name="graph">The object graph</param>
        /// <param name="key">The value key</param>
        /// <param name="values">All values for the key</param>
        internal static void GetValuesForKeyRecursive(IEnumerable<KeyValuePair<string, object>> graph, string key, ref List<object> values)
        {
            foreach (var value in graph)
            {
                if (value.Key == key)
                {
                    values.Add(value.Value);
                }

                if (value.Value is object[])
                {
                    var subGraph = new List<object>(value.Value as object[])
                        .Cast<IEnumerable<KeyValuePair<string, object>>>()
                        .SelectMany(v => v);
                    GetValuesForKeyRecursive(subGraph, key, ref values);
                }
                else if (value.Value is IEnumerable<KeyValuePair<string, object>>)
                {
                    var subValues = value.Value as IEnumerable<KeyValuePair<string, object>>;
                    GetValuesForKeyRecursive(subValues, key, ref values);
                }
            }
        }

        /// <summary>Fails the assertion with the specified message as well as details of the response</summary>
        /// <param name="response">The response that failed the assertion</param>
        /// <param name="message">The failure message</param>
        /// <param name="parameters">Message parameters</param>
        private static void Fail(HttpResponseMessage response, string message, params object[] parameters)
        {
            var requestHeaders = response.Request.Headers
                .Select(kvp => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1}",
                    kvp.Key,
                    string.Join("; ", kvp.Value)));
            var responseHeaders = response.Headers
                .Select(kvp => string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: {1}",
                    kvp.Key,
                    string.Join("; ", kvp.Value)));
            var responseContent = response.Content.ReadAsString();
            var responseDetails = string.Format(
                CultureInfo.InvariantCulture,
                "\n----------------------------------------\nRequest: {0} {1}\n{2}\n----------------------------------------\nResponse: {3} [{4}] ({5} {6})\n{7}\n\n{8}",
                response.Request.Method,
                response.Request.Uri,
                string.Join("\n", requestHeaders),
                response.StatusCode,
                (int)response.StatusCode,
                response.Method,
                response.Uri,
                string.Join("\n", responseHeaders),
                responseContent);
            var formattedMessage = string.Format(
                CultureInfo.InvariantCulture,
                message,
                parameters);
            Assert.Fail(formattedMessage + responseDetails);
        }
    }
}
