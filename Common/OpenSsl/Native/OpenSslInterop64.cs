//-----------------------------------------------------------------------
// <copyright file="OpenSslInterop64.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Utilities.Cryptography.OpenSsl.Native
{
    /// <summary>Interop for the 64-bit OpenSSL native libraries</summary>
    [SuppressMessage("Microsoft.Performance", "CA1812", Justification = "Instantiated via NativeWrapper")]
    internal class OpenSslInterop64 : IOpenSslInterop
    {
        /// <summary>Name of the native DLL to bind to</summary>
        private const string DLLNAME = "libeay64";

        /// <summary>Calls BIO_new_mem_buf</summary>
        /// <param name="buf">Bytes to initialize the new BIO with</param>
        /// <param name="len">Length of the bytes to use</param>
        /// <returns>The created BIO</returns>
        public IntPtr BioNewMemBuf(byte[] buf, int len)
        {
            return NativeMethods.BIO_new_mem_buf(buf, len);
        }

        /// <summary>Calls BIO_free</summary>
        /// <param name="bio">The BIO to free</param>
        public void BioFree(IntPtr bio)
        {
            NativeMethods.BIO_free(bio);
        }
        
        /// <summary>Calls PEM_read_bio_RSA_PUBKEY</summary>
        /// <param name="bp">BIO to read</param>
        /// <param name="x">Output buffer, or null</param>
        /// <param name="cb">Callback used to retrieve pass phrase for encrypted PEM structures</param>
        /// <param name="u">If cb is not null, the parameter passed to the callback; otherwise, the pass phrase</param>
        /// <returns>Pointer to the RSA instance</returns>
        public IntPtr PemReadBioRsaPubKey(IntPtr bp, IntPtr x, PemPasswordCallback cb, IntPtr u)
        {
            return NativeMethods.PEM_read_bio_RSA_PUBKEY(bp, x, cb, u);
        }

        /// <summary>Calls PEM_read_bio_RSAPrivateKey</summary>
        /// <param name="bp">BIO to read</param>
        /// <param name="x">Output buffer, or null</param>
        /// <param name="cb">Callback used to retrieve pass phrase for encrypted PEM structures</param>
        /// <param name="u">If cb is not null, the parameter passed to the callback; otherwise, the pass phrase</param>
        /// <returns>Pointer to the RSA instance</returns>
        public IntPtr PemReadBioRsaPrivateKey(IntPtr bp, IntPtr x, PemPasswordCallback cb, IntPtr u)
        {
            return NativeMethods.PEM_read_bio_RSAPrivateKey(bp, x, cb, u);
        }

        /// <summary>Calls RSA_public_encrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (ciphertext output) buffer</param>
        /// <param name="rsa">Public key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the encrypted data</returns>
        public int RsaPublicEncrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding)
        {
            return NativeMethods.RSA_public_encrypt(flen, from, to, rsa, (int)padding);
        }

        /// <summary>Calls RSA_private_encrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (ciphertext output) buffer</param>
        /// <param name="rsa">Private key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the encrypted data</returns>
        public int RsaPrivateEncrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding)
        {
            return NativeMethods.RSA_private_encrypt(flen, from, to, rsa, (int)padding);
        }

        /// <summary>Calls RSA_public_decrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (plaintext output) buffer</param>
        /// <param name="rsa">Public key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the recovered plaintext</returns>
        public int RsaPublicDecrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding)
        {
            return NativeMethods.RSA_public_decrypt(flen, from, to, rsa, (int)padding);
        }

        /// <summary>Calls RSA_private_decrypt</summary>
        /// <param name="flen">From length</param>
        /// <param name="from">From (input) buffer</param>
        /// <param name="to">To (plaintext output) buffer</param>
        /// <param name="rsa">Private key RSA</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>Size of the recovered plaintext</returns>
        public int RsaPrivateDecrypt(int flen, byte[] from, byte[] to, IntPtr rsa, RsaPadding padding)
        {
            return NativeMethods.RSA_private_decrypt(flen, from, to, rsa, (int)padding);
        }

        /// <summary>Gets the RSA modulus size</summary>
        /// <param name="rsa">The RSA instance</param>
        /// <returns>The RSA modulus size</returns>
        public int RsaSize(IntPtr rsa)
        {
            return NativeMethods.RSA_size(rsa);
        }

        /// <summary>Calls RSA_free</summary>
        /// <param name="rsa">RSA instance to free</param>
        public void RsaFree(IntPtr rsa)
        {
            NativeMethods.RSA_free(rsa);
        }

        /// <summary>Native method P/Invoke bindings</summary>
        private static class NativeMethods
        {
            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr BIO_new_mem_buf(byte[] buf, int len);
            
            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void BIO_free(IntPtr bio);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr PEM_read_bio_RSA_PUBKEY(IntPtr bp, IntPtr x, PemPasswordCallback cb, IntPtr u);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr PEM_read_bio_RSAPrivateKey(IntPtr bp, IntPtr x, PemPasswordCallback cb, IntPtr u);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern int RSA_public_encrypt(int flen, byte[] from, byte[] to, IntPtr rsa, int padding);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern int RSA_private_encrypt(int flen, byte[] from, byte[] to, IntPtr rsa, int padding);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern int RSA_public_decrypt(int flen, byte[] from, byte[] to, IntPtr rsa, int padding);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern int RSA_private_decrypt(int flen, byte[] from, byte[] to, IntPtr rsa, int padding);

            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern int RSA_size(IntPtr rsa);
            
            [DllImport(DLLNAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void RSA_free(IntPtr rsa);
        }
    }
}
