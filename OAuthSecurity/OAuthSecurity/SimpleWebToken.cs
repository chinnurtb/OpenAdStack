// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleWebToken.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.IdentityModel.Claims;

namespace OAuthSecurity
{
    /// <summary>
    /// Class to depict simple web token
    /// </summary>
    public class SimpleWebToken : SecurityToken
    {
        /// <summary>
        /// simple web token base time
        /// </summary>
        public static readonly DateTime SimpleWebTokenBaseTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        /// <summary>
        /// Id of token
        /// </summary>
        private string id;

        /// <summary>
        /// when token expires
        /// </summary>
        private DateTime expiresOn;

        /// <summary>
        /// token signature
        /// </summary>
        private string signature;

        /// <summary>
        /// token valid from
        /// </summary>
        private DateTime validFrom;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleWebToken"/> class.
        /// </summary>
        public SimpleWebToken()
        {
            this.validFrom = SimpleWebTokenBaseTime;
            this.id = null;
        }

        /// <summary>
        /// Initializes a new instance of the SimpleWebToken class.
        /// </summary>
        /// <param name="audienceUriParameter">The Audience Uri of the token.</param>
        /// <param name="issuerParameter">The issuer of the token.</param>
        /// <param name="expiresOnParameter">The expiry time of the token.</param>
        /// <param name="claimsParameter">The claims in the token.</param>
        /// <param name="signatureParameter">The signature of the token.</param>
        /// <param name="unsignedStringParameter">The serialized token without its signature.</param>
        /// <param name="signedStringParameter">The serialized token with its signature.</param>
        internal SimpleWebToken(Uri audienceUriParameter, string issuerParameter, DateTime expiresOnParameter, Collection<Claim> claimsParameter, string signatureParameter, string unsignedStringParameter, string signedStringParameter)
            : this()
        {
            this.AudienceUri = audienceUriParameter;
            this.Issuer = issuerParameter;
            this.expiresOn = expiresOnParameter;
            this.signature = signatureParameter;
            this.UnsignedString = unsignedStringParameter;
            this.Claims = claimsParameter;
            this.SignedString = signedStringParameter;
        }

        /// <summary>
        /// Gets the Id of the token.
        /// </summary>
        /// <value>The Id of the token.</value>
        public override string Id
        {
            get { return this.id; }
        }

        /// <summary>
        /// Gets the keys associated with this token.
        /// </summary>
        /// <value>The keys associated with this token.</value>
        public override ReadOnlyCollection<SecurityKey> SecurityKeys
        {
            get { return new ReadOnlyCollection<SecurityKey>(new List<SecurityKey>()); }
        }

        /// <summary>
        /// Gets the time from when the token is valid.
        /// </summary>
        /// <value>The time from when the token is valid.</value>
        public override DateTime ValidFrom
        {
            get { return this.validFrom; }
        }

        /// <summary>
        /// Gets the time when the token expires.
        /// </summary>
        /// <value>The time upto which the token is valid.</value>
        public override DateTime ValidTo
        {
            get { return this.expiresOn; }
        }

        /// <summary>
        /// Gets the AudienceUri for the token.
        /// </summary>
        /// <value>The audience Uri of the token.</value>
        public Uri AudienceUri
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Issuer for the token.
        /// </summary>
        /// <value>The issuer for the token.</value>
        public string Issuer
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the Claims in the token.
        /// </summary>
        /// <value>The Claims in the token.</value>
        public Collection<Claim> Claims
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the raw token before signing
        /// </summary>
        /// <value>The raw token before signing</value>
        public string UnsignedString
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the raw token
        /// </summary>
        /// <value>The raw token after signing</value>
        public string SignedString
        {
            get;
            private set;
        }

        /// <summary>
        /// Verifies the signature of the incoming token.
        /// </summary>
        /// <param name="key">The key used for signing.</param>
        /// <returns>true if the signatures match, false otherwise.</returns>
        public bool SignVerify(byte[] key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (this.signature == null || this.UnsignedString == null)
            {
                throw new InvalidOperationException("Token has never been signed");
            }

            string verifySignature;

            using (HMACSHA256 signatureAlgorithm = new HMACSHA256(key))
            {
                verifySignature = Convert.ToBase64String(signatureAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(this.UnsignedString)));
            }

            if (string.CompareOrdinal(HttpUtility.UrlDecode(verifySignature), HttpUtility.UrlDecode(this.signature)) == 0)
            {
                return true;
            }

            return false;
        }
    }
}
