//-----------------------------------------------------------------------
// <copyright file="HttpRestClient.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Web.Script.Serialization;
using Diagnostics;
using Microsoft.Http;
using Utilities.Net.Http;
using Utilities.Runtime;

namespace Utilities.Net
{
    /// <summary>
    /// Provides basic functionality for a throttled REST client
    /// </summary>
    public abstract class HttpRestClient : IHttpRestClient, IDisposable
    {
        /// <summary>Default Timeout</summary>
        private static readonly TimeSpan DefaultTimeout = new TimeSpan(0, 0, 3);

        /// <summary>Default MaxRetries</summary>
        private const int DefaultMaxRetries = 5;
        
        /// <summary>Default RetryWait</summary>
        private static readonly TimeSpan DefaultRetryWait = new TimeSpan(0, 0, 0, 0, 500);

        /// <summary>JavaScript (JSON) Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>
        /// Initializes a new instance of the HttpRestClient class using a new
        /// instance of Utilities.Net.Http.HttpClient.
        /// </summary>
        /// <param name="endpoint">Base URI of the REST service</param>
        protected HttpRestClient(string endpoint)
            : this(new Http.HttpClient(endpoint))
        {
        }

        /// <summary>
        /// Initializes a new instance of the HttpRestClient class using the
        /// provided IHttpClient instance.
        /// </summary>
        /// <param name="client">The HTTP client used to make REST requests</param>
        protected HttpRestClient(IHttpClient client)
        {
            this.Client = client;
            this.Client.TransportSettings.ConnectionTimeout = DefaultTimeout;
            this.MaxRetries = DefaultMaxRetries;
            this.RetryWait = DefaultRetryWait;
            this.Timeout = DefaultTimeout;
        }

        /// <summary>Gets or sets how many times to retry sending requests</summary>
        public int MaxRetries { get; set; }

        /// <summary>Gets or sets the time to wait between retries</summary>
        public TimeSpan RetryWait { get; set; }

        /// <summary>Gets or sets how long until requests timeout</summary>
        public TimeSpan Timeout
        {
            get
            {
                return this.Client.TransportSettings.ConnectionTimeout.Value;
            }

            set
            {
                this.Client.TransportSettings.ConnectionTimeout = value;
            }
        }

        /// <summary>Gets or sets the service endpoint URI</summary>
        public Uri Endpoint
        {
            get
            {
                return this.Client.BaseAddress;
            }

            set
            {
                this.Client.BaseAddress = value;
            }
        }

        /// <summary>Gets the IHttpClient used for making REST requests</summary>
        protected IHttpClient Client { get; private set; }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used
        /// by the Utilities.Net.HttpRestClient.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>Deletes the object at the specified URI</summary>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        public string Delete(string objectPath, params object[] args)
        {
            return this.Send(this.BuildRequestUri(objectPath, args), HttpMethod.DELETE, null);
        }

        /// <summary>Gets the object at the specified URI as a TResponse</summary>
        /// <typeparam name="TResponse">Type of the object to get. Must implement IJsonSerializable</typeparam>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The content of the object</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        public TResponse Get<TResponse>(string objectPath, params object[] args)
        {
            return JsonSerializer.Deserialize<TResponse>(this.Get(objectPath, args));
        }

        /// <summary>Gets the object at the specified URI</summary>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The content of the object</returns>
        public string Get(string objectPath, params object[] args)
        {
            return this.Send(this.BuildRequestUri(objectPath, args), HttpMethod.GET, null);
        }

        /// <summary>Posts the TContent object to the specified URI</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        public TResponse Post<TResponse>(object content, string objectPath, params object[] args)
        {
            return this.Post<TResponse>(JsonSerializer.Serialize(content), objectPath, args);
        }

        /// <summary>Posts the JSON content to the specified URI</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        public TResponse Post<TResponse>(string content, string objectPath, params object[] args)
        {
            return JsonSerializer.Deserialize<TResponse>(this.Post(content, objectPath, args));
        }

        /// <summary>Posts the JSON content to the object at the specified URI</summary>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        public string Post(string content, string objectPath, params object[] args)
        {
            return this.Send(this.BuildRequestUri(objectPath, args), HttpMethod.POST, content);
        }

