//-----------------------------------------------------------------------
// <copyright file="ICipherEngine.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.IO;

namespace Utilities.Cryptography
{
    /// <summary>Interface for cipher engines</summary>
    public interface ICipherEngine
    {
        /// <summary>Gets the name of the cipher algorithm</summary>
        string Algorithm { get; }

        /// <summary>Gets or sets the key container</summary>
        IKeyContainer KeyContainer { get; set; }

        /// <summary>Encrypts the provided bytes</summary>
        /// <param name="value">Bytes to encrypt</param>
        /// <returns>Encrypted bytes</returns>
        byte[] Encrypt(byte[] value);

        /// <summary>Decrypts the provided bytes</summary>
        /// <param name="value">Bytes to decrypt</param>
        /// <returns>Decrypted bytes</returns>
        byte[] Decrypt(byte[] value);
    }
}
