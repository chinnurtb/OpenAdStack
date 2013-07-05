// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DynamicAllocationException.cs" company="Rare Crowds Inc">
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