// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAccessStaleEntityException.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace DataAccessLayer
{
    /// <summary>Custom exception for DAL stale entity save error.</summary>
    [Serializable]
    public class DataAccessStaleEntityException : DataAccessException
    {
        /// <summary>Initializes a new instance of the <see cref="DataAccessStaleEntityException"/> class.</summary>
        public DataAccessStaleEntityException()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessStaleEntityException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public DataAccessStaleEntityException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessStaleEntityException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public DataAccessStaleEntityException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessStaleEntityException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected DataAccessStaleEntityException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}