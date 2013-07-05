// -----------------------------------------------------------------------
// <copyright file="AppsJsonSerializerFixture.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Utilities.Serialization;

namespace CommonUnitTests
{
    /// <summary>Test fixture for AppsJsonSerializer.</summary>
    [TestClass]
    public class AppsJsonSerializerFixture
    {
        /// <summary>Successfully round-trip a UTC date.</summary>
        [TestMethod]
        public void RoundtripUtcDate()
        {
            var dates = new[] { DateTime.UtcNow };
            var json = AppsJsonSerializer.SerializeObject(dates);
            var roundtripDates = AppsJsonSerializer.DeserializeObject<DateTime[]>(json);

            Assert.AreEqual(DateTimeKind.Utc, roundtripDates[0].Kind);
            Assert.AreEqual(dates[0].Ticks, roundtripDates[0].Ticks);
        }

        /// <summary>Date serialized as ISO.</summary>
        [TestMethod]
        public void DateSerializationIsIso()
        {
            var dates = new[] { new DateTime(2012, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc) };
            var json = AppsJsonSerializer.SerializeObject(dates);
            Assert.AreEqual("[\"2012-01-01T01:01:01.001Z\"]", json);
        }

        /// <summary>ISO Date deserialized.</summary>
        [TestMethod]
        public void IsoDateDeserialized()
        {
            var dates = new[] { new DateTime(2012, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc) };
            var deserDates =
                AppsJsonSerializer.DeserializeObject<DateTime[]>(
                    "[\"2012-01-01T01:01:01.001Z\",\"2012-01-01T01:01:01.001Z\"]");

            Assert.AreEqual(DateTimeKind.Utc, deserDates[0].Kind);
            Assert.AreEqual(dates[0].Ticks, deserDates[0].Ticks);
        }

        /// <summary>JsonConvert default date deserialized.</summary>
        [TestMethod]
        public void DefaultJsonConvertDateDeserialized()
        {
            var dates = new[] { new DateTime(2012, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc) };

            // 2012-01-01T01:01:01.001Z is 1325379661001 ms since 1/1/1970
            var deserDates =
                AppsJsonSerializer.DeserializeObject<DateTime[]>(
                    "[\"\\/Date(1325379661001)\\/\",\"\\/Date(1325379661001)\\/\"]");

            Assert.AreEqual(DateTimeKind.Utc, deserDates[0].Kind);
            Assert.AreEqual(dates[0].Ticks, deserDates[0].Ticks);
        }

        /// <summary>JavaScript eval date deserialized.</summary>
        [TestMethod]
        public void JavaScriptDateDeserialized()
        {
            var dates = new[] { new DateTime(2012, 1, 1, 1, 1, 1, 1, DateTimeKind.Utc) };

            // 2012-01-01T01:01:01.001Z is 1325379661001 ms since 1/1/1970
            var deserDates =
                AppsJsonSerializer.DeserializeObject<DateTime[]>("[new Date(1325379661001),new Date(1325379661001)]");

            Assert.AreEqual(DateTimeKind.Utc, deserDates[0].Kind);
            Assert.AreEqual(dates[0].Ticks, deserDates[0].Ticks);
        }

        /// <summary>Roundtrip an anonymous type.</summary>
        [TestMethod]
        public void RoundtripAnAnonymousType()
        {
            var value1 = "value1";
            var value2 = 1.1m;
            var anonymousType = new { Name1 = value1, Name2 = value2 };
            var anonymousTypeDef = new { Name1 = string.Empty, Name2 = new decimal() };

            var json = AppsJsonSerializer.SerializeObject(anonymousType);
            var roundTripType = AppsJsonSerializer.DeserializeAnonymousType(json, anonymousTypeDef);
            Assert.AreEqual(value1, roundTripType.Name1);
            Assert.AreEqual(value2, roundTripType.Name2);
        }

        /// <summary>Rethrow JsonReader exception.</summary>
        [TestMethod]
        public void RethrowJsonReaderException()
        {
            try
            {
                AppsJsonSerializer.DeserializeObject<Dictionary<int, int>>("SomeBogusJson");
                Assert.Fail();
            }
            catch (AppsJsonException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(JsonReaderException));
            }
        }

        /// <summary>Rethrow JsonSerialization exception.</summary>
        [TestMethod]
        public void RethrowJsonSerializationException()
        {
            try
            {
                var obj = new BadJsonClass();
                AppsJsonSerializer.SerializeObject(obj);
                Assert.Fail();
            }
            catch (AppsJsonException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(JsonSerializationException));
            }
        }

        /// <summary>Rethrow other exception.</summary>
        [TestMethod]
        public void RethrowOtherException()
        {
            try
            {
                AppsJsonSerializer.DeserializeObject<Dictionary<int, int>>(null);
                Assert.Fail();
            }
            catch (AppsJsonException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
            }
        }

        /// <summary>Class for testing JSON serialization error.</summary>
        private class BadJsonClass
        {
            /// <summary>Gets Test member</summary>
            public string Dummy 
            {
                get { throw new ArgumentException("Dummy test exception"); }  
            }
        }
    }
}