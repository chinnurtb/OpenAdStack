//-----------------------------------------------------------------------
// <copyright file="HttpContextClaimRetriever.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
