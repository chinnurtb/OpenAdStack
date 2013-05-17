//-----------------------------------------------------------------------
// <copyright file="AppNexusRestClientBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// <summary>Abstract base class for AppNexus REST clients</summary>
    [SuppressMessage("Microsoft.Design", "CA1063", Justification = "IDisposable is correctly implemented by HttpRestClient")]
    public abstract class AppNexusRestClientBase : HttpRestClient, IAppNexusRestClient
    {
        /// <summary>Authentication information</summary>
        internal static readonly IDictionary<string, string> AuthTokens = new Dictionary<string, string>();

        /// <summary>URI for the auth request</summary>
        protected const string AuthRequestUri = "auth";

        /// <summary>Message used to check for authentication limit exceeded errors</summary>
        protected const string AuthLimitExceededErrorMessage = "You have exceeded your authentication limit";

        /// <summary>Length of the authentication limit period (in seconds)</summary>
        protected const int AuthLimitPeriodSeconds = 300;

        /// <summary>List of AppNexus error ids for which exceptions are thrown</summary>
        private static readonly AppNexusErrorId[] ExceptionalErrorIds = new[] { AppNexusErrorId.System, AppNexusErrorId.NoAuth, AppNexusErrorId.UnAuth };

        /// <summary>JavaScript (JSON) Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer() { MaxJsonLength = int.MaxValue };

        /// <summary>Initializes a new instance of the AppNexusRestClientBase class.</summary>
        /// <param name="config">Configuration to use</param>
        protected AppNexusRestClientBase(IConfig config)
            : base(config.GetValue("AppNexus.Endpoint"))
        {
            this.Config = config;
            this.Timeout = this.Config.GetTimeSpanValue("AppNexus.Timeout");
            this.MaxRetries = this.Config.GetIntValue("AppNexus.Retries");
            this.RetryWait = this.Config.GetTimeSpanValue("AppNexus.RetryWait");
        }

        /// <summary>
        /// Initializes a new instance of the AppNexusRestClientBase class using the provided IHttpClient.
        /// </summary>
        /// <remarks>Used for testing with mock IHttpClients.</remarks>
        /// <param name="httpClient">Mock http client</param>
        /// <param name="config">Configuration to use</param>
        protected AppNexusRestClientBase(IHttpClient httpClient, IConfig config)
            : base(httpClient)
        {
            this.Config = config;
            this.Timeout = this.Config.GetTimeSpanValue("AppNexus.Timeout");
            this.MaxRetries = this.Config.GetIntValue("AppNexus.Retries");
            this.RetryWait = this.Config.GetTimeSpanValue("AppNexus.RetryWait");
        }

        /// <summary>Gets a string identifying the REST client</summary>
        public abstract string Id { get; }

        /// <summary>Gets the configuration for endpoint, credentials, etc</summary>
        protected IConfig Config { get; private set; }

        /// <summary>Gets the values from an AppNexus API response</summary>
        /// <param name="httpResponseContent">The response</param>
        /// <returns>If the response content is valid JSON, the values; Otherwise null.</returns>
        public IDictionary<string, object> TryGetResponseValues(string httpResponseContent)
        {
            try
            {
                // Temporary: Trim garbage off report api responses
                if (httpResponseContent.StartsWith("{}", StringComparison.OrdinalIgnoreCase))
                {
                    httpResponseContent = httpResponseContent.Substring(2);
                }

                var response = JsonSerializer.Deserialize<IDictionary<string, object>>(httpResponseContent);
                return JsonSerializer.ConvertToType<IDictionary<string, object>>(response["response"]);
            }
            catch (ArgumentException ae)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Attempt to parse values from JSON failed: {0}",
                    ae);
            }
            catch (InvalidOperationException ioe)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Attempt to parse values from JSON failed: {0}",
                    ioe);
            }

            return null;
        }

        /// <summary>
        /// Checks if the AppNexus response values contain an error
        /// </summary>
        /// <param name="responseValues">The response values</param>
        /// <returns>True if the response contains an error; Otherwise, false.</returns>
        public bool IsErrorResponse(IDictionary<string, object> responseValues)
        {
            return responseValues.ContainsKey(AppNexusValues.Error);
        }

        /// <summary>Adds authentication information (such as headers or credentials) to a request</summary>
        /// <param name="httpRequestMessage">The request</param>
        public sealed override void AddAuthentication(ref HttpRequestMessage httpRequestMessage)
        {
            lock (AuthTokens)
            {
                if (!AuthTokens.ContainsKey(this.Id) || string.IsNullOrEmpty(AuthTokens[this.Id]))
                {
                    AuthTokens[this.Id] = this.Authenticate();
                }

                httpRequestMessage.Headers["Authorization"] = AuthTokens[this.Id];
            }
        }
        
        /// <summary>Authenticates the REST client</summary>
        /// <returns>The new authentication token</returns>
        internal abstract string Authenticate();

        /// <summary>
        /// Handles the response from the server and either returns the response content
        /// or throws HttpRestClientException.
        /// </summary>
        /// <param name="httpResponseMessage">The response received</param>
        /// <returns>The response content</returns>
        /// <exception cref="HttpRestClientException">
        /// There was an error in the response
        /// </exception>
        protected sealed override string HandleResponse(HttpResponseMessage httpResponseMessage)
        {
            // TODO: Check response status code
            var responseContent = httpResponseMessage.Content.ReadAsString();
            LogManager.Log(
                LogLevels.Trace,
                "Response:\n{0}",
                responseContent.Substring(0, Math.Min(responseContent.Length, 1000)));

            // If the response is JSON, check the response values
            var values = this.TryGetResponseValues(responseContent);
            if (values != null)
            {
                // Update the throttle
                var dbgInfo = values[AppNexusValues.DebugInfo] as IDictionary<string, object>;
                AppNexusThrottle.UpdateThrottleInfo(httpResponseMessage.Method, dbgInfo);

                // Check for and handle errors
                if (this.IsErrorResponse(values))
                {
                    // Special handling for specific errors
                    var exception = new AppNexusClientException(
                        "Error: {0}".FormatInvariant(values[AppNexusValues.Error]),
                        responseContent,
                        values);
                    if (ExceptionalErrorIds.Contains(exception.ErrorId))
                    {
                        // Auth token was missing, invalid or expired
                        if (exception.ErrorId == AppNexusErrorId.NoAuth)
                        {
                            lock (AuthTokens)
                            {
                                AuthTokens[this.Id] = this.Authenticate();
                            }
                        }

                        // Throttle rate was exceeded, sleep until the next
                        // quota period starts.
                        if (exception.ErrorCode == "RATE_EXCEEDED")
                        {
                            var now = DateTime.UtcNow;
                            if (AppNexusThrottle.NextPeriodStart > now)
                            {
                                System.Threading.Thread.Sleep(AppNexusThrottle.NextPeriodStart - now);
                            }
                        }

                        LogManager.Log(
                            LogLevels.Trace,
                            "AppNexusClientException: {0}",
                            exception);
                        throw exception;
                    }
                }
            }

            return responseContent;
        }
    }
}
