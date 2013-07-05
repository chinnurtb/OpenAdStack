//-----------------------------------------------------------------------
// <copyright file="InvitationMailConfigTestFixture.cs" company="Rare Crowds Inc">
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

using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using EntityActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityActivitiesUnitTests
{
    /// <summary>
    /// Test for InvitationMailConfig
    /// </summary>
    [TestClass]
    public class InvitationMailConfigTestFixture
    {
        /// <summary>
        /// Tests creation of Invitation Mail Configuration file
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CreateInvitationMailConfigTest()
        {
            // Creating mapping list            
            var invitationMailConfig = new InvitationMailConfig
            {
                SubjectFormat = "Test Invitation",
                BodyFormat = "<a href = \"https://traffiqdev.cloudapp.net/api/user/verify/{0}\"> https://traffiqdev.cloudapp.net/api/user/verify/{0}</a>"
            };          

            // Deserializing to configurationMapping.xml
            using (FileStream writer = new FileStream("InvitationMailConfig.xml", FileMode.Create))
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(InvitationMailConfig));
                ser.WriteObject(writer, invitationMailConfig);                
            }
        }
    }
}
