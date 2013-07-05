//-----------------------------------------------------------------------
// <copyright file="IHttpClient.cs" company="Rare Crowds Inc">
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
