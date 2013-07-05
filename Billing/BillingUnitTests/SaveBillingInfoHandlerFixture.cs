// -----------------------------------------------------------------------
// <copyright file="SaveBillingInfoHandlerFixture.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using Activities;
using BillingActivities;
using DataAccessLayer;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaymentProcessor;
using ResourceAccess;
using Rhino.Mocks;
using Utilities;

namespace BillingUnitTests
{
    /// <summary>
    /// Unit-test fixture for SaveBillingInfoHandler
    /// </summary>
    [TestClass]
    public class SaveBillingInfoHandlerFixture
    {
        /// <summary>Default repository for testing.</summary>
        private IEntityRepository repository;

        /// <summary>IResourceAccessHandler for testing</summary>
        private IResourceAccessHandler accessHandler;

        /// <summary>IPaymentProcessor for testing</summary>
        private IPaymentProcessor paymentProcessor;

        /// <summary>Company id for testing</summary>
        private EntityId companyEntityId;

        /// <summary>Company entity for testing</summary>
        private CompanyEntity companyEntity;

        /// <summary>Authorization user id for testing</summary>
        private string authUserId;

        /// <summary>EntityId for testing.</summary>
        private EntityId userEntityId;

        /// <summary>UserEntity for testing.</summary>
        private UserEntity userEntity;

        /// <summary>api request payload for testing.</summary>
        private string messagePayload;

        /// <summary>Billing token for testing.</summary>
        private string billingToken;

        /// <summary>Per-test intitialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.companyEntityId = new EntityId();
            this.companyEntity = EntityTestHelpers.CreateTestCompanyEntity(this.companyEntityId, "companyname");
            this.authUserId = "userfoo";
            this.userEntityId = new EntityId();
            this.userEntity = EntityTestHelpers.CreateTestUserEntity(this.userEntityId, this.authUserId, string.Empty);
            this.billingToken = "sometoken";
            this.messagePayload = @"{{""BillingInfoToken"":""{0}""}}".FormatInvariant(this.billingToken);
            this.repository = MockRepository.GenerateStub<IEntityRepository>();
            this.accessHandler = MockRepository.GenerateStub<IResourceAccessHandler>();
            this.paymentProcessor = MockRepository.GenerateStub<IPaymentProcessor>();

            // Setup stubs for access checks
            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Equal(this.authUserId)))
                .Return(this.userEntity);
            this.accessHandler.Stub(f => f.CheckAccess(
                    Arg<CanonicalResource>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.userEntityId))).Return(true);
        }

        /// <summary>Happy path construction success.</summary>
        [TestMethod]
        public void ConstructorSuccess()
        {
            var handler = new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.messagePayload, this.authUserId);
            Assert.IsNotNull(handler);
            Assert.IsInstanceOfType(handler, typeof(SaveBillingInfoHandler));
            Assert.AreSame(this.repository, handler.Repository);
            Assert.AreSame(this.accessHandler, handler.AccessHandler);
            Assert.AreEqual(this.companyEntityId, handler.CompanyEntityId);
            Assert.AreEqual(this.authUserId, handler.AuthUserId);
            Assert.AreEqual(this.messagePayload, handler.MessagePayload);
        }

        /// <summary>Null repository should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullRepository()
        {
            new SaveBillingInfoHandler(
                null, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.messagePayload, this.authUserId);
        }

        /// <summary>Null access handler should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullAccessHandler()
        {
            new SaveBillingInfoHandler(
                this.repository, null, this.paymentProcessor, this.companyEntityId, this.messagePayload, this.authUserId);
        }

        /// <summary>Null payment processor should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullPaymentProcessor()
        {
            new SaveBillingInfoHandler(
                this.repository, this.accessHandler, null, this.companyEntityId, this.messagePayload, this.authUserId);
        }

        /// <summary>Null company id should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullCompanyId()
        {
            new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, null, this.messagePayload, this.authUserId);
        }

        /// <summary>Null message payload should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullMessagePayload()
        {
            new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, null, this.authUserId);
        }

        /// <summary>Null auth user id should fail.</summary>
        [TestMethod]
        [ExpectedException(typeof(AppsGenericException))]
        public void ConstructorNullAuthUserId()
        {
            new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.messagePayload, null);
        }

        /// <summary>Failed access check should throw.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void ExecuteAccessDenied()
        {
            // Setup stub for failed access check
            this.accessHandler = MockRepository.GenerateStub<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(
                    Arg<CanonicalResource>.Is.Anything,
                    Arg<EntityId>.Is.Equal(this.userEntityId))).Return(false);
            var handler = new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.messagePayload, this.authUserId);

            handler.Execute();
        }

        /// <summary>Happy-path for new billing customer.</summary>
        [TestMethod]
        public void ExecuteCreateNewCustomerSuccess()
        {
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, this.companyEntity, false);
            CompanyEntity savedCompany = null;
            RepositoryStubUtilities.SetupSaveEntityStub<CompanyEntity>(this.repository, e => { savedCompany = e; }, false);

            var billingCustomerId = "somecustomerid";
            this.paymentProcessor.Stub(f => f.CreateCustomer(
                    Arg<string>.Is.Equal(this.billingToken),
                    Arg<string>.Is.Equal((string)this.companyEntity.ExternalName))).Return(billingCustomerId);
            var handler = new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.messagePayload, this.authUserId);
            
            handler.Execute();

            Assert.AreEqual(billingCustomerId, savedCompany.GetPropertyByName<string>(BillingActivityNames.CustomerBillingId));
        }

        /// <summary>Happy-path for update billing customer.</summary>
        [TestMethod]
        public void ExecuteUpdateCustomerSuccess()
        {
            var billingCustomerId = "somecustomerid";
            this.companyEntity.SetPropertyByName(BillingActivityNames.CustomerBillingId, billingCustomerId);
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, this.companyEntityId, this.companyEntity, false);

            this.paymentProcessor.Stub(f => f.UpdateCustomer(
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything,
                    Arg<string>.Is.Anything)).Return(billingCustomerId);
            var handler = new SaveBillingInfoHandler(
                this.repository, this.accessHandler, this.paymentProcessor, this.companyEntityId, this.messagePayload, this.authUserId);

            handler.Execute();

            this.paymentProcessor.AssertWasCalled(f => f.UpdateCustomer(
                this.billingToken, billingCustomerId, this.companyEntity.ExternalName));
        }
    }
}
