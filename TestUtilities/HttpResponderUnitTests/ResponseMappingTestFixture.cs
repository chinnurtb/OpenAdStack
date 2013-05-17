//-----------------------------------------------------------------------
// <copyright file="ResponseMappingTestFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using HttpResponder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HttpResponderUnitTests
{
    /// <summary>
    /// Tests related to response mapping
    /// </summary>
    [TestClass]
    public class ResponseMappingTestFixture
    {
        /// <summary>
        /// Tests creation of Configuration mapping file (ConfigurationMapping.xml)
        /// </summary>
        [TestMethod]
        public void CreateMappingConfigurationTest()
        {
            // Creating mapping list
            List<ResponseMapping> mappingList = new List<ResponseMapping>();
            var getUserMapping = new ResponseMapping
            {
                ResourceName = "HttpResponder.Resources.GetUserJson",
                HttpVerb = "GET",
                ResponseStatusCode = 404,
                UrlRegex = "/user/.*"
            };

            var savedUserMapping = new ResponseMapping
            {
                ResourceName = "HttpResponder.Resources.SavedUserJson",
                HttpVerb = "PUT",
                ResponseStatusCode = 404,
                UrlContains = "/user",
                BodyRegex = @"""EntityCategory""[ ]*:[ ]*""User"""
            };
            mappingList.Add(getUserMapping);
            mappingList.Add(savedUserMapping);

            // Getting assembly's current executing path
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Deserializing to configurationMapping.xml
            FileStream writer = new FileStream(currentPath + "\\ConfigurationMapping.xml", FileMode.Create);
            DataContractSerializer ser = new DataContractSerializer(typeof(List<ResponseMapping>));
            ser.WriteObject(writer, mappingList);
            writer.Close();
        }
    }
}
