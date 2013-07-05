//-----------------------------------------------------------------------
// <copyright file="UserMailTestFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.IO;
using System.Runtime.Serialization;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using EntityActivities.UserMail;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Net.Mail;

namespace EntityActivityUnitTests
{
    /// <summary>
    /// Test for user mail
    /// </summary>
    [TestClass]
    public class UserMailTestFixture
    {
        /// <summary>
        /// Mock entity repository used for tests
        /// </summary>
        private IEntityRepository repository;

        /// <summary>
        /// Initialize the mock entity repository before each test
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.repository = MockRepository.GenerateMock<IEntityRepository>();

            ConfigurationManager.AppSettings["Mail.UserInvite.SmtpHost"] = "mail.rc.dev";
            ConfigurationManager.AppSettings["Mail.UserInvite.Username"] = "username";
            ConfigurationManager.AppSettings["Mail.UserInvite.Password"] = "password";
            ConfigurationManager.AppSettings["UserMail.Invitation.LinkFormat"] = "https://traffiqdev.cloudapp.net/userverification.html?id={0}";
        }
        
        /// <summary>
        /// Tests creation of Mail Configuration file
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CreateDefaultMailTemplateXml()
        {
            // Creating mapping list            
            var mailTemplates = new Dictionary<string, MailTemplate>
            {
                {
                    "UserInvite",
                    new MailTemplate
                    {
                        Sender = "test@traffiq.com",
                        SubjectFormat = "Test Invitation",
                        BodyFormat = @"<p>Click the link below to complete registration:</p><p>{0}</p>",
                        IsBodyHtml = true
                    }
                }
            };

            // Deserializing to MailTemplate.xml
            using (FileStream writer = new FileStream("DefaultMailTemplates.xml", FileMode.Create))
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(Dictionary<string, MailTemplate>));
                ser.WriteObject(writer, mailTemplates);                
            }
        }

        /// <summary>
        /// Tests SendUserInviteActivity
        /// </summary>
        [TestMethod]
        public void SendUserInviteTest()
        {
            var mockMailClient = MockRepository.GenerateMock<IMailClient>();
            mockMailClient.Stub(f =>
                f.SendMail(Arg<string>.Is.Anything, Arg<string>.Is.Anything, Arg<object[]>.Is.Anything, Arg<object[]>.Is.Anything));

            var userEntityId = new EntityId();
            var user = EntityTestHelpers.CreateTestUserEntity(userEntityId, Guid.NewGuid().ToString(), "newuser@rc.dev");
            RepositoryStubUtilities.SetupGetEntityStub(this.repository, userEntityId, user, false);

            // Create a request
            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", Guid.NewGuid().ToString() },
                    { "EntityId", userEntityId }
                }
            };

            // Create the activity
            var activity = (SendUserInviteMailActivity)Activity.CreateActivity(typeof(SendUserInviteMailActivity), new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } }, ActivityTestHelpers.SubmitActivityRequest);
            Assert.IsNotNull(activity);

            // Set the mock mail config provider
            activity.MailClient = mockMailClient;

            // Run the activity
            var result = activity.Run(request);
            ActivityTestHelpers.AssertValidSuccessResult(result);
            mockMailClient.AssertWasCalled(f =>
                f.SendMail(
                    Arg<string>.Is.Equal("UserInvite"),
                    Arg<string>.Is.Equal("newuser@rc.dev"),
                    Arg<object[]>.Is.Null,
                    Arg<object[]>.Is.NotNull));
        }
    }
}
