// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAccessEntityNotFoundException.cs" company="Rare Crowds Inc">
// Copyright 2012-2013 Rare Crowds, Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
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