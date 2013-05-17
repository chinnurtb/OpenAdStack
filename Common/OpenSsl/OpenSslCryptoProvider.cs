//-----------------------------------------------------------------------
// <copyright file="OpenSslCryptoProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
