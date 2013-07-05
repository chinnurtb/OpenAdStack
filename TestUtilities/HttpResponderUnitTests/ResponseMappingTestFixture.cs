//-----------------------------------------------------------------------
// <copyright file="ResponseMappingTestFixture.cs" company="Rare Crowds Inc">
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
