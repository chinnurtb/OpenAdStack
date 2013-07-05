//-----------------------------------------------------------------------
// <copyright file="AppNexusRestClient.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using ConfigManager;
using Diagnostics;
using Microsoft.Http;
using Utilities.Net;
using Utilities.Net.Http;
using Utilities.Storage;

namespace AppNexusClient
{
    /// <summary>REST client for AppNexus using credentials from config</summary>
    [SuppressMessage("Microsoft.Design", "CA1063", Justification = "IDisposable is correctly implemented by HttpRestClient")]
    public class AppNexusRestClient : AppNexusRestClientBase, IAppNexusRestClient
    {
        /// <summary>Backing field for Id</summary>
        private string id;

        /// <summary>Initializes a new instance of the AppNexusRestClient class.</summary>
        /// <param name="config">Configuration to use</param>
        public AppNexusRestClient(IConfig config)
            : base(config)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AppNexusRestClient class using the provided IHttpClient.
        /// </summary>
        /// <remarks>Used for testing with mock IHttpClients.</remarks>
        /// <param name="httpClient">Mock http client</param>
        /// <param name="config">Configuration to use</param>
        internal AppNexusRestClient(IHttpClient httpClient, IConfig config)
            : base(httpClient, config)
        {
        }

        /// <summary>Gets a string identifying this AppNexus client</summary>
        public override string Id
        {
            get { return this.id = this.id ?? this.Username; }
        }

        /// <summary>Gets the AppNexus account username</summary>
        private string Username
        {
            get { return this.Config.GetValue("AppNexus.Username"); }
        }

        /// <summary>Gets the AppNexus account password</summary>
        private string Password
        {
            get { return this.Config.GetValue("AppNexus.Password"); }
        }

        /// <summary>Authenticates the REST client</summary>
        /// <returns>The new authentication token</returns>
        internal override string Authenticate()
        {
            var authRequestJson = AppNexusJson.AuthRequestFormat
                .FormatInvariant(this.Username, this.Password);

            var httpResponse = this.Client.Send(new HttpRequestMessage
            {
                Uri = new Uri(this.Client.BaseAddress, AuthRequestUri),
                Method = HttpMethod.POST.ToString(),
                Content = HttpContent.Create(authRequestJson)
            });

            if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new AppNexusClientException(
                    "AppNexus Authentication Failed: Status Code: {0}".FormatInvariant(httpResponse.StatusCode),
                    httpResponse.Content.ReadAsString());
            }

            var responseContent = httpResponse.Content.ReadAsString();
            var values = TryGetResponseValues(responseContent);
            if (values == null)
            {
                throw new AppNexusClientException(
                    "Authentication Failed: Unable to get values from response",
                    responseContent);
            }

            if (IsErrorResponse(values))
            {
                var exception = new AppNexusClientException(
                    "AppNexus Authentication Failed",
                    responseContent,
                    values);
                if (exception.ErrorId == AppNexusErrorId.System &&
                    exception.ErrorMessage.Contains(AuthLimitExceededErrorMessage))
                {
                    // Authentication limit has been exceeded
                    // Sleep until the auth limit period has passed
                    Thread.Sleep(AuthLimitPeriodSeconds * 1000);
                }

                throw exception;
            }

            if (!values.ContainsKey(AppNexusValues.AuthToken))
            {
                throw new AppNexusClientException(
                    "AppNexus Authentication Failed (response did not contain an auth token)",
                    responseContent,
                    values);
            }

            return values[AppNexusValues.AuthToken] as string;
        }
    }
}
