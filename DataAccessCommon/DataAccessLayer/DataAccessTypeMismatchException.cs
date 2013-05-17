// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAccessTypeMismatchException.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace DataAccessLayer
{
    /// <summary>Custom exception for DAL errors.</summary>
    [Serializable]
    public class DataAccessTypeMismatchException : DataAccessException
    {
        /// <summary>Initializes a new instance of the <see cref="DataAccessTypeMismatchException"/> class.</summary>
        public DataAccessTypeMismatchException()
            : this(string.Empty)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessTypeMismatchException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public DataAccessTypeMismatchException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessTypeMismatchException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public DataAccessTypeMismatchException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessTypeMismatchException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected DataAccessTypeMismatchException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}