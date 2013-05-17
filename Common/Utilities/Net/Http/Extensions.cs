//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Linq;
using System.Net;
using Microsoft.Http;

namespace Utilities.Net.Http
{
    /// <summary>Http related extensions for internal use only.</summary>
    internal static class Extensions
    {
        /// <summary>List of status codes considered successful</summary>
        private static readonly HttpStatusCode[] SuccessStatusCodes = { HttpStatusCode.OK };

        /// <summary>
        /// Checks if the response has a successful status code
        /// </summary>
        /// <param name="response">The response</param>
        /// <returns>True if the status code is successful; otherwise, false.</returns>
        public static bool IsSuccessStatusCode(this HttpResponseMessage response)
        {
            return SuccessStatusCodes.Contains(response.StatusCode);
        }
    }
}
