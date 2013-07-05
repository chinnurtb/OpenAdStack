//-----------------------------------------------------------------------
// <copyright file="IOpenSslInterop.cs" company="Rare Crowds Inc">
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
using System.Runtime.InteropServices;

namespace Utilities.Cryptography.OpenSsl.Native
{
    /// <summary>Callback used to get the password for encrypted PEM structures</summary>
    /// <param name="buf">Output buffer</param>
    /// <param name="size">Buffer size</param>
    /// <param name="rwflag">0 = reading (input), 1 = writing (verification)</param>
    /// <param name="userdata">User data (passed to the PEM routine)</param>
    /// <returns>If successful, the length of the password; otherwise 0.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int PemPasswordCallback(IntPtr buf, int size, int rwflag, IntPtr userdata);
    
    /// <summary>Interface for OpenSSL native interop</summary>
    internal interface IOpenSslInterop
    {
        /// <summary>Calls BIO_new_mem_buf</summary>
        /// <param name="buf">Bytes to initialize the new BIO with</param>
        /// <param name="len">Length of the bytes to use</param>
        /// <returns>The created BIO</returns>
        IntPtr BioNewMemBuf(byte[] buf, int len);

        /// <summary>Calls BIO_free</summary>
        /// <param name="bio">The BIO to free</param>
        void BioFree(IntPtr bio);

        /// <summary>Calls PEM_read_bio_RSA_PUBKEY</summary>
        /// <param name="bp">BIO to read</param>
        /// <param name="x">Output buffer, or null</param>
        /// <param name="cb">Callback used to retrieve pass phrase for encrypted PEM structures</param>
        /// <param name="u">If cb is not null, the parameter passed to the callback; otherwise, the pass phrase</param>
        /// <returns>Pointer to the RSA instance</returns>
        IntPtr PemReadBioRsaPubKey(IntPtr bp, IntPtr x, PemPasswordCallback cb, IntPtr u);

        /// <summary>Calls PEM_read_bio_RSAPrivateKey</summary>
        /// <param name="bp">BIO to read</param>
        /// <param name="x">Output buffer, or null</param>
        /// <param name="cb">Callback used to retrieve pass phrase for encrypted PEM structures</param>
        /// <param name="u">If cb is not null, the parameter passed to the callback; otherwise, the pass phrase</param>
        /// <returns>Pointer to the RSA instance</returns>
        IntPtr PemReadBioRsaPrivateKey(IntPtr bp, IntPtr x, PemPasswordCallback cb, IntPtr u);

        /// <summary>Calls RSA_public_encrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (ciphertext output) buffer</param>
        /// <param name="rsa">Public key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the encrypted data</returns>
        int RsaPublicEncrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding);

        /// <summary>Calls RSA_private_encrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (ciphertext output) buffer</param>
        /// <param name="rsa">Private key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the encrypted data</returns>
        int RsaPrivateEncrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding);

        /// <summary>Calls RSA_public_decrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (plaintext output) buffer</param>
        /// <param name="rsa">Public key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the recovered plaintext</returns>
        int RsaPublicDecrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding);

        /// <summary>Calls RSA_private_decrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (plaintext output) buffer</param>
        /// <param name="rsa">Private key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the recovered plaintext</returns>
        int RsaPrivateDecrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding);

        /// <summary>Gets the RSA modulus size</summary>
        /// <param name="rsa">The RSA instance</param>
        /// <returns>The RSA modulus size</returns>
        int RsaSize(IntPtr rsa);

        /// <summary>Calls RSA_free</summary>
        /// <param name="rsa">RSA instance to free</param>
        void RsaFree(IntPtr rsa);
    }

    /// <summary>Extensions for INativeInterop</summary>
    internal static class NativeInteropExtensions
    {
        /// <summary>Check that the result is not null</summary>
        /// <param name="this">The native interop</param>
        /// <param name="result">The result to check</param>
        /// <returns>The result</returns>
        /// <exception cref="System.NullReferenceException">Thrown if the result is null</exception>
        [SuppressMessage("Microsoft.Performance", "CA1801", Justification = "Extension requires target type")]
        public static IntPtr CheckResultNonNull(this IOpenSslInterop @this, IntPtr result)
        {
            if (result == IntPtr.Zero)
            {
                throw new ArgumentNullException("result", "The result was null");
            }
            
            return result;
        }

        /// <summary>Checks that the result was a success</summary>
        /// <param name="this">The native interop</param>
        /// <param name="result">The result to check</param>
        /// <returns>The result</returns>
        /// <exception cref="System.Exception">Thrown if the result is not success</exception>
        [SuppressMessage("Microsoft.Performance", "CA1801", Justification = "Extension requires target type")]
        public static int CheckResultSuccess(this IOpenSslInterop @this, int result)
        {
            if (result <= 0)
            {
                throw new ArgumentException("The result was unsuccessful", "result");
            }

            return result;
        }
    }
}
