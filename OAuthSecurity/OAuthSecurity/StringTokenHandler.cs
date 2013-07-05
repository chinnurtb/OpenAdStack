// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StringTokenHandler.cs" company="Rare Crowds Inc">
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IdentityModel.Tokens;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Tokens;
using OAuthSecurity.Properties;

namespace OAuthSecurity
{
    /// <summary>
    /// Class that extends the SecurityTokenHandler class
    /// </summary>
    public abstract class StringTokenHandler : SessionSecurityTokenHandler
    {
        /// <summary>
        /// Initializes a new instance of the StringTokenHandler class
        /// </summary>
        protected StringTokenHandler()
            : base()
        {
        }

        /// <summary>
        /// Gets the CanWriteToken
        /// </summary>
        public override bool CanWriteToken
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets token lifetime as string
        /// </summary>
        public string Lifetime { get; set; }

        /// <summary>
        /// Gets the CanValidateToken
        /// </summary>
        public override bool CanValidateToken
        {
            get { return false; }
        }

        /// <summary>
        /// Method to determine whether token can be red
        /// </summary>
        /// <param name="reader">token reader</param>
        /// <returns>whether token can be read</returns>
        public override bool CanReadToken(XmlReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            return (reader.IsStartElement("BinarySecurityToken", Resources.BinarySecurityTokenNs) &&
                reader.GetAttribute("ValueType") == Resources.BinarySecurityTokenValueType) || reader.IsStartElement("stringToken");
        }

        /// <summary>
        /// Method to read the token
        /// </summary>
        /// <param name="reader">XmlReader to read the token</param>
        /// <returns>SecurityToken that was read</returns>
        public override SecurityToken ReadToken(XmlReader reader)
        {
            if (!this.CanReadToken(reader))
            {
                throw new InvalidOperationException(Resources.HandlerCannotReadToken);
            }

            string rawToken = string.Empty;
            bool base64 = false;
            string possibleBase64String = reader.ReadElementContentAsString();
            if (SecurityHelper.IsBase64(possibleBase64String))
            {
                base64 = true;
                var swtBuffer = Convert.FromBase64String(possibleBase64String);
                rawToken = Encoding.ASCII.GetString(swtBuffer);
            }
            else
            {
                rawToken = possibleBase64String;
            }

            const char ParameterSeparator = '&';

            string unsignedString = string.Empty;

            if (string.IsNullOrEmpty(rawToken))
            {
                throw new InvalidOperationException(Resources.RawTokenNullOrEmpty);
            }

            int lastSeparator = rawToken.LastIndexOf(ParameterSeparator);

            if (lastSeparator > 0)
            {
                string lastParamStart = ParameterSeparator + "HMACSHA256=";
                string lastParam = rawToken.Substring(lastSeparator);

                if (lastParam.StartsWith(lastParamStart, StringComparison.Ordinal))
                {
                    unsignedString = rawToken.Substring(0, lastSeparator);
                }
            }
            else
            {
                throw new InvalidOperationException(Resources.SwtMustHaveSignature);
            }

            if (string.IsNullOrWhiteSpace(unsignedString))
            {
                throw new InvalidOperationException(Resources.SwtMustHaveSignature);
            }

            NameValueCollection rawClaims = ParseToken(rawToken, base64);
            string audience = GetValueAndRemoveFromRawClaims("Audience", rawClaims, Resources.SwtNoAudienceUri);
            Uri audienceUri = new Uri(HttpUtility.UrlDecode(audience));
            string expires = GetValueAndRemoveFromRawClaims("ExpiresOn", rawClaims, Resources.SwtNoExpiryTime);
            string issuer = GetValueAndRemoveFromRawClaims("Issuer", rawClaims, Resources.SwtNoIssuer);
            issuer = HttpUtility.UrlDecode(issuer);
            string signature = GetValueAndRemoveFromRawClaims("HMACSHA256", rawClaims, Resources.SwtNoSignature);
            signature = HttpUtility.UrlDecode(signature);

            Collection<Claim> claims = DecodeClaims(issuer, rawClaims);

            return new SimpleWebToken(audienceUri, issuer, this.DecodeExpiry(expires), claims, signature, unsignedString, rawToken);
        }

