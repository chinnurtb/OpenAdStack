//-----------------------------------------------------------------------
// <copyright file="ActivityResultTestFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
