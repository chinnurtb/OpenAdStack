//-----------------------------------------------------------------------
// <copyright file="PemKeyContainer.cs" company="Rare Crowds Inc">
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

namespace Utilities.Cryptography.OpenSsl
{
    /// <summary>Loads PEM keys</summary>
    /// <remarks>
    /// This is a wrapper for functionality provided by the OpenSSL .Net managed interop and OpenSSL.
    /// For license details, see $\trunk\External\openssl-net-0.5\LICENSE
    /// </remarks>
    internal class PemKeyContainer : IPemKeyContainer
    {
        /// <summary>Gets the contained key</summary>
        public object Key
        {
            get { return this.Pem; }
        }

        /// <summary>Gets the PEM encoded key</summary>
        internal string Pem { get; private set; }

        /// <summary>Gets the password for encrypted keys</summary>
        internal string Password { get; private set; }

        /// <summary>Gets a value indicating whether the contained key is a private key</summary>
        internal bool IsPrivate
        {
            get { return this.Pem.Contains(" PRIVATE KEY"); }
        }

        /// <summary>Gets a value indicating whether the contained key is encrypted</summary>
        internal bool IsEncrypted
        {
            get { return this.Pem.Contains("ENCRYPTED"); }
        }
        
        /// <summary>Reads the PEM encoded key from the provided string.</summary>
        /// <remarks>
        /// The provided string should only contain a single object (encrypted
        /// private key, certificate, etc). If it contains multiple objects
        /// only the first object will be read.
        /// </remarks>
        /// <param name="pem">PEM encoded object to read</param>
        public void ReadPem(string pem)
        {
            this.Pem = pem.Replace("\\n", "\n");
        }

        /// <summary>Reads the PEM encoded key from the provided string.</summary>
        /// <remarks>
        /// The provided string should only contain a single object (encrypted
        /// private key, certificate, etc). If it contains multiple objects
        /// only the first object will be read.
        /// </remarks>
        /// <param name="pem">Encrypted PEM encoded object to read</param>
        /// <param name="password">Password to decrypt the PEM object</param>
        public void ReadEncryptedPem(string pem, string password)
        {
            this.Pem = pem.Replace("\\n", "\n");
            this.Password = password;
        }
    }
}
