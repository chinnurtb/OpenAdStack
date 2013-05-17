// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAccessEntityNotFoundException.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace DataAccessLayer
{
    /// <summary>Custom exception for DAL entity not found error.</summary>
    [Serializable]
    public class DataAccessEntityNotFoundException : DataAccessException
    {
        /// <summary>Initializes a new instance of the <see cref="DataAccessEntityNotFoundException"/> class.</summary>
        public DataAccessEntityNotFoundException()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessEntityNotFoundException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        public DataAccessEntityNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessEntityNotFoundException"/> class.</summary>
        /// <param name="message">Message for the exception.</param>
        /// <param name="inner">The inner exception.</param>
        public DataAccessEntityNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="DataAccessEntityNotFoundException"/> class.</summary>
        /// <param name="info">SerializationInfo object</param>
        /// <param name="context">StreamingContext object</param>
        protected DataAccessEntityNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}