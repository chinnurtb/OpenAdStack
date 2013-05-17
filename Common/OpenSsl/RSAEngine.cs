//-----------------------------------------------------------------------
// <copyright file="RSAEngine.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
