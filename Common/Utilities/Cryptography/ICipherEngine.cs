//-----------------------------------------------------------------------
// <copyright file="ICipherEngine.cs" company="Rare Crowds Inc">
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
