//-----------------------------------------------------------------------
// <copyright file="OAuthSecurityFixture.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using AzureUtilities.Storage;
using DataAccessLayer;
using Diagnostics;
using EntityUtilities;
using IdentityFederation;
using Microsoft.IdentityModel.Claims;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ResourceAccess;
using Rhino.Mocks;
using RuntimeIoc.WebRole;
using TestUtilities;
using Utilities.Storage;

namespace SecurityIntegrationTests
{
    /// <summary>Integration test fixture for Authorization tests.</summary>
    [TestClass]
    public class OAuthSecurityFixture
    {
        /// <summary>Default company for testing (created once for the test run).</summary>
        private static EntityId defaultTestCompanyId = null;

        /// <summary>User for testing (created once for the test run).</summary>
        private static EntityId defaultTestUserEntityId = null;

        /// <summary>Unique user id corresponding to defaultTestUserEntityId.</summary>
        private static string defaultTestUserId = null;
        
        /// <summary>Default repository for testing.</summary>
        private IEntityRepository entityRepository;

        /// <summary>Initialize Azure storage emulator.</summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static void AssemblyInitialization(TestContext context)
        {
            // Settings we don't care about tha need to be there to keep runtime ioc happy
            ConfigurationManager.AppSettings["Queue.WorkItemStoreName"] = "someaddress";
            ConfigurationManager.AppSettings["Logging.BlobContainer"] = "quotalogs";
            ConfigurationManager.AppSettings["Logging.RootPath"] = "quotalogs";
            ConfigurationManager.AppSettings["Logging.MaximumSizeInMegabytes"] = "1024";
            ConfigurationManager.AppSettings["Logging.ScheduledTransferPeriodMinutes"] = "5";
            ConfigurationManager.AppSettings["Testing.HttpHeaderClaimOverrides"] = "false";
            ConfigurationManager.AppSettings["AppNexus.IsApp"] = "false";
            
            // Singleton initialization we don't care about but need to keep environment happy
            LogManager.Initialize(new[] { MockRepository.GenerateMock<ILogger>() });
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = PersistentDictionaryType.Cloud.ToString();
            PersistentDictionaryFactory.Initialize(new[]
                {
                    new CloudBlobDictionaryFactory(ConfigurationManager.AppSettings["Blob.ConnectionString"])
                });
            
            // Force Azure emulated storage to start. DSService can still be running
            // but the emulated storage not available. The most reliable way to make sure
            // it's running and available is to stop it then start again.
            var emulatorRunnerPath = ConfigurationManager.AppSettings["AzureEmulatorExe"];
            AzureEmulatorHelper.StopStorageEmulator(emulatorRunnerPath);
            AzureEmulatorHelper.StartStorageEmulator(emulatorRunnerPath);
        }

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.entityRepository = RuntimeIocContainer.Instance.Resolve<IEntityRepository>();

            // Create a default company to save entites against
            if (defaultTestCompanyId == null)
            {
                var company = EntityJsonSerializer.DeserializeCompanyEntity(new EntityId(), @"{""ExternalName"":""TestCompany"", ""ExternalType"":""Company.Agency""}");
                this.entityRepository.AddCompany(new RequestContext(), company);
                defaultTestCompanyId = company.ExternalEntityId;
            }

            // Create a test user with a unique userId
            if (defaultTestUserEntityId == null)
            {
                var user = EntityJsonSerializer.DeserializeUserEntity(new EntityId(), @"{""ExternalName"":""TestUser"", ""ExternalType"":""User""}");
                user.UserId = Guid.NewGuid().ToString("N");
                this.entityRepository.SaveUser(new RequestContext(), user);
                defaultTestUserEntityId = user.ExternalEntityId;
                defaultTestUserId = user.UserId;
            }
        }

        /// <summary>Default construction of CustomClaimsAuthorizationManager.</summary>
        [TestMethod]
        public void DefaultConstructCustomClaimsAuthorizationManager()
        {
            var authZMgr = new CustomClaimsAuthorizationManager();
            Assert.IsNotNull(authZMgr.ResourceAccessHandler);
            Assert.IsNotNull(((ResourceAccessHandler)authZMgr.ResourceAccessHandler).EntityRepository);
            Assert.IsNotNull(((ResourceAccessHandler)authZMgr.ResourceAccessHandler).UserAccessRepository);
        }

        /// <summary>Default construction of CustomClaimsAuthenticationManager.</summary>
        [TestMethod]
        public void DefaultConstructCustomClaimsAuthenticationManager()
        {
            var authNMgr = new CustomClaimsAuthenticationManager();
            Assert.IsNotNull(authNMgr.EntityRepository);
        }
        
        /// <summary>Authorization access list successfully retrieved.</summary>
        [TestMethod]
        public void CheckAccessUserFound()
        {
            var authZMgr = new CustomClaimsAuthorizationManager();

            var userClaim = new Claim(
                CustomClaimsAuthenticationManager.RcUserClaimType,
                defaultTestUserEntityId.ToString(),
                ClaimValueTypes.String,
                CustomClaimsAuthenticationManager.InternalIssuer);

            var validPrincipal = new ClaimsPrincipal(new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { userClaim }) });
            var authContext = new AuthorizationContext(validPrincipal, "https://localhost/api/COMPANY", "GET");
            var accessGranted = authZMgr.CheckAccess(authContext);
            Assert.IsTrue(accessGranted);
        }

        /// <summary>Authentication succeeds when user found in system.</summary>
        [TestMethod]
        public void AuthenticateUserFound()
        {
            var authNMgr = new CustomClaimsAuthenticationManager();
            
            var identityClaim = new Claim(
                ClaimTypes.NameIdentifier, defaultTestUserId, ClaimValueTypes.String, authNMgr.AcsIssuer);

            var validPrincipal = new ClaimsPrincipal(new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { identityClaim }) });

            var modifedPrincipal = authNMgr.Authenticate("https://localhost/api/COMPANY", validPrincipal);
            var userClaim = modifedPrincipal.Identities.SelectMany(
                i => i.Claims.Where(c => c.ClaimType == CustomClaimsAuthenticationManager.RcUserClaimType)).SingleOrDefault();
            Assert.IsNotNull(userClaim);
            Assert.AreEqual(defaultTestUserEntityId.ToString(), userClaim.Value);
        }

        /// <summary>Authentication fails when user not found in system.</summary>
        [TestMethod]
        public void AuthenticateUserNotFound()
        {
            var authNMgr = new CustomClaimsAuthenticationManager();

            var identityClaim = new Claim(
                ClaimTypes.NameIdentifier, "nahnahnahnah", ClaimValueTypes.String, authNMgr.AcsIssuer);

            var validPrincipal = new ClaimsPrincipal(new List<IClaimsIdentity> { new ClaimsIdentity(new List<Claim> { identityClaim }) });

            var modifedPrincipal = authNMgr.Authenticate("https://localhost/api/COMPANY", validPrincipal);
            var userClaim = modifedPrincipal.Identities.SelectMany(
                i => i.Claims.Where(c => c.ClaimType == CustomClaimsAuthenticationManager.RcUserClaimType)).SingleOrDefault();
            Assert.IsNull(userClaim);
        }
    }
}
