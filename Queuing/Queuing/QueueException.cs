//-----------------------------------------------------------------------
// <copyright file="QueueException.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using System.Security;

namespace Queuing
{
    /// <summary>
    /// Custom exception for queue errors
    /// </summary>
    [Serializable]
    public class QueueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the QueueException class.
        /// </summary>
        public QueueException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the QueueException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public QueueException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the QueueException class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public QueueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the QueueException class.
        /// </summary>
        /// <param name="info">
        /// The System.Runtime.Serialization.SerializationInfo that holds the serialized
        /// object data about the exception being thrown.
        /// </param>
        /// <param name="context">
        /// The System.Runtime.Serialization.StreamingContext that contains contextual
        /// information about the source or destination.
        /// </param>
        /// <exception cref="System.ArgumentNullException">The info parameter is null.</exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">The class name is null or System.Exception.HResult is zero (0).</exception>
        protected QueueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
