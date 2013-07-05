//-----------------------------------------------------------------------
// <copyright file="IHttpRestClient.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Http;
using Utilities.Net.Http;
using Utilities.Serialization;

namespace Utilities.Net
{
    /// <summary>
    /// Provides basic functionality for a throttled REST client
    /// </summary>
    public interface IHttpRestClient : IDisposable
    {
        /// <summary>Gets the service endpoint URI</summary>
        Uri Endpoint { get; }

        /// <summary>Gets or sets how many times to retry sending requests</summary>
        int MaxRetries { get; set; }

        /// <summary>Gets or sets the time (in milliseconds) to wait between retries</summary>
        TimeSpan RetryWait { get; set; }

        /// <summary>Gets or sets how long until requests timeout (in milliseconds)</summary>
        TimeSpan Timeout { get; set; }

        /// <summary>Deletes the object at the specified URI</summary>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        string Delete(string objectPath, params object[] args);

        /// <summary>Gets the object at the specified URI as a TResponse</summary>
        /// <typeparam name="TResponse">Type of the object to get. Must implement IJsonSerializable</typeparam>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The content of the object</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        [SuppressMessage("Microsoft.Naming", "CA1716", Justification = "'Get' is correct")]
        TResponse Get<TResponse>(string objectPath, params object[] args);

        /// <summary>Gets the object at the specified URI</summary>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The content of the object</returns>
        [SuppressMessage("Microsoft.Naming", "CA1716", Justification = "'Get' is correct")]
        string Get(string objectPath, params object[] args);

        /// <summary>Posts the object to the specified URI (after serializing it to JSON)</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        TResponse Post<TResponse>(object content, string objectPath, params object[] args);

        /// <summary>Posts the JSON content to the specified URI</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        TResponse Post<TResponse>(string content, string objectPath, params object[] args);

        /// <summary>Posts the content to the object at the specified URI</summary>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        string Post(string content, string objectPath, params object[] args);

        /// <summary>Puts the object to the specified URI (after serializing it to JSON)</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        TResponse Put<TResponse>(object content, string objectPath, params object[] args);

        /// <summary>Puts the JSON content to the specified URI</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        TResponse Put<TResponse>(string content, string objectPath, params object[] args);

        /// <summary>Puts the content to the object at the specified URI</summary>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        string Put(string content, string objectPath, params object[] args);
    }
}