        /// <summary>
        /// Method to write the token
        /// </summary>
        /// <param name="writer">XmlWriter for token</param>
        /// <param name="token">token to be written</param>
        public override void WriteToken(XmlWriter writer, SecurityToken token)
        {
            var simpleWebToken = token as SimpleWebToken;

            if (simpleWebToken == null)
            {
                throw new InvalidOperationException(Resources.SwtWrongType);
            }

            if (IsBinaryEncoded(simpleWebToken))
            {
                WrapInsideBinarySecurityToken(simpleWebToken.UnsignedString).WriteTo(writer);
            }
            else
            {
                writer.WriteStartElement("stringToken");
                writer.WriteString(simpleWebToken.SignedString);
                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Method to determine whether the token is binary encoded
        /// </summary>
        /// <param name="token">token to be evaluated</param>
        /// <returns>whether token is binary encoded</returns>
        protected static bool IsBinaryEncoded(SimpleWebToken token)
        {
            if (token == null)
            {
                throw new ArgumentNullException("token");
            }

            XmlTextReader reader = new XmlTextReader(token.UnsignedString);

            return reader.IsStartElement("BinarySecurityToken", Resources.BinarySecurityTokenNs) &&
                reader.GetAttribute("ValueType") == Resources.BinarySecurityTokenValueType;
        }

        /// <summary>Create <see cref="Claim"/> from the incoming token.
        /// </summary>
        /// <param name="issuer">The issuer of the token.</param>
        /// <param name="rawClaims">The name value pairs from the token.</param>        
        /// <returns>A list of Claims created from the token.</returns>
        protected static Collection<Claim> DecodeClaims(string issuer, NameValueCollection rawClaims)
        {
            if (rawClaims == null)
            {
                throw new ArgumentNullException("rawClaims");
            }

            Collection<Claim> decodedClaims = new Collection<Claim>();

            foreach (string key in rawClaims.Keys)
            {
                if (string.IsNullOrEmpty(rawClaims[key]))
                {
                    throw new InvalidOperationException(Resources.ClaimValueEmpty);
                }

                decodedClaims.Add(new Claim(key, rawClaims[key], ClaimValueTypes.String, issuer));
                if (key == Resources.AcsNameClaimType)
                {
                    // add a default name claim from the Name identifier claim.
                    decodedClaims.Add(new Claim(Resources.DefaultNameClaimType, rawClaims[key], ClaimValueTypes.String, issuer));
                }
            }

            return decodedClaims;
        }

        /// <summary>
        /// Convert the expiryTime to the <see cref="DateTime"/> format.
        /// </summary>
        /// <param name="expiry">The expiry time from the token.</param>
        /// <returns>The local expiry time of the token.</returns>
        protected DateTime DecodeExpiry(string expiry)
        {
            long lifetimeSeconds = 0;

            // get the value from LifeTime element, if it exists
            if (!long.TryParse(this.Lifetime, out lifetimeSeconds))
            {
                lifetimeSeconds = 0;
            }

            long totalSeconds = 0;
            if (!long.TryParse(expiry, out totalSeconds))
            {
                throw new InvalidOperationException(Resources.SwtUnexpectedTimeFormat);
            }

            // If lifetime > 0, then take the shorter of the two
            if (lifetimeSeconds > totalSeconds)
            {
                totalSeconds = lifetimeSeconds;
            }

            long maxSeconds = (long)(DateTime.MaxValue - SimpleWebToken.SimpleWebTokenBaseTime).TotalSeconds - 1;
            if (totalSeconds > maxSeconds)
            {
                totalSeconds = maxSeconds;
            }

            return SimpleWebToken.SimpleWebTokenBaseTime + TimeSpan.FromSeconds(totalSeconds);
        }

        /// <summary>
        /// Method to wrap inside binary security token
        /// </summary>
        /// <param name="accessToken">the access token</param>
        /// <returns>the wrapped xml element</returns>
        private static XElement WrapInsideBinarySecurityToken(string accessToken)
        {
            return new XElement(
                XNamespace.Get(Resources.BinarySecurityTokenNs).GetName("BinarySecurityToken"),
                new XAttribute("ValueType", Resources.BinarySecurityTokenValueType),
                new XAttribute("EncodingType", Resources.BinarySecurityTokenEncodingType),
                Convert.ToBase64String(Encoding.Default.GetBytes(accessToken)));
        }

        /// <summary>
        /// Method to get and remove value from NameValueCollection
        /// </summary>
        /// <param name="key">key to NameValueCollection</param>
        /// <param name="rawClaims">claims from token</param>
        /// <param name="errorMessage">error message in the event of error</param>
        /// <returns>the value from the NameValueCollection</returns>
        private static string GetValueAndRemoveFromRawClaims(string key, NameValueCollection rawClaims, string errorMessage)
        {
            string value = rawClaims[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(errorMessage);
            }

            rawClaims.Remove(key);
            return value;
        }

        /// <summary>
        /// Parses the token into a collection.
        /// </summary>
        /// <param name="encodedToken">The serialized token.</param>
        /// <param name="base64">whether the token is base64</param>
        /// <returns>A collection of all name-value pairs from the token.</returns>
        private static NameValueCollection ParseToken(string encodedToken, bool base64)
        {
            NameValueCollection claimCollection = new NameValueCollection();
            foreach (string nameValue in encodedToken.Split('&'))
            {
                string key = string.Empty;
                string value = string.Empty;
                int equalSignPosition = nameValue.IndexOf("=", StringComparison.OrdinalIgnoreCase);

                if (equalSignPosition <= 0)
                {
                    throw new InvalidOperationException(Resources.SwtIncorrectlyFormed);
                }

                key = nameValue.Substring(0, equalSignPosition).Trim();
                value = nameValue.Substring(equalSignPosition + 1).Trim().Trim('"');
                if (string.IsNullOrEmpty(value))
                {
                    // ignore parameter with empty values
                    continue;
                }

                if (base64)
                {
                    key = HttpUtility.UrlDecode(key);
                    value = HttpUtility.UrlDecode(value);
                }

                claimCollection.Add(key, value);
            }

            return claimCollection;
        }
    }
}
