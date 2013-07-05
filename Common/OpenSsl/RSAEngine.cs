//-----------------------------------------------------------------------
// <copyright file="RSAEngine.cs" company="Rare Crowds Inc">
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
using Utilities.Cryptography.OpenSsl.Native;

namespace Utilities.Cryptography.OpenSsl
{
    /// <summary>Interface for cipher engines</summary>
    /// <remarks>
    /// This is a wrapper for functionality provided by the OpenSSL .Net managed interop and OpenSSL.
    /// For license details, see $\trunk\External\openssl-net-0.5\LICENSE
    /// </remarks>
    internal class RSAEngine : ICipherEngine
    {
        /// <summary>Gets the name of the cipher algorithm</summary>
        public string Algorithm
        {
            get { return "RSA"; }
        }

        /// <summary>Gets or sets the key container</summary>
        public IKeyContainer KeyContainer { get; set; }

        /// <summary>Gets the key container as its underlying type</summary>
        private PemKeyContainer PemKeyContainer
        {
            get { return this.KeyContainer as PemKeyContainer; }
        }

        /// <summary>Encrypts the provided bytes</summary>
        /// <param name="value">Bytes to encrypt</param>
        /// <returns>Encrypted bytes</returns>
        public byte[] Encrypt(byte[] value)
        {
            using (var rsa =
                this.PemKeyContainer.IsPrivate ?
                    (this.PemKeyContainer.IsEncrypted ?
                        Rsa.FromPrivateKey(this.PemKeyContainer.Pem, this.PemKeyContainer.Password) :
                        Rsa.FromPrivateKey(this.PemKeyContainer.Pem)) :
                    (this.PemKeyContainer.IsEncrypted ?
                        Rsa.FromPublicKey(this.PemKeyContainer.Pem, this.PemKeyContainer.Password) :
                        Rsa.FromPublicKey(this.PemKeyContainer.Pem)))
            {
                return this.PemKeyContainer.IsPrivate ?
                    rsa.PrivateEncrypt(value, RsaPadding.PKCS1) :
                    rsa.PublicEncrypt(value, RsaPadding.PKCS1);
            }
        }

        /// <summary>Decrypts the provided bytes</summary>
        /// <param name="value">Bytes to decrypt</param>
        /// <returns>Decrypted bytes</returns>
        public byte[] Decrypt(byte[] value)
        {
            using (var rsa =
                this.PemKeyContainer.IsPrivate ?
                    (this.PemKeyContainer.IsEncrypted ?
                        Rsa.FromPrivateKey(this.PemKeyContainer.Pem, this.PemKeyContainer.Password) :
                        Rsa.FromPrivateKey(this.PemKeyContainer.Pem)) :
                    (this.PemKeyContainer.IsEncrypted ?
                        Rsa.FromPublicKey(this.PemKeyContainer.Pem, this.PemKeyContainer.Password) :
                        Rsa.FromPublicKey(this.PemKeyContainer.Pem)))
            {
                return this.PemKeyContainer.IsPrivate ?
                    rsa.PrivateDecrypt(value, RsaPadding.PKCS1) :
                    rsa.PublicDecrypt(value, RsaPadding.PKCS1);
            }
        }
    }
}
