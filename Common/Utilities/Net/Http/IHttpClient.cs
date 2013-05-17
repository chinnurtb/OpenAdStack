//-----------------------------------------------------------------------
// <copyright file="IHttpClient.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.Http;
using Microsoft.Http.Headers;

namespace Utilities.Net.Http
{
    /// <summary>Defines an interface for an HTTP client</summary>
    public interface IHttpClient : IDisposable
    {
        /// <summary>Gets or sets the base address for requests</summary>
        Uri BaseAddress { get; set; }

        /// <summary>Gets or sets the default request headers</summary>
        RequestHeaders DefaultHeaders { get; set; }
        
        /// <summary>Gets or sets the transport settings</summary>
        HttpWebRequestTransportSettings TransportSettings { get; set; }
        
        /// <summary>Sends the request</summary>
        /// <param name="request">The request</param>
        /// <returns>The response</returns>
        HttpResponseMessage Send(HttpRequestMessage request);
    }
}
