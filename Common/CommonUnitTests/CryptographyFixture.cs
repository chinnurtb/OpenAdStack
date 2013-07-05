//-----------------------------------------------------------------------
// <copyright file="CryptographyFixture.cs" company="Rare Crowds Inc">
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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Cryptography;
using Utilities.Cryptography.OpenSsl;

namespace CommonUnitTests
{
    /// <summary>Tests for cryptography</summary>
    [TestClass]
    public class CryptographyFixture
    {
        /// <summary>Public key PEM</summary>
        private const string PublicKeyPem =
@"-----BEGIN PUBLIC KEY-----
MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAJ2nXowX8fNVkmhqZr2OUc+yCncisjfc
MFKwcqMXuRxQ0LLb6nKt3fqiT8ZNkfzQHOByJYGHgqTyoZtIyycNbdMCAwEAAQ==
-----END PUBLIC KEY-----";

        /// <summary>Encrypted private key PEM</summary>
        private const string EncryptedPrivateKeyPem =
@"-----BEGIN ENCRYPTED PRIVATE KEY-----\nMIIBpjBABgkqhkiG9w0BBQ0wMzAbBgkqhkiG9w0BBQwwDgQI45MdQLaQuBACAggA\nMBQGCCqGSIb3DQMHBAjvFU2nzngc5gSCAWASmAhrhpmtTxhiIv6d5D10+JCpqkQv\nk+MsM6YHX2jx3JOwGbbdynO2OjUYExqKBpecCNHR89UGuPascWw6t6SRBO7hq2wp\nCIH9jQCYbNhmHc1/UZb7xKjUHeunByFf/ZkUgc9BNpeWzGO8DktfQ8HTmR29DQkk\nLukLLwEDvr7cF1f74s7V1HyUHfdb0VD03C0amvlU61vPVwpwIb2uq0lvE2Ul9atk\n5nLRJaGuShvweLX/5Tfh9UJb/5cwYfo5cgSsk0NI7MAWOSlOjAP6uxgAIb4SNUpq\nQcXOY0gT1XZUE8IT3f6kxNDlJdlaaAmXh6aolyumyEblOZwB7t3Zc5iveIX84XXE\nJV1DxYNXJZPDygsQzHZMHJRCG7hMYv6VeTWCjn4TAbmqQNecDCtRQj4QnpI6BUhG\nDqOHRg+ieq26Mp2rBK6XCNRcNHQ+dwHqJ8wAVsLSrRAvA3Rsz8B5/ga8\n-----END ENCRYPTED PRIVATE KEY-----";

        /// <summary>Private key PEM</summary>
        private const string PrivateKeyPem =
@"-----BEGIN PRIVATE KEY-----
MIIBVgIBADANBgkqhkiG9w0BAQEFAASCAUAwggE8AgEAAkEAnadejBfx81WSaGpm
vY5Rz7IKdyKyN9wwUrByoxe5HFDQstvqcq3d+qJPxk2R/NAc4HIlgYeCpPKhm0jL
Jw1t0wIDAQABAkEAjnGA3bds5t10UV+BwNdsV+qXxhjVSd9q0euXSIDQwiFfsRHb
MMC718ek2SfiRHwn8TaNx+GDUQwM0/0qUK154QIhAM1QCFfIieU4AldlxR+WjLox
3Bzj+kJe6M/meDSmGJjpAiEAxJM/2DTklLVLRiZbALiRg6vnSkafMjzbwCdN9CWq
m1sCIFBrFrl7nTehVplxDWMwDvMncHYIfg/dKQe12EOXA29xAiEAm8SDJvRi3WP7
zg6+tgeLZ2dk0/q6U7jd+Zorr3fZhVkCIQCCkOHn11oGvTbSoQfDgyRJnfVjX9ri
x+9cbbohVIG3eg==
-----END PRIVATE KEY-----";

        /// <summary>Verify signing is equivalent to PHP's openssl_private_encrypt</summary>
        [TestMethod]
        public void OpenSslSignatureTest()
        {
            const string ExpectedSignature = "IKcPbY4CeVlK9Z/+QZKGoHEVID0nuGjMJEHpTS2K40ZZX++fShLJlISd3cmfRKATiQ2gHU0SJA3Gu2VqNATACg==";
            var rawSignature = "1354734731|3275";
            var rawSignatureBytes = Encoding.UTF8.GetBytes(rawSignature);

            var provider = new OpenSslCryptoProvider();

            var privateKey = provider.CreatePemKeyContainer();
            privateKey.ReadPem(PrivateKeyPem);

            var rsa = provider.CreateCipherEngine("RSA");
            rsa.KeyContainer = privateKey;
            var signatureBytes = rsa.Encrypt(rawSignatureBytes);
            var signature = Convert.ToBase64String(signatureBytes);

            var expectedSignatureBytes = Convert.FromBase64String(ExpectedSignature);
            Assert.AreEqual(expectedSignatureBytes.Length, signatureBytes.Length);

            Assert.AreEqual(ExpectedSignature, signature);
        }

        /// <summary>Verify signing is equivalent to PHP's openssl_private_encrypt</summary>
        /// <remarks>TODO: Make this work</remarks>
        [TestMethod]
        [Ignore]
        public void OpenSslSignatureWithEncryptedKeyTest()
        {
            const string ExpectedSignature = "IKcPbY4CeVlK9Z/+QZKGoHEVID0nuGjMJEHpTS2K40ZZX++fShLJlISd3cmfRKATiQ2gHU0SJA3Gu2VqNATACg==";
            var rawSignature = "1354734731|3275";
            var rawSignatureBytes = Encoding.UTF8.GetBytes(rawSignature);

            var provider = new OpenSslCryptoProvider();

            var privateKey = provider.CreatePemKeyContainer();
            privateKey.ReadEncryptedPem(EncryptedPrivateKeyPem, "RCAPNXAPP!");

            var rsa = provider.CreateCipherEngine("RSA");
            rsa.KeyContainer = privateKey;
            var signatureBytes = rsa.Encrypt(rawSignatureBytes);
            var signature = Convert.ToBase64String(signatureBytes);

            var expectedSignatureBytes = Convert.FromBase64String(ExpectedSignature);
            Assert.AreEqual(expectedSignatureBytes.Length, signatureBytes.Length);

            Assert.AreEqual(ExpectedSignature, signature);
        }
    }
}
