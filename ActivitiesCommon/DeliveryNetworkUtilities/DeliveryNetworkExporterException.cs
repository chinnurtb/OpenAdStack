//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkExporterException.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace DeliveryNetworkUtilities
{
    /// <summary>Exception for delivery network exporter errors</summary>
    [SuppressMessage("Microsoft.Design", "CA1032", Justification = "The only constructors that should be used are the ones included here")]
    [Serializable]
    public sealed class DeliveryNetworkExporterException : Exception
    {
        /// <summary>Initializes a new instance of the DeliveryNetworkExporterException class.</summary>
        /// <param name="innerException">The cause of the error.</param>
        public DeliveryNetworkExporterException(Exception innerException)
            : this(innerException, null)
        {
        }

        /// <summary>Initializes a new instance of the DeliveryNetworkExporterException class.</summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="args">Arguments for the message.</param>
        public DeliveryNetworkExporterException(string message, params object[] args)
            : this(null, message, args)
        {
        }

        /// <summary>Initializes a new instance of the DeliveryNetworkExporterException class.</summary>
        /// <param name="innerException">The cause of the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="args">Arguments for the message.</param>
        public DeliveryNetworkExporterException(Exception innerException, string message, params object[] args)
            : base(message.FormatInvariant(args), innerException)
        {
        }

        /// <summary>
        /// Sets the SerializationInfo with information about the exception.
        /// </summary>
        /// <param name="info">
        /// SerializationInfo that holds the serialized object data about the exception being thrown
        /// </param>
        /// <param name="context">
        /// StreamingContext that contains contextual information about the source or destination.
        /// </param>
        /// <exception cref="System.ArgumentNullException">
        /// The info parameter is a null reference.
        /// </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}