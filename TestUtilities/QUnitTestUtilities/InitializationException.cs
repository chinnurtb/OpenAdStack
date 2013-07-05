//-----------------------------------------------------------------------
// <copyright file="InitializationException.cs" company="Rare Crowds Inc">
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
// <remarks>
//      You may find some additional information about this at https://github.com/robdmoore/NQUnit and https://github.com/Sqdw/SQUnit. 
// </remarks>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace TestUtilities
{
    /// <summary>
    /// The exception that is thrown when QUnit initialization is failed.
    /// </summary>
    [Serializable]
    public class InitializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InitializationException class
        /// </summary>
        public InitializationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the InitializationException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InitializationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InitializationException class with a specified
        /// error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If the innerException
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        public InitializationException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the InitializationException class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context"> The contextual information about the source or destination.</param>
        protected InitializationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}