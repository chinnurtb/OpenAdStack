//-----------------------------------------------------------------------
// <copyright file="InvalidETagException.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Utilities.Storage
{
    /// <summary>
    /// Exception thrown when a persistent dictionary entry's eTag is invalid
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032", Justification = "The only constructors that should be used are the ones included here")]
    [Serializable]
    public class InvalidETagException : InvalidOperationException
    {
        /// <summary>Format for invalid ETag exception messages</summary>
        private const string MessageFormat = "The ETag ({0}) for entry '{1}' of '{2}' is not valid.";

        /// <summary>
        /// Initializes a new instance of the InvalidETagException class
        /// </summary>
        /// <param name="storeName">The store name</param>
        /// <param name="key">The entry key</param>
        /// <param name="etag">The invalid eTag</param>
        public InvalidETagException(string storeName, string key, string etag)
            : this(storeName, key, etag, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidETagException class
        /// </summary>
        /// <param name="storeName">The store name</param>
        /// <param name="key">The entry key</param>
        /// <param name="etag">The invalid eTag</param>
        /// <param name="innerException">The inner exception</param>
        public InvalidETagException(string storeName, string key, string etag, Exception innerException)
            : base(MessageFormat.FormatInvariant(etag, key, storeName), innerException)
        {
            this.StoreName = storeName;
            this.Key = key;
            this.InvalidETag = etag;
        }

        /// <summary>Gets the dictionary store name</summary>
        public string StoreName { get; private set; }

        /// <summary>Gets the entry key</summary>
        public string Key { get; private set; }

        /// <summary>Gets the invalid eTag</summary>
        public string InvalidETag { get; private set; }

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
