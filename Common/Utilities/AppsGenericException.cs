// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppsGenericException.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Utilities
{
    /// <summary>Custom exception for application errors.</summary>
    [Serializable]
    public class AppsGenericException : Exception
    {
        /// <summary>Initializes a new instance of the <see cref="AppsGenericException"/> class.</summary>
        public AppsGenericException()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AppsGenericException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public AppsGenericException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AppsGenericException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public AppsGenericException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="AppsGenericException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected AppsGenericException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}