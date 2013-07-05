//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
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
