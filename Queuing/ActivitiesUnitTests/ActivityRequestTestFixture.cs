//-----------------------------------------------------------------------
// <copyright file="ActivityRequestTestFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using Activities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ActivityUnitTests
{
    /// <summary>
    /// Tests for ActivityRequest
    /// </summary>
    [TestClass]
    public class ActivityRequestTestFixture
    {
        /// <summary>
        /// Test round-trip serialization/deserialization
        /// </summary>
        [TestMethod]
        public void SerializationTest()
        {
            var expected = new ActivityRequest
            {
                Task = Guid.NewGuid().ToString(),
                Values =
                {
                    { "Foo", Guid.NewGuid().ToString() },
                    { "Bar", Guid.NewGuid().ToString() }
                }
            };

            var serializedXml = expected.SerializeToXml();
            Assert.IsNotNull(serializedXml);
            Assert.IsTrue(serializedXml.Contains(expected.Task));
            Assert.IsTrue(serializedXml.Contains("Foo"));
            Assert.IsTrue(serializedXml.Contains("Bar"));
            Assert.IsTrue(serializedXml.Contains(expected.Values["Foo"]));
            Assert.IsTrue(serializedXml.Contains(expected.Values["Bar"]));

            var deserialized = ActivityRequest.DeserializeFromXml(serializedXml);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(expected.Task, deserialized.Task);
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
        /// Test trying to get a valid value
        /// </summary>
        [TestMethod]
        public void TryGetValidValue()
        {
            var expectedValue = Guid.NewGuid().ToString("N");
            var request = new ActivityRequest
            {
                Values =
                {
                    { "value", expectedValue }
                }
            };

            string value;
            Assert.IsTrue(request.TryGetValue("value", out value));
            Assert.AreEqual(expectedValue, value);
        }

        /// <summary>
        /// Test trying to get a non-existent value
        /// </summary>
        [TestMethod]
        public void TryGetInvalidValue()
        {
            var request = new ActivityRequest();
            string value;
            Assert.IsFalse(request.TryGetValue("value", out value));
        }

        /// <summary>
        /// Test trying to get a valid integer value
        /// </summary>
        [TestMethod]
        public void TryGetValidIntegerValue()
        {
            var expectedValue = new Random().Next();
            var request = new ActivityRequest
            {
                Values =
                {
                    { "value", expectedValue.ToString(CultureInfo.InvariantCulture) }
                }
            };

            int value;
            Assert.IsTrue(request.TryGetIntegerValue("value", out value));
            Assert.AreEqual(expectedValue, value);
        }

        /// <summary>
        /// Test trying to get an invalid integer value
        /// </summary>
        [TestMethod]
        public void TryGetInvalidIntegerValue()
        {
            var request = new ActivityRequest
            {
                Values = { { "value", "invalid" } }
            };

            int value;
            Assert.IsFalse(request.TryGetIntegerValue("value", out value));
        }

        /// <summary>
        /// Test trying to get a valid decimal value
        /// </summary>
        [TestMethod]
        public void TryGetValidDecimalValue()
        {
            var r = new Random();
            var expectedValue = (decimal)r.NextDouble() * r.Next();
            var request = new ActivityRequest
            {
                Values =
                {
                    { "value", expectedValue.ToString(CultureInfo.InvariantCulture) }
                }
            };

            decimal value;
            Assert.IsTrue(request.TryGetDecimalValue("value", out value));
            Assert.AreEqual(expectedValue, value);
        }

        /// <summary>
        /// Test trying to get an invalid decimal value
        /// </summary>
        [TestMethod]
        public void TryGetInvalidDecimalValue()
        {
            var request = new ActivityRequest
            {
                Values = { { "value", "invalid" } }
            };

            decimal value;
            Assert.IsFalse(request.TryGetDecimalValue("value", out value));
        }
    }
}
