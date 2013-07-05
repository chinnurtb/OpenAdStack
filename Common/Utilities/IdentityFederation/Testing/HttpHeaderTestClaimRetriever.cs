//-----------------------------------------------------------------------
// <copyright file="HttpHeaderTestClaimRetriever.cs" company="Rare Crowds Inc">
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
using System.Web;
using Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace Utilities.IdentityFederation.Testing
{
    /// <summary>
    /// Attempts to retrieve claim values from the request header for testing
    /// </summary>
    public class HttpHeaderTestClaimRetriever : HttpContextClaimRetriever
    {
        /// <summary>Header in which test claims are sent</summary>
        private const string TestClaimsHeader = "X-Test-Claims";

        /// <summary>
        /// Initializes a new instance of the HttpHeaderTestClaimRetriever class.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2122", Justification = "Will be running within Azure emulator")]
        public HttpHeaderTestClaimRetriever()
        {
            if (!RoleEnvironment.IsAvailable || !RoleEnvironment.IsEmulated)
            {
                throw new InvalidOperationException("Not supported outside of compute emulator");
            }
        }

        /// <summary>Gets the value for the specified claim.</summary>
        /// <param name="claimType">The claim type.</param>
        /// <returns>The claim value, if available; otherwise null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "If anything goes wrong, fall back to base behavior")]
        public override string GetClaimValue(string claimType)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(HttpContext.Current.Request.Headers[TestClaimsHeader]))
                {
                    var claimsHeader = HttpContext.Current.Request.Headers[TestClaimsHeader];
                    LogManager.Log(
                        LogLevels.Trace,
                        "Getting claims from HTTP header '{0}': {1}",
                        TestClaimsHeader,
                        claimsHeader);
                    var claims = claimsHeader
                        .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(claim => claim.Split(new[] { '\\' }, 2))
                        .ToDictionary(claim => claim[0], claim => claim[1]);
                    return claims[claimType];
                }
                else
                {
                    LogManager.Log(
                        LogLevels.Trace,
                        "HTTP header value for {0} not found. Getting default claim from AppSetting.",
                        TestClaimsHeader);
                    return ConfigurationManager.AppSettings["Testing.DefaultClaim"];
                }
            }
            catch (Exception e)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Defaulting to claim from HttpContext. Unable to retrieve test claim from header or default: {0}",
                    e);
                return base.GetClaimValue(claimType);
            }
        }
    }
}
