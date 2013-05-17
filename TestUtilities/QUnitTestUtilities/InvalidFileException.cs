//-----------------------------------------------------------------------
// <copyright file="InvalidFileException.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
    /// The exception that is thrown when one of the test files provided is not valid. (e.g.: test file is 
    /// missing the list of qunit tests - element '#qunit-tests' was not found.)
    /// </summary>
    [Serializable]
    public class InvalidFileException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InvalidFileException class
        /// </summary>
        public InvalidFileException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidFileException class with a specified error message.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public InvalidFileException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidFileException class with a specified
        /// error message and a reference to the inner exception that is the cause of
        /// this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="inner">The exception that is the cause of the current exception. If the innerException
        /// parameter is not a null reference, the current exception is raised in a catch
        /// block that handles the inner exception.
        /// </param>
        public InvalidFileException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the InvalidFileException class with serialized data.
        /// </summary>
        /// <param name="info">The object that holds the serialized object data.</param>
        /// <param name="context"> The contextual information about the source or destination.</param>
        protected InvalidFileException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}