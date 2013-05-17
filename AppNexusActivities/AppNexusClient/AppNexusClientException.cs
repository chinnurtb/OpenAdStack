//-----------------------------------------------------------------------
// <copyright file="AppNexusClientException.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using Diagnostics;
using Microsoft.Http;

namespace AppNexusClient
{
    /// <summary>Error IDs that can be present in AppNexus responses</summary>
    public enum AppNexusErrorId
    {
        /// <summary>The response error_id was missing or unknown</summary>
        Unknown,

        /// <summary>Authentication information is either missing or invalid</summary>
        NoAuth,
        
        /// <summary>The user is not authorized to take the requested action</summary>
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Maps to value in AppNexus response")]
        UnAuth,
        
        /// <summary>The syntax of the request is incorrect</summary>
        Syntax,
        
        /// <summary>A system error has occurred </summary>
        System,
        
        /// <summary>
        /// A client request is inconsistent; for example, an attempt to delete
        /// a default creative attached to an active placement.
        /// </summary>
        Integrity
    }

    /// <summary>Exception for AppNexus client errors</summary>
    [SuppressMessage("Microsoft.Design", "CA1032", Justification = "The only constructors that should be used are the ones included here")]
    [Serializable]
    public sealed class AppNexusClientException : Exception
    {
        /// <summary>Initializes a new instance of the AppNexusClientException class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The cause of the error.</param>
        public AppNexusClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Initializes a new instance of the AppNexusClientException class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="httpResponseContent">The http response message</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Initializing an exception must not throw other exceptions")]
        public AppNexusClientException(string message, string httpResponseContent)
            : this(message, httpResponseContent, null)
        {
        }

        /// <summary>Initializes a new instance of the AppNexusClientException class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="httpResponseContent">The http response message</param>
        /// <param name="responseValues">The JSON response values (if available)</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Initializing an exception must not throw other exceptions")]
        public AppNexusClientException(string message, string httpResponseContent, IDictionary<string, object> responseValues)
            : base(message)
        {
            this.Response = httpResponseContent;
            if (responseValues != null)
            {
                try
                {
                    AppNexusErrorId errorId;
                    this.ErrorId = Enum.TryParse((string)responseValues[AppNexusValues.ErrorId], true, out errorId) ? errorId : AppNexusErrorId.Unknown;
                    this.ErrorCode = responseValues[AppNexusValues.ErrorCode] as string;
                    this.ErrorMessage = responseValues[AppNexusValues.Error] as string;
                }
                catch
                {
                }
            }
        }

        /// <summary>Gets the AppNexus response error_id value</summary>
        public AppNexusErrorId ErrorId { get; private set; }

        /// <summary>Gets the AppNexus response error_code value</summary>
        public string ErrorCode { get; private set; }

        /// <summary>Gets the AppNexus response error value</summary>
        public string ErrorMessage { get; private set; }

        /// <summary>Gets the response content text</summary>
        public string Response { get; private set; }

        /// <summary>Returns a string representation of this exception</summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Error: {1} - {2}\n----------------------------------------\nResponse:\n{2}\n----------------------------------------\n{3}\n",
                this.ErrorId,
                this.ErrorMessage,
                this.Response,
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
