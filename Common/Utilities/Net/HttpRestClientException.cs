//-----------------------------------------------------------------------
// <copyright file="HttpRestClientException.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Http;

namespace Utilities.Net
{
    /// <summary>Exception for AppNexus client errors</summary>
    [SuppressMessage("Microsoft.Design", "CA1032", Justification = "The only constructors that should be used are the ones included here")]
    [Serializable]
    public sealed class HttpRestClientException : Exception
    {
        /// <summary>Initializes a new instance of the HttpRestClientException class.</summary>
        /// <param name="message">The error message that explains the reason for the exception</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public HttpRestClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Initializes a new instance of the HttpRestClientException class.</summary>
        /// <param name="httpResponseMessage">The http response message</param>
        public HttpRestClientException(HttpResponseMessage httpResponseMessage)
            : this(null, httpResponseMessage)
        {
        }

        /// <summary>Initializes a new instance of the HttpRestClientException class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="httpResponseMessage">The http response message</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "TODO: Look up what exceptions are thrown by Content.ReadAsString")]
        public HttpRestClientException(string message, HttpResponseMessage httpResponseMessage)
            : base(message)
        {
            this.Uri = httpResponseMessage.Uri;
            this.HttpStatusCode = (int)httpResponseMessage.StatusCode;
            try
            {
                this.ResponseContent = httpResponseMessage.Content.ReadAsString();
            }
            catch
            {
            }
        }

        /// <summary>Gets the HTTP status code in the response</summary>
        public int HttpStatusCode { get; private set; }

        /// <summary>Gets the URI of the request</summary>
        public Uri Uri { get; private set; }

        /// <summary>Gets the response content text</summary>
        public string ResponseContent { get; private set; }

        /// <summary>Returns a string representation of this exception</summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Status: {0} Uri: {1}\n----------------------------------------\n{2}\n----------------------------------------\n{3}",
                this.HttpStatusCode,
                this.Uri,
                this.ResponseContent,
                base.ToString());
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">
        /// SerializationInfo that holds the serialized object data about the exception being thrown
        /// </param>
        /// <param name="context">
        /// StreamingContext that contains contextual information about the source or destination.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// The info parameter is a null reference.
        /// </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
