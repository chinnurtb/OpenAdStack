//-----------------------------------------------------------------------
// <copyright file="Bio.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
