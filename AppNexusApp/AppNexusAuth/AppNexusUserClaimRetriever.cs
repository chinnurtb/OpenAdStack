//-----------------------------------------------------------------------
// <copyright file="AppNexusUserClaimRetriever.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Diagnostics;
using Microsoft.Http;
using Newtonsoft.Json;
using Utilities.IdentityFederation;
using Utilities.Storage;

namespace AppNexusApp.AppNexusAuth
{
    /// <summary>
    /// Attempts to retrieve claim values from the request header for testing
    /// </summary>
    public class AppNexusUserClaimRetriever : IClaimRetriever
    {
        /// <summary>How many hours to cache user tokens before reverification</summary>
        private const int UserTokenCacheExpirationHours = 2;

        /// <summary>Query value containing the AppNexus user id token</summary>
        private const string ApnxUserIdToken = "ApnxUserIdToken";

        /// <summary>Query value containing the AppNexus api url</summary>
        private const string ApnxApiUrl = "ApnxApiUrl";

        /// <summary>Query value containing the user token (for verification api)</summary>
        private const string UserTokenQueryValue = "user_token";

        /// <summary>Name of the user verification service</summary>
        private const string UserVerificationService = "user-verification";

        /// <summary>Verification response property containing the user id</summary>
        private const string ResponseUserIdProperty = "user-id";

        /// <summary>Anti-man-in-the-middle API URL test regex</summary>
        private static readonly Regex AppNexusUrlTest =
            new Regex("^(http|https)://[^/]*?(appnexus\\.com|adnxs\\.net)/");

        /// <summary>Backing field for UserTokenCache</summary>
        private static IPersistentDictionary<Tuple<DateTime, string>> userTokenCache;

        /// <summary>Gets the dictionary where user tokens are cached</summary>
        private static IPersistentDictionary<Tuple<DateTime, string>> UserTokenCache
        {
            get
            {
                return userTokenCache = userTokenCache ??
                    PersistentDictionaryFactory.CreateDictionary<Tuple<DateTime, string>>("ApnxUserTokens");
            }
        }

        /// <summary>Gets the value for the specified claim.</summary>
        /// <param name="claimType">The claim type.</param>
        /// <returns>The claim value, if available; otherwise null.</returns>
        public string GetClaimValue(string claimType)
        {
            if (!HttpContext.Current.Request.Cookies.AllKeys.Contains(ApnxApiUrl) ||
                !HttpContext.Current.Request.Cookies.AllKeys.Contains(ApnxUserIdToken))
            {
                throw new InvalidOperationException("Missing one or more required cookie values");
            }

            var apiUrl = new Uri(HttpContext.Current.Request.Cookies[ApnxApiUrl].Value);
            if (!AppNexusUrlTest.IsMatch(apiUrl.AbsoluteUri))
            {
                throw new InvalidOperationException("Invalid {0} query value: '{1}'".FormatInvariant(ApnxApiUrl, apiUrl));
            }

            var userIdToken = HttpContext.Current.Request.Cookies[ApnxUserIdToken].Value;

            // Get the user id cached for the token or get a new one
            // TODO: Need to clean the cache at some point
            if (!UserTokenCache.ContainsKey(userIdToken) ||
                DateTime.UtcNow > UserTokenCache[userIdToken].Item1)
            {
                using (var client = new HttpClient(apiUrl))
                {
                    var httpResponse = client.Get(
                        new Uri(apiUrl, UserVerificationService),
                        new Dictionary<string, string>
                        {
                            { UserTokenQueryValue, userIdToken }
                        });
                    var responseJson = httpResponse.Content.ReadAsString();
                    var userId = GetUserIdFromJson(responseJson);
                    UserTokenCache[userIdToken] = new Tuple<DateTime, string>(
                        DateTime.UtcNow.AddHours(UserTokenCacheExpirationHours),
                        userId.ToString());
                }
            }

            return UserTokenCache[userIdToken].Item2;
        }

        /// <summary>Gets the user id from the json response</summary>
        /// <param name="json">The response json</param>
        /// <returns>The user id</returns>
        internal static string GetUserIdFromJson(string json)
        {
            var response = JsonConvert.DeserializeObject<IDictionary<string, IDictionary<string, object>>>(json);
            var userId = response["response"][ResponseUserIdProperty];
            return userId.ToString();
        }
    }
}
