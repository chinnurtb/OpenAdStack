//-----------------------------------------------------------------------
// <copyright file="RsaPadding.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