        /// <summary>Puts the TContent object to the specified URI</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        public TResponse Put<TResponse>(object content, string objectPath, params object[] args)
        {
            return this.Put<TResponse>(JsonSerializer.Serialize(content), objectPath, args);
        }

        /// <summary>Puts the JSON content to the specified URI</summary>
        /// <typeparam name="TResponse">Type to deserialize the response to. Must implement IJsonSerializable.</typeparam>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        /// <seealso cref="Utilities.Serialization.IJsonSerializable"/>
        public TResponse Put<TResponse>(string content, string objectPath, params object[] args)
        {
            return JsonSerializer.Deserialize<TResponse>(this.Put(content, objectPath, args));
        }

        /// <summary>Puts the JSON content to the object at the specified URI</summary>
        /// <param name="content">The object content</param>
        /// <param name="objectPath">URI path of the object, relative to the service endpoint. Can be a format string.</param>
        /// <param name="args">Optional arguments for if the URI is a format string.</param>
        /// <returns>The response content</returns>
        public string Put(string content, string objectPath, params object[] args)
        {
            return this.Send(this.BuildRequestUri(objectPath, args), HttpMethod.PUT, content);
        }

        /// <summary>
        /// Adds authentication to the request before it is sent
        /// </summary>
        /// <param name="httpRequestMessage">The request about to be sent</param>
        public abstract void AddAuthentication(ref HttpRequestMessage httpRequestMessage);

        /// <summary>Sends a REST request to the service with retries</summary>
        /// <param name="uri">URI of the request</param>
        /// <param name="method">HTTP verb for the request</param>
        /// <param name="content">Request content (optional)</param>
        /// <returns>The response</returns>
        internal string Send(Uri uri, HttpMethod method, string content)
        {
            var retries = 0;
            while (true)
            {
                var request = new HttpRequestMessage
                {
                    Uri = uri,
                    Method = method.ToString(),
                    Content = content != null ?
                        HttpContent.Create(content) :
                        HttpContent.CreateEmpty()
                };

                try
                {
                    return this.SendMessage(request);
                }
                catch (Exception e)
                {
                    LogManager.Log(
                        LogLevels.Warning,
                        "HttpRestClient.Send('{0}', '{1}', content) - Error sending request (attempt {2} of {3}): {4}",
                        uri,
                        method,
                        retries,
                        this.MaxRetries,
                        e);

                    if (retries++ >= this.MaxRetries)
                    {
                        var message = "HttpRestClient.Send('{0}', '{1}', content) - Failed after {2} retries."
                            .FormatInvariant(uri, method, this.MaxRetries);
                        LogManager.Log(LogLevels.Error, message);
                        throw new HttpRestClientException(message, e);
                    }

                    Thread.Sleep(this.RetryWait);
                }
            }
        }

        /// <summary>Sends a REST request to the service</summary>
        /// <param name="request">The request</param>
        /// <returns>The response</returns>
        internal string SendMessage(HttpRequestMessage request)
        {
            this.AddAuthentication(ref request);
            var response = this.Client.Send(request);
            return this.HandleResponse(response);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Utilities.Net.HttpRestClient and
        /// optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// True to release both managed and unmanaged resources; false to releases only
        /// unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            this.Client.Dispose();
        }

        /// <summary>
        /// Handles the response from the server and either returns the response content
        /// or throws HttpRestClientException.
        /// </summary>
        /// <param name="httpResponseMessage">The response received</param>
        /// <returns>The response content</returns>
        /// <exception cref="HttpRestClientException">
        /// There was an error in the response
        /// </exception>
        protected abstract string HandleResponse(HttpResponseMessage httpResponseMessage);

        /// <summary>
        /// Makes a request Uri from the provided object path and arguments.
        /// </summary>
        /// <param name="objectPath">The object path</param>
        /// <param name="args">The arguments (optional)</param>
        /// <returns>The Uri</returns>
        private Uri BuildRequestUri(string objectPath, params object[] args)
        {
            return new Uri(this.Endpoint, objectPath.FormatInvariant(args));
        }
    }
}
