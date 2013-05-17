// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicAllocationException.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace DynamicAllocation
{
    /// <summary>
    /// an exception class for Outputs that violate DynamicAllocation's needs
    /// </summary>
    [Serializable]
    public class DynamicAllocationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAllocationException"/> class.
        /// </summary>
        public DynamicAllocationException()
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAllocationException"/> class with a string message.
        /// </summary>
        /// <param name="message">string message describing the error</param>
        public DynamicAllocationException(string message) : base(message) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAllocationException"/> class with a string message and an inner exception
        /// </summary>
        /// <param name="message">string message describing the error</param>
        /// <param name="innerException">an inner excpetion</param>
        public DynamicAllocationException(string message, Exception innerException) : base(message, innerException) 
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAllocationException"/> class for serialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        protected DynamicAllocationException(SerializationInfo info, StreamingContext context) : base(info, context) 
        { 
        }
    }
}