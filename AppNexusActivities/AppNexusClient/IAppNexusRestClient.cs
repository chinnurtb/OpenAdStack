//-----------------------------------------------------------------------
// <copyright file="IAppNexusRestClient.cs" company="Rare Crowds Inc">
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
    /// <summary>REST client for AppNexus</summary>
    public interface IAppNexusRestClient : IHttpRestClient
    {
        /// <summary>Gets a string identifying this AppNexus REST client</summary>
        string Id { get; }

        /// <summary>Gets the values from an AppNexus API response</summary>
        /// <param name="httpResponseContent">The response</param>
        /// <returns>If the response content is valid JSON, the values; Otherwise null.</returns>
        IDictionary<string, object> TryGetResponseValues(string httpResponseContent);

        /// <summary>
        /// Checks if the AppNexus response values contain an error
        /// </summary>
        /// <param name="responseValues">The response values</param>
        /// <returns>True if the response contains an error; Otherwise, false.</returns>
        bool IsErrorResponse(IDictionary<string, object> responseValues);
    }
}
