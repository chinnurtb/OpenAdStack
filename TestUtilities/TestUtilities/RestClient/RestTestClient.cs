// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RestTestClient.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Microsoft.Http;

namespace TestUtilities
{
    /// <summary>REST client specialized for testing</summary>
    public class RestTestClient
    {
        /// <summary>Header in which test claims are sent</summary>
        private const string TestClaimsHeader = "X-Test-Claims";

        /// <summary>Default value for MaxRetries</summary>
        private const int DefaultMaxRetries = 3;

        /// <summary>Default value for RetryWait</summary>
        private const int DefaultRetryWait = 100;

        /// <summary>Initializes a new instance of the RestTestClient class.</summary>
        /// <param name="serviceAddress">Service base address</param>
        public RestTestClient(string serviceAddress)
            : this(serviceAddress, DefaultMaxRetries, DefaultRetryWait)
        {
        }

        /// <summary>Initializes a new instance of the RestTestClient class.</summary>
        /// <param name="serviceAddress">Service base address</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="retryWait">Time to wait between retries (in milliseconds)</param>
        public RestTestClient(string serviceAddress, int maxRetries, int retryWait)
        {
            this.Claims = new Dictionary<string, string>();
            this.MaxRetries = maxRetries;
            this.RetryWait = retryWait;
            this.ServiceAddress = new Uri(serviceAddress);
        }

        /// <summary>Gets or sets the maximum retries</summary>
        public int MaxRetries { get; set; }

        /// <summary>Gets or sets the time to wait between retries (in milliseconds)</summary>
        public int RetryWait { get; set; }

        /// <summary>Gets the service address</summary>
        public Uri ServiceAddress { get; private set; }

        /// <summary>Gets the claims collection</summary>
        public IDictionary<string, string> Claims { get; private set; }

        /// <summary>
        /// Sends a request to a url with optional content using the specified verb
        /// </summary>
        /// <param name="method">The HTTP verb</param>
        /// <param name="objectPath">The resource location</param>
        /// <returns>The response content</returns>
        public HttpResponseMessage SendRequest(HttpMethod method, string objectPath)
        {
            return this.SendRequest(method, objectPath, null);
        }

        /// <summary>
        /// Sends a request to a url with optional content using the specified verb
        /// </summary>
        /// <param name="method">The HTTP verb</param>
        /// <param name="objectPath">The resource location</param>
        /// <param name="content">The optional request content</param>
        /// <returns>The response content</returns>
        public HttpResponseMessage SendRequest(HttpMethod method, string objectPath, string content)
        {
            return this.SendRequest(
                method.ToString(),
                new Uri(this.ServiceAddress, objectPath),
                content != null ? HttpContent.Create(content) : HttpContent.CreateEmpty(),
                0);
        }

        /// <summary>
        /// Sends a request to a url with optional content using the specified verb
        /// </summary>
        /// <param name="method">The HTTP verb</param>
        /// <param name="uri">The resource location</param>
        /// <param name="content">The optional request content</param>
        /// <param name="maximumRedirects">Maximum automatic redirects allowed</param>
        /// <returns>The response content</returns>
        internal HttpResponseMessage SendRequest(string method, Uri uri, HttpContent content, int maximumRedirects)
        {
            var retries = this.MaxRetries;
            while (true)
            {
                var request = new HttpRequestMessage { Uri = uri, Method = method, Content = content };
                this.AddClaimsHeader(ref request);
                if (content.GetLength() > 0)
                {
                    request.Headers.ContentLength = content.GetLength();
                    request.Headers.ContentType = "application/json";
                }

                try
                {
                    using (var client = new HttpClient(this.ServiceAddress))
                    {
                        // Ignore SSL errors such as self signed certificates
                        ServicePointManager.ServerCertificateValidationCallback =
                            (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                            {
                                return true;
                            };

                        // Limit automatic redirects
                        client.TransportSettings.MaximumAutomaticRedirections = maximumRedirects;

                        // Send the request and load the content into a buffer
                        var response = client.Send(request);
                        response.Content.LoadIntoBuffer();
                        return response;
                    }
                }
                catch
                {
                    if (--retries > 0)
                    {
                        Thread.Sleep(this.RetryWait);
                        continue;
                    }

                    throw;
                }
            }
        }

        /// <summary>Adds the claims to the request as a header value</summary>
        /// <param name="request">Request to which the claims header is to be added.</param>
        private void AddClaimsHeader(ref HttpRequestMessage request)
        {
            var claims = this.Claims.Select(claim => string.Join("\\", claim.Key, claim.Value));
            request.Headers[TestClaimsHeader] = string.Join("|", claims);
        }
    }
}
