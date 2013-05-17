//-----------------------------------------------------------------------
// <copyright file="GoogleDfpClientException.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpClient
{
    /// <summary>Exception for Google DFP client errors</summary>
    [SuppressMessage("Microsoft.Design", "CA1032", Justification = "The only constructors that should be used are the ones included here")]
    [Serializable]
    public sealed class GoogleDfpClientException : Exception
    {
        /// <summary>Separator for detailed exception message sections</summary>
        internal const string MessageSeparator = "\r\n--------------------------------------------------------------------------------\r\n";

        /// <summary>The exception message</summary>
        private readonly string message;

        /// <summary>Initializes a new instance of the GoogleDfpClientException class.</summary>
        /// <param name="dfpException">The cause of the error.</param>
        public GoogleDfpClientException(DfpException dfpException)
            : this(dfpException, null)
        {
        }

        /// <summary>Initializes a new instance of the GoogleDfpClientException class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="args">Arguments for the message.</param>
        public GoogleDfpClientException(string message, params object[] args)
            : this(null, message, args)
        {
        }

        /// <summary>Initializes a new instance of the GoogleDfpClientException class.</summary>
        /// <param name="dfpException">The cause of the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="args">Arguments for the message.</param>
        public GoogleDfpClientException(DfpException dfpException, string message, params object[] args)
            : base(string.Empty, dfpException)
        {
            var sb = new StringBuilder(!string.IsNullOrWhiteSpace(message) ? message.FormatInvariant(args) : null);

            // Build a detailed message from the Google DFP exception/errors (if available)
            if (dfpException != null)
            {
                if (message != null)
                {
                    sb.Append(MessageSeparator);
                }

                sb.Append("DfpException: " + dfpException.Message);

                if (this.IsApiException)
                {
                    sb.Append(MessageSeparator);
                    sb.Append("ApiException: " + this.ApiException.message);
                    sb.Append(MessageSeparator);
                    sb.Append("Error(s):\r\n");
                    sb.Append(string.Join("\r\n", this.ApiErrors.Select(e => "{0} @ {1}; trigger: {2}".FormatInvariant(e.errorString, e.fieldPath, e.trigger))));
                }
            }

            this.message = sb.ToString();
        }

        /// <summary>Gets a message that describes the current exception</summary>
        public override string Message
        {
            get { return this.message; }
        }

        /// <summary>Gets a value indicating whether the underlying cause is a DfpApiException</summary>
        public bool IsApiException
        {
            get { return this.InnerException is DfpApiException; }
        }

        /// <summary>Gets the Google DFP ApiException</summary>
        internal ApiException ApiException
        {
            get { return this.IsApiException ? (ApiException)((DfpApiException)this.InnerException).ApiException : null; }
        }

        /// <summary>Gets the Google DFP ApiErrors</summary>
        internal IEnumerable<ApiError> ApiErrors
        {
            get { return this.IsApiException ? this.ApiException.errors : new ApiError[0]; }
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

        /// <summary>
        /// Gets a value indicating whether the specified error type is one of the
        /// ApiErrors included in the original Google DFP ApiException.
        /// </summary>
        /// <param name="apiErrorType">Type of the Google DFP ApiError</param>
        /// <returns>
        /// True if one or more instances of TApiError are present;
        /// Otherwise, false.
        /// </returns>
        public bool ContainsApiError(string apiErrorType)
        {
            Type t = Type.GetType(apiErrorType, false, true);
            return this.ApiErrors.Any(e => t.IsInstanceOfType(t));
        }

        /// <summary>
        /// Gets a value indicating whether the specified error type is one of the
        /// ApiErrors included in the original Google DFP ApiException.
        /// </summary>
        /// <typeparam name="TApiError">Type of the Google DFP ApiError</typeparam>
        /// <returns>
        /// True if one or more instances of TApiError are present;
        /// Otherwise, false.
        /// </returns>
        internal bool ContainsApiError<TApiError>() where TApiError : ApiError
        {
            return this.ApiErrors.Any(e => e is TApiError);
        }
    }
}