// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAccessException.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using Utilities;

namespace DataAccessLayer
{
    /// <summary>Custom exception for DAL errors.</summary>
    [Serializable]
    public class DataAccessException : AppsGenericException
    {
        /// <summary>Initializes a new instance of the <see cref="DataAccessException"/> class.</summary>
        public DataAccessException()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public DataAccessException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public DataAccessException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected DataAccessException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}