//-----------------------------------------------------------------------
// <copyright file="HttpContextClaimRetriever.cs" company="Rare Crowds Inc">
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

using System.Linq;
using System.Web;
using Microsoft.IdentityModel.Claims;

namespace Utilities.IdentityFederation
{
    /// <summary>
    /// Retrieves claim values using a ClaimsPrincipal created from the current HttpContext
    /// </summary>
    public class HttpContextClaimRetriever : IClaimRetriever
    {
        /// <summary>Gets the value for the specified claim.</summary>
        /// <param name="claimType">The claim type.</param>
        /// <returns>The claim value, if available; otherwise null.</returns>
        public virtual string GetClaimValue(string claimType)
        {
            if (HttpContext.Current == null)
            {
                return null;
            }

            var claimsPrincipal = ClaimsPrincipal.CreateFromHttpContext(HttpContext.Current);
            var claimsIdentity = claimsPrincipal.Identity as IClaimsIdentity;
            return claimsIdentity.Claims
                .Where(c => c.ClaimType == claimType)
                .Select(c => c.Value)
                .SingleOrDefault();
        }
    }
}
