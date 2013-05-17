//-----------------------------------------------------------------------
// <copyright file="InvitationMailConfigTestFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
