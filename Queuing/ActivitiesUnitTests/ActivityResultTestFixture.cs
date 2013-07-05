//-----------------------------------------------------------------------
// <copyright file="ActivityResultTestFixture.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Web.Script.Serialization;
using Activities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ActivityUnitTests
{
    /// <summary>
    /// Tests for ActivityResult
    /// </summary>
    [TestClass]
    public class ActivityResultTestFixture
    {
        /// <summary>
        /// Test round-trip serialization/deserialization
        /// </summary>
        [TestMethod]
        public void SerializationTest()
        {
            var expected = new ActivityResult
            {
                Succeeded = false,
                Error =
                {
                    ErrorId = Guid.NewGuid().GetHashCode(),
                    Message = Guid.NewGuid().ToString()
                },
                Values =
                {
                    { "Foo", Guid.NewGuid().ToString() },
                    { "Bar", Guid.NewGuid().ToString() }
                }
            };

            var serializedXml = expected.SerializeToXml();
            Assert.IsNotNull(serializedXml);
            Assert.IsTrue(serializedXml.Contains(expected.Error.ErrorId.ToString()));
            Assert.IsTrue(serializedXml.Contains(expected.Error.Message));
            Assert.IsTrue(serializedXml.Contains("Foo"));
            Assert.IsTrue(serializedXml.Contains("Bar"));
            Assert.IsTrue(serializedXml.Contains(expected.Values["Foo"]));
            Assert.IsTrue(serializedXml.Contains(expected.Values["Bar"]));

            var deserialized = ActivityResult.DeserializeFromXml(serializedXml);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(expected.Succeeded, deserialized.Succeeded);
            Assert.IsNotNull(deserialized.Error);
            Assert.AreEqual(expected.Error.ErrorId, deserialized.Error.ErrorId);
            Assert.AreEqual(expected.Error.Message, deserialized.Error.Message);
            Assert.IsNotNull(deserialized.Values);
            Assert.AreEqual(2, deserialized.Values.Count);
            Assert.IsTrue(deserialized.Values.ContainsKey("Foo"));
            Assert.IsTrue(deserialized.Values.ContainsKey("Bar"));
            Assert.AreEqual(expected.Values["Foo"], deserialized.Values["Foo"]);
            Assert.AreEqual(expected.Values["Bar"], deserialized.Values["Bar"]);

            var reserializedXml = deserialized.SerializeToXml();
            Assert.AreEqual(serializedXml, reserializedXml);
        }

        /// <summary>
        /// Test to verify serialization of errors to json
        /// </summary>
        [TestMethod]
        public void ActivityErrorJsonTest()
        {
            var error = new ActivityError
            {
                ErrorId = 42,
                Message = Guid.NewGuid().ToString("N")
            };

            var errorJson = error.SerializeToJson();
            Assert.IsNotNull(errorJson);

            var deserialized = new JavaScriptSerializer().Deserialize<ActivityError>(errorJson);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(error.ErrorId, deserialized.ErrorId);
            Assert.AreEqual(error.Message, deserialized.Message);
        }
    }
}
