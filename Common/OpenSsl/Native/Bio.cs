//-----------------------------------------------------------------------
// <copyright file="Bio.cs" company="Rare Crowds Inc">
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
using System.Runtime.InteropServices;
using System.Text;
using Utilities.Runtime.InteropServices;

namespace Utilities.Cryptography.OpenSsl.Native
{
    /// <summary>Wrapper for native OpenSSL buffered IO</summary>
    internal class Bio : OpenSslNativeWrapper
    {
        /// <summary>Initializes a new instance of the Bio class</summary>
        /// <param name="value">Bytes to be copied to the BIO memory buffer</param>
        public Bio(byte[] value)
            : base(Native.BioNewMemBuf(value, value.Length))
        {
        }

        /// <summary>Initializes a new instance of the Bio class</summary>
        /// <remarks>Creates a BIO containing the provided text in the specified encoding</remarks>
        /// <param name="text">Text to be copied to BIO memory buffer</param>
        /// <param name="encoding">The encoding to be used</param>
        public Bio(string text, Encoding encoding)
            : this(encoding.GetBytes(text))
        {
        }

        /// <summary>Initializes a new instance of the Bio class</summary>
        /// <remarks>Creates a BIO containing the provided text in ASCII encoding</remarks>
        /// <param name="text">Text to be copied to BIO memory buffer</param>
        public Bio(string text)
            : this(text, Encoding.ASCII)
        {
        }

        /// <summary>Calls BIO_free</summary>
        protected override void OnDispose()
        {
            Native.BioFree(this.Handle);
        }
    }
}
