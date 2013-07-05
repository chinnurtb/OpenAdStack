//-----------------------------------------------------------------------
// <copyright file="RsaPadding.cs" company="Rare Crowds Inc">
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

namespace Utilities.Cryptography.OpenSsl.Native
{
    /// <summary>RSA padding mode</summary>
    internal enum RsaPadding
    {
        /// <summary>RSA PKCS1 PADDING</summary>
        PKCS1 = 0x01,

        /// <summary>RSA SSLV23 PADDING</summary>
        SSLv23 = 0x02,

        /// <summary>RSA NO PADDING</summary>
        None = 0x03,

        /// <summary>RSA PKCS1 OAEP PADDING</summary>
        /// <remarks>Optimal Asymmetric Encryption Padding</remarks>
        OAEP = 0x04,

        /// <summary>RSA X931 PADDING</summary>
        X931 = 0x05,
    }
}
