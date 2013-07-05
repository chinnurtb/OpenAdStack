//-----------------------------------------------------------------------
// <copyright file="OpenSslCryptoProvider.cs" company="Rare Crowds Inc">
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

namespace Utilities.Cryptography.OpenSsl
{
    /// <summary>Provides cryptographic functionality implemented using Bouncy Castle</summary>
    /// <remarks>
    /// This is a wrapper for functionality provided by the OpenSSL .Net managed interop and OpenSSL.
    /// For license details, see $\trunk\External\openssl-net-0.5\LICENSE
    /// </remarks>
    public class OpenSslCryptoProvider : ICryptoProvider
    {
        /// <summary>Gets an ICipherEngine implementation</summary>
        /// <param name="algorithm">Name of the algorithm</param>
        /// <returns>Cipher engine for the algorithm</returns>
        public ICipherEngine CreateCipherEngine(string algorithm)
        {
            switch (algorithm.ToUpperInvariant())
            {
                case "RSA": return new RSAEngine();
                default:
                    throw new ArgumentException(
                        "Unsupported algorithm: {0}".FormatInvariant(algorithm),
                        "algorithm");
            }
        }

        /// <summary>Gets a container for PEM encoded keys</summary>
        /// <returns>Container with the key</returns>
        public IPemKeyContainer CreatePemKeyContainer()
        {
            return new PemKeyContainer();
        }
    }
}
