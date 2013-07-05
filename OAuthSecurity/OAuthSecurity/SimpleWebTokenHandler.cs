// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleWebTokenHandler.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ConfigManager;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.SecurityTokenService;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Web;
using OAuthSecurity.Properties;

namespace OAuthSecurity
{
    /// <summary>
    /// Class to manage the simple web token security handler
    /// </summary>
    public class SimpleWebTokenHandler : StringTokenHandler
    {
        /// <summary>
        /// trusted issuer
        /// </summary>
        private static readonly string trustedIssuer = string.Format(CultureInfo.CurrentCulture, "https://{0}.{1}/", Config.GetValue("ACS.ServiceNamespace"), Config.GetValue("ACS.acsHostUrl"));

        /// <summary>
        /// symmetric signature key
        /// </summary>
        private static readonly string symmetricSignatureKey = Config.GetValue("ACS.SymmetricSignatureKey");

        /// <summary>
        /// Initializes a new instance of the SimpleWebTokenHandler class
        /// </summary>
        public SimpleWebTokenHandler()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SimpleWebTokenHandler class 
        /// </summary>
        /// <param name="customConfigElements">child elements in web.config </param>
        public SimpleWebTokenHandler(XmlNodeList customConfigElements)
            : base()
        {
            foreach (XmlNode customConfigElement in customConfigElements)
            {
                if (customConfigElement.InnerText == "sessionTokenRequirement")
                {
                    XmlNode lifetime = customConfigElement.Attributes.GetNamedItem("lifetime");
                    if (lifetime != null)
                    {
                        this.Lifetime = lifetime.InnerText;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the SimpleWebTokenHandler class  
        /// </summary>
        /// <param name="certificate">certificate used for encrypting session tokens</param>
        public SimpleWebTokenHandler(X509Certificate2 certificate)
            : base()
        {
            List<CookieTransform> transforms = new List<CookieTransform>();
            transforms.Add(new DeflateCookieTransform());
            transforms.Add(new RsaEncryptionCookieTransform(certificate));
            transforms.Add(new RsaSignatureCookieTransform(certificate));
            this.SetTransforms(transforms);
        }

        /// <summary>
        /// Initializes a new instance of the SimpleWebTokenHandler class  
        /// </summary>
        /// <param name="transforms">readonly collection of cookie transforms</param>
        public SimpleWebTokenHandler(ReadOnlyCollection<CookieTransform> transforms)
            : base()
        {
            this.SetTransforms(transforms);
        }

        /// <summary>
        /// Gets the token type
        /// </summary>
        public override Type TokenType
        {
            get { return typeof(SimpleWebToken); }
        }

        /// <summary>
        /// Gets whether token can be validated
        /// </summary>
        public override bool CanValidateToken
        {
            get { return true; }
        }

        /// <summary>
        /// Gets whether token can be written
        /// </summary>
        public override bool CanWriteToken
        {
            get { return true; }
        }

        /// <summary>
        /// Method to get the token type identifiers
        /// </summary>
        /// <returns>array of token type identifiers</returns>
        public override string[] GetTokenTypeIdentifiers()
        {
            return new string[] { Resources.TokenTypeIdentifier };
        }

        /// <summary>
        /// Method to write the token
        /// </summary>
        /// <param name="writer">XmlWriter to write the token</param>
        /// <param name="token">SecurityToken to be written</param>
        public override void WriteToken(XmlWriter writer, SecurityToken token)
        {
            var simpleWebToken = token as SimpleWebToken;

            if (simpleWebToken == null)
            {
                throw new InvalidOperationException(Resources.SwtWrongType);
            }

            WrapInsideBinarySecurityToken(simpleWebToken.SignedString).WriteTo(writer);
        }

        /// <summary>
        /// Method to validate the token
        /// </summary>
        /// <param name="token">SecurityToken to be validated</param>
        /// <returns>identity cliams collection</returns>
        public override ClaimsIdentityCollection ValidateToken(SecurityToken token)
        {
            SimpleWebToken realToken = token as SimpleWebToken;
            if (realToken == null)
            {
                throw new InvalidOperationException(Resources.SwtWrongType);
            }

            // Make sure audienceUri is the one expected
            if (StringComparer.OrdinalIgnoreCase.Compare(realToken.AudienceUri.ToString(), Config.GetValue("ACS.AudienceUri")) != 0)
            {
                throw new InvalidOperationException(Resources.ExpectedAudienceUri + Config.GetValue("ACS.AudienceUri"));
            }

            // Make sure issuer is trusted
            if (StringComparer.OrdinalIgnoreCase.Compare(realToken.Issuer, trustedIssuer) != 0)
            {
                throw new InvalidOperationException(Resources.IssuerNotTrusted + trustedIssuer);
            }

            // Verify signature
            if (!realToken.SignVerify(Convert.FromBase64String(symmetricSignatureKey)))
            {
                throw new InvalidOperationException(Resources.SignatureVerificationFailed);
            }

            // Make sure token is not expired
            if (DateTime.Compare(realToken.ValidTo, DateTime.UtcNow) <= 0)
            {
                throw new InvalidOperationException(Resources.TokenExpired);
            }

            ClaimsIdentityCollection identities = new ClaimsIdentityCollection();
            ClaimsIdentity identity = new ClaimsIdentity();

            foreach (var claim in realToken.Claims)
            {
                identity.Claims.Add(claim);
            }

            if (this.Configuration.SaveBootstrapTokens)
            {
                identity.BootstrapToken = token;
            }

            identities.Add(identity);

            return identities;
        }

        /// <summary>
        /// Method to create the token
        /// </summary>
        /// <param name="tokenDescriptor">token descriptor</param>
        /// <returns>created security token</returns>
        public override SecurityToken CreateToken(SecurityTokenDescriptor tokenDescriptor)
        {
            throw new InvalidOperationException(Resources.NotImplemented);
        }

        /// <summary>
        /// Method to create the security token reference
        /// </summary>
        /// <param name="token">token to be referenced</param>
        /// <param name="attached">Whether to attache the token</param>
        /// <returns>the security token reference</returns>
        public override SecurityKeyIdentifierClause CreateSecurityTokenReference(SecurityToken token, bool attached)
        {
            var swt = token as SimpleWebToken;

            if (swt == null)
            {
                throw new InvalidOperationException(Resources.SwtWrongType);
            }

            return new KeyNameIdentifierClause(swt.Issuer);
        }

        /// <summary>
        /// Method to create a session security token
        /// </summary>
        /// <param name="principal">the claims principal</param>
        /// <param name="context">security context</param>
        /// <param name="endpointId">endpoint identifier</param>
        /// <param name="validFrom">date token is valid</param>
        /// <param name="validTo">date token ends validity</param>
        /// <returns>session security token</returns>
        public override SessionSecurityToken CreateSessionSecurityToken(Microsoft.IdentityModel.Claims.IClaimsPrincipal principal, string context, string endpointId, DateTime validFrom, DateTime validTo)
        {
            return base.CreateSessionSecurityToken(principal, context, endpointId, DateTime.Now, DateTime.UtcNow.AddMinutes(Config.GetIntValue("ACS.TokenDurationMinutes")));
        }

        /// <summary>
        /// Method to specify the token encrypting credentials. 
        /// </summary>
        /// <param name="tokenDescriptor">The token descriptor.</param>
        /// <returns>The token encrypting credentials.</returns>
        protected virtual EncryptingCredentials GetEncryptingCredentials(SecurityTokenDescriptor tokenDescriptor)
        {
            if (null == tokenDescriptor)
            {
                throw new ArgumentNullException("tokenDescriptor");
            }

            EncryptingCredentials encryptingCredentials = null;

            if (null != tokenDescriptor.EncryptingCredentials)
            {
                encryptingCredentials = tokenDescriptor.EncryptingCredentials;

                if (encryptingCredentials.SecurityKey is SymmetricSecurityKey)
                {
                    encryptingCredentials = new EncryptedKeyEncryptingCredentials(encryptingCredentials, SecurityAlgorithmSuite.Default.DefaultSymmetricKeyLength, SecurityAlgorithmSuite.Default.DefaultEncryptionAlgorithm);
                }
            }

            return encryptingCredentials;
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
    }
}
