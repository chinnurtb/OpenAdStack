//-----------------------------------------------------------------------
// <copyright file="IAppNexusRestClient.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
