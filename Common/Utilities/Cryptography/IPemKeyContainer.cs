//-----------------------------------------------------------------------
// <copyright file="IPemKeyContainer.cs" company="Rare Crowds Inc">
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
