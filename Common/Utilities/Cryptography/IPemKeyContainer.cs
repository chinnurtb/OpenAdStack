//-----------------------------------------------------------------------
// <copyright file="IPemKeyContainer.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Cryptography
{
    /// <summary>Interface for key containers used with ICipherEngine</summary>
    public interface IPemKeyContainer : IKeyContainer
    {
        /// <summary>Reads the PEM encoded key from the provided string.</summary>
        /// <remarks>
        /// The provided string should only contain a single object (encrypted
        /// private key, certificate, etc). If it contains multiple objects
        /// only the first object will be read.
        /// </remarks>
        /// <param name="pem">PEM encoded object to read</param>
        void ReadPem(string pem);

        /// <summary>Reads the PEM encoded key from the provided string.</summary>
        /// <remarks>
        /// The provided string should only contain a single object (encrypted
        /// private key, certificate, etc). If it contains multiple objects
        /// only the first object will be read.
        /// </remarks>
        /// <param name="pem">Encrypted PEM encoded object to read</param>
        /// <param name="password">Password to decrypt the PEM object</param>
        void ReadEncryptedPem(string pem, string password);
    }
}
