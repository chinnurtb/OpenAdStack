//-----------------------------------------------------------------------
// <copyright file="OAuthSecurityTestFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using ConfigManager;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OAuthSecurity;

namespace SecurityUnitTests
{
    /// <summary>
    /// Class for testing OAuthSecurity project
    /// </summary>
    [TestClass]
    public class OAuthSecurityTestFixture
    {
        /// <summary>
        /// SecurityToken used for testing
        /// </summary>
        private XElement xelement = null;

        /// <summary>
        /// Initialize the claims
        /// </summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.xelement = new XElement(
                            XNamespace.Get("http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd").GetName("BinarySecurityToken"),
                            new XAttribute("ValueType", "http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0"),
                            new XAttribute("EncodingType", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"),
                            Convert.ToBase64String(Encoding.Default.GetBytes("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier=9iAhEJf60UnNiZtI6AfH3YPaDo40iFbSxVCCD25XGaE=&http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider=uri:WindowsLiveID&Audience=https://localhost/&ExpiresOn=9999999999&Issuer=https://traffiqemu.accesscontrol.windows.net/&HMACSHA256=HfQKf72+nOmFtEKoB21YSBh1Gp4bg/0R4/7r/zIGYZA=")));
        }

        /// <summary>
        /// Test for SecurityHelper IsOAuthAuthorization()
        /// </summary>
        [TestMethod]
        public void IsOAuthAuthorizationNotAuthorized()
        {
            string uri = "http://localhost:80/ApiLayer/campaign/123";
            HttpRequest httpRequest = new HttpRequest("someFile", uri, "someQueryString");
            Assert.IsFalse(SecurityHelper.IsOAuthAuthorization(httpRequest));
        }

        /// <summary>
        /// Test for SecurityHelper IsBase64()
        /// </summary>
        [TestMethod]
        public void IsBase64True()
        {
            string base64 = "yes=";
            Assert.IsTrue(SecurityHelper.IsBase64(base64));
        }

        /// <summary>
        /// Test for SecurityHelper IsBase64()
        /// </summary>
        [TestMethod]
        public void IsBase64False()
        {
            string base64 = "no!=";
            Assert.IsFalse(SecurityHelper.IsBase64(base64));
        }
        
        /// <summary>
        /// Test for creating SimpleWebToken
        /// </summary>
        [TestMethod]
        public void SimpleWebToken()
        {
            Collection<Claim> claims = new Collection<Claim>();
            Claim organizationClaim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/organization", "Emerging Media Group", ClaimValueTypes.String, Config.GetValue("ACS.Issuer"));
            claims.Add(organizationClaim);
            DateTime tenMinutes = DateTime.UtcNow.AddMinutes(10.0);
            SimpleWebToken simpleWebToken = new SimpleWebToken(new Uri("https://localhost"), Config.GetValue("ACS.Issuer"), tenMinutes, claims, "signature", "unsigned string", "signed string");
            Assert.AreEqual(new Uri("https://localhost"), simpleWebToken.AudienceUri);
            Assert.AreEqual(claims, simpleWebToken.Claims);
            Assert.AreEqual(Config.GetValue("ACS.Issuer"), simpleWebToken.Issuer);
            Assert.AreEqual("signed string", simpleWebToken.SignedString);
            Assert.AreEqual("unsigned string", simpleWebToken.UnsignedString);
            Assert.AreEqual(tenMinutes, simpleWebToken.ValidTo);
        }

        /// <summary>
        /// Test for ability to read SecurityToken
        /// </summary>
        [TestMethod]
        public void CanReadToken()
        {
            SimpleWebTokenHandler tokenHandler = new SimpleWebTokenHandler();
            Assert.IsTrue(tokenHandler.CanReadToken(this.xelement.CreateReader()));
        }

        /// <summary>
        /// Test for reading SecurityToken
        /// </summary>
        [TestMethod]
        public void ReadToken()
        {
            SimpleWebTokenHandler tokenHandler = new SimpleWebTokenHandler();
            SimpleWebToken token = (SimpleWebToken)tokenHandler.ReadToken(this.xelement.CreateReader());
            Assert.AreEqual("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier=9iAhEJf60UnNiZtI6AfH3YPaDo40iFbSxVCCD25XGaE=&http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider=uri:WindowsLiveID&Audience=https://localhost/&ExpiresOn=9999999999&Issuer=https://traffiqemu.accesscontrol.windows.net/", token.UnsignedString);
        }

        /// <summary>
        /// Test for writing SecurityToken
        /// </summary>
        [TestMethod]
        public void WriteToken()
        {
            SimpleWebTokenHandler tokenHandler = new SimpleWebTokenHandler();
            SimpleWebToken token = (SimpleWebToken)tokenHandler.ReadToken(this.xelement.CreateReader());
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter textWriter = new XmlTextWriter(stringWriter))
            {
                tokenHandler.WriteToken(textWriter, token);
                Assert.AreEqual(@"<BinarySecurityToken ValueType=""http://schemas.xmlsoap.org/ws/2009/11/swt-token-profile-1.0"" EncodingType=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"" xmlns=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">aHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXI9OWlBaEVKZjYwVW5OaVp0STZBZkgzWVBhRG80MGlGYlN4VkNDRDI1WEdhRT0maHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS9hY2Nlc3Njb250cm9sc2VydmljZS8yMDEwLzA3L2NsYWltcy9pZGVudGl0eXByb3ZpZGVyPXVyaTpXaW5kb3dzTGl2ZUlEJkF1ZGllbmNlPWh0dHBzOi8vbG9jYWxob3N0LyZFeHBpcmVzT249OTk5OTk5OTk5OSZJc3N1ZXI9aHR0cHM6Ly90cmFmZmlxZW11LmFjY2Vzc2NvbnRyb2wud2luZG93cy5uZXQvJkhNQUNTSEEyNTY9SGZRS2Y3MituT21GdEVLb0IyMVlTQmgxR3A0YmcvMFI0LzdyL3pJR1laQT0=</BinarySecurityToken>", stringWriter.ToString());
            }
        }

        /// <summary>
        /// Test for writing SecurityToken with null token
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WriteTokenNullToken()
        {
            SimpleWebTokenHandler tokenHandler = new SimpleWebTokenHandler();
            SimpleWebToken token = null;
            using (StringWriter stringWriter = new StringWriter())
            using (XmlTextWriter textWriter = new XmlTextWriter(stringWriter))
            {
                tokenHandler.WriteToken(textWriter, token);
            }
        }

        /// <summary>
        /// Test for validating SimpleWebToken
        /// </summary>
        [TestMethod]
        public void ValidateToken()
        {
            SimpleWebTokenHandler tokenHandler = new SimpleWebTokenHandler();
            SecurityTokenHandlerConfiguration config = new SecurityTokenHandlerConfiguration();
            config.SaveBootstrapTokens = true;
            tokenHandler.Configuration = config;
            SimpleWebToken token = (SimpleWebToken)tokenHandler.ReadToken(this.xelement.CreateReader());
            ClaimsIdentityCollection identityCollection = tokenHandler.ValidateToken(token);
            Assert.AreEqual(1, identityCollection.Count);
            Assert.AreEqual(3, identityCollection[0].Claims.Count);
        }

        /// <summary>
        /// Test for creating a session token
        /// </summary>
        [TestMethod]
        public void CreateSessionSecurityToken()
        {
            SimpleWebTokenHandler tokenHandler = new SimpleWebTokenHandler();
            SecurityTokenHandlerConfiguration config = new SecurityTokenHandlerConfiguration();
            config.SaveBootstrapTokens = true;
            tokenHandler.Configuration = config;
            Collection<Claim> claims = new Collection<Claim>();
            Claim organizationClaim = new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/organization", "Emerging Media Group", ClaimValueTypes.String, Config.GetValue("ACS.Issuer"));
            claims.Add(organizationClaim);
            List<IClaimsIdentity> claimsIdentities = new List<IClaimsIdentity>();
            ClaimsIdentity claimIdentity = new ClaimsIdentity(claims, "Federated");
            claimsIdentities.Add(claimIdentity);
            ClaimsPrincipal incomingPrincipal = new ClaimsPrincipal(claimsIdentities);
            SessionSecurityToken securityToken = tokenHandler.CreateSessionSecurityToken(incomingPrincipal, "context", "1", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(2));
            Assert.IsNotNull(securityToken);
            Assert.IsTrue(DateTime.UtcNow < securityToken.ValidTo);
        }        
    }
}
