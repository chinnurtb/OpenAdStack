//-----------------------------------------------------------------------
// <copyright file="HttpClient.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Utilities.Net.Http
{
    /// <summary>HttpClient wrapper implementing IHttpClient</summary>
    internal class HttpClient : Microsoft.Http.HttpClient, IHttpClient
    {
        /// <summary>Initializes a new instance of the HttpClient class.</summary>
        /// <param name="baseAddress">Base address for requests.</param>
        public HttpClient(string baseAddress)
            : base(new Uri(baseAddress))
        {
        }
    }
}
