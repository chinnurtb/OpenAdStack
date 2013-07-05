//-----------------------------------------------------------------------
// <copyright file="Rsa.cs" company="Rare Crowds Inc">
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
    /// <summary>Wrapper for native OpenSSL RSA functionality</summary>
    internal class Rsa : OpenSslNativeWrapper
    {
        /// <summary>Initializes a new instance of the Rsa class</summary>
        /// <param name="handle">Handle to the wrapped RSA resource</param>
        public Rsa(IntPtr handle)
            : base(handle)
        {
        }

        /// <summary>Gets the RSA modulus size</summary>
        public int Size
        {
            get { return Native.RsaSize(this.Handle); }
        }

        /// <summary>Calls PEM_read_bio_RSA_PUBKEY()</summary>
        /// <param name="publicKey">Public key</param>
        /// <returns>The RSA instance</returns>
        public static Rsa FromPublicKey(string publicKey)
        {
            return FromPublicKey(publicKey, null);
        }

        /// <summary>Calls PEM_read_bio_RSAPrivateKey()</summary>
        /// <param name="privateKey">Private key</param>
        /// <returns>The RSA instance</returns>
        public static Rsa FromPrivateKey(string privateKey)
        {
            return FromPrivateKey(privateKey, null);
        }

        /// <summary>Calls PEM_read_bio_RSA_PUBKEY()</summary>
        /// <param name="publicKey">Public key</param>
        /// <param name="passphrase">Passphrase for encrypted keys (or null)</param>
        /// <returns>The RSA instance</returns>
        public static Rsa FromPublicKey(string publicKey, string passphrase)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentNullException("publicKey");
            }

            using (var publicKeyBio = new Bio(publicKey))
            {
                var password = new Password(passphrase);
                var ptr = Native.PemReadBioRsaPubKey(
                    publicKeyBio.Handle,
                    IntPtr.Zero,
                    password.Callback,
                    IntPtr.Zero);
                return new Rsa(Native.CheckResultNonNull(ptr));
            }
        }

        /// <summary>Calls PEM_read_bio_RSAPrivateKey()</summary>
        /// <param name="privateKey">Private key</param>
        /// <param name="passphrase">Passphrase for encrypted keys (or null)</param>
        /// <returns>The RSA instance</returns>
        public static Rsa FromPrivateKey(string privateKey, string passphrase)
        {
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                throw new ArgumentNullException("privateKey");
            }

            using (var privateKeyBio = new Bio(privateKey, Encoding.ASCII))
            {
                var password = new Password(passphrase);
                var ptr = Native.PemReadBioRsaPrivateKey(
                    privateKeyBio.Handle,
                    IntPtr.Zero,
                    password.Callback,
                    IntPtr.Zero);
                return new Rsa(Native.CheckResultNonNull(ptr));
            }
        }

        /// <summary>Calls RSA_public_encrypt()</summary>
        /// <param name="value">Bytes to encrypt</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>The encrypted bytes</returns>
        public byte[] PublicEncrypt(byte[] value, RsaPadding padding)
        {
            var buffer = new byte[this.Size];
            var size = Native.CheckResultSuccess(Native.RsaPublicEncrypt(value.Length, value, buffer, this.Handle, padding));
            if (size != buffer.Length)
            {
                var padded = new byte[size];
                Buffer.BlockCopy(buffer, 0, padded, 0, size);
                return padded;
            }

            return buffer;
        }

        /// <summary>Calls RSA_private_encrypt()</summary>
        /// <param name="value">Bytes to encrypt</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>The encrypted bytes</returns>
        public byte[] PrivateEncrypt(byte[] value, RsaPadding padding)
        {
            var buffer = new byte[this.Size];
            var size = Native.CheckResultSuccess(Native.RsaPrivateEncrypt(value.Length, value, buffer, this.Handle, padding));
            if (size != buffer.Length)
            {
                var padded = new byte[size];
                Buffer.BlockCopy(buffer, 0, padded, 0, size);
                return padded;
            }

            return buffer;
        }

        /// <summary>Calls RSA_public_decrypt()</summary>
        /// <param name="value">Bytes to decrypt</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>The decrypted bytes</returns>
        public byte[] PublicDecrypt(byte[] value, RsaPadding padding)
        {
            var buffer = new byte[this.Size];
            int size = Native.CheckResultSuccess(Native.RsaPublicDecrypt(value.Length, value, buffer, this.Handle, padding));
            if (size != buffer.Length)
            {
                var padded = new byte[size];
                Buffer.BlockCopy(buffer, 0, padded, 0, size);
                return padded;
            }

            return buffer;
        }

        /// <summary>Calls RSA_private_decrypt()</summary>
        /// <param name="value">Bytes to decrypt</param>
        /// <param name="padding">Padding mode</param>
        /// <returns>The decrypted bytes</returns>
        public byte[] PrivateDecrypt(byte[] value, RsaPadding padding)
        {
            var buffer = new byte[this.Size];
            int size = Native.CheckResultSuccess(Native.RsaPrivateDecrypt(value.Length, value, buffer, this.Handle, padding));
            if (size != buffer.Length)
            {
                var padded = new byte[size];
                Buffer.BlockCopy(buffer, 0, padded, 0, size);
                return padded;
            }

            return buffer;
        }

        /// <summary>Calls RSA_free</summary>
        protected override void OnDispose()
        {
            Native.RsaFree(this.Handle);
        }

        /// <summary>Clear text implementation of the PEM password callback</summary>
        private class Password
        {
            /// <summary>The password bytes</summary>
            private byte[] password;

            /// <summary>Initializes a new instance of the Password class</summary>
            /// <param name="password">The password</param>
            public Password(string password)
            {
                this.password = password != null ? Encoding.ASCII.GetBytes(password) : null;
            }

            /// <summary>Gets the password callback</summary>
            public PemPasswordCallback Callback
            {
                get { return this.GetPassword; }
            }

            /// <summary>Copies the password bytes into the provided buffer</summary>
            /// <param name="buf">Buffer to copy the password into</param>
            /// <param name="size">Size of the buffer</param>
            /// <param name="rwflag">The parameter is not used.</param>
            /// <param name="userdata">The parameter is not used.</param>
            /// <returns>The length of the copied password</returns>
            private int GetPassword(IntPtr buf, int size, int rwflag, IntPtr userdata)
            {
                if (this.password == null)
                {
                    return 0;
                }

                var length = Math.Min(this.password.Length, size);
                Marshal.Copy(this.password, 0, buf, length);
                return length;
            }
        }
    }
}
