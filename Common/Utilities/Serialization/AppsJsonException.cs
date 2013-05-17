// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppsJsonException.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Utilities.Serialization
{
    /// <summary>Custom exception for Json Serialization errors.</summary>
    [Serializable]
    public class AppsJsonException : AppsGenericException
    {
        /// <summary>Initializes a new instance of the <see cref="AppsJsonException"/> class.</summary>
        public AppsJsonException()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AppsJsonException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public AppsJsonException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AppsJsonException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public AppsJsonException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AppsJsonException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected AppsJsonException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}