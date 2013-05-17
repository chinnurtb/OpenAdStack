//-----------------------------------------------------------------------
// <copyright file="ICryptoProvider.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Cryptography
{
    /// <summary>Interface for cryptographic functionality providers</summary>
    public interface ICryptoProvider
    {
        /// <summary>Gets an ICipherEngine implementation</summary>
        /// <param name="algorithm">Name of the algorithm</param>
        /// <returns>Cipher engine for the algorithm</returns>
        ICipherEngine CreateCipherEngine(string algorithm);

        /// <summary>Gets a container for PEM encoded keys</summary>
        /// <returns>Container with the key</returns>
        IPemKeyContainer CreatePemKeyContainer();
    }
}
