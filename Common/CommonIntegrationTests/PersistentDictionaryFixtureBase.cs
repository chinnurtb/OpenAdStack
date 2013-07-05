//-----------------------------------------------------------------------
// <copyright file="PersistentDictionaryFixtureBase.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using AzureUtilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using TestUtilities;
using Utilities.Storage;

namespace CommonIntegrationTests
{
    /// <summary>
    /// Base class containing tests for IPersistentDictionary implementations.
    /// </summary>
    [TestClass]
    public abstract class PersistentDictionaryFixtureBase
    {
        /// <summary>Lorem Ipsum test content</summary>
        private const string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";

        /// <summary>Test setting a value</summary>
        [TestMethod]
        public void SetValue()
        {
            var dictionary = this.CreateTestDictionary<TestValueType>();
            var value = CreateTestValue();
            var key = CreateTestKey();

            dictionary[key] = value;

            this.AssertPersistentStoreCreated();
            this.AssertValuePersisted(key);
        }
        
        /// <summary>
        /// Create a dictionary with a primative string type and
        /// round-trip an serialized value through it.
        /// </summary>
        [TestMethod]
        public void RoundtripDictionaryForStringType()
        {
            var dictionary = this.CreateTestDictionary<string>();
            Assert.IsNotNull(dictionary);
            this.AssertPersistentStoreCreated();
            AssertRoundtripValueEqual<string>(dictionary, Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Create a dictionary with a primative double type and
        /// round-trip an serialized value through it.
        /// </summary>
        [TestMethod]
        public void RoundtripDictionaryForDoubleType()
        {
            var dictionary = this.CreateTestDictionary<double>();
            Assert.IsNotNull(dictionary);
            this.AssertPersistentStoreCreated();
            AssertRoundtripValueEqual<double>(dictionary, 3.14159265);
        }

        /// <summary>
        /// Create a dictionary with a known data contract type (DateTime) and
        /// round-trip an serialized value through it.
        /// </summary>
        [TestMethod]
        public void RoundtripDictionaryForKnownDateTimeType()
        {
            var dictionary = this.CreateTestDictionary<DateTime>();
            Assert.IsNotNull(dictionary);
            this.AssertPersistentStoreCreated();
            AssertRoundtripValueEqual<DateTime>(dictionary, DateTime.UtcNow);
        }

        /// <summary>Create a dictionary with an invalid, non-DataContract value type</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidDataContractException))]
        public void RoundtripDictionaryForInvalidDataContractType()
        {
            var dictionary = this.CreateTestDictionary<NonDataContractValueType>();
            var expected = new NonDataContractValueType(42);
            var key = CreateTestKey();

            Assert.IsNotNull(dictionary);
            dictionary[key] = expected;
        }

        /// <summary>Test setting a value and retrieving it</summary>
        [TestMethod]
        public void RoundtripDictionaryForValidDataContractType()
        {
            var dictionary = this.CreateTestDictionary<TestValueType>();
            var expected = CreateTestValue();
            var key = CreateTestKey();

            dictionary[key] = expected;
            var value = dictionary[key];

            Assert.IsNotNull(value);
            Assert.AreNotSame(expected, value);
            Assert.AreEqual(expected.IntegerValue, value.IntegerValue);
            Assert.AreEqual(expected.StringValue, value.StringValue);
            Assert.AreNotSame(expected.StringValue, value.StringValue);
        }

        /// <summary>Test getting a value that does not exist</summary>
        [TestMethod]
        [ExpectedException(typeof(KeyNotFoundException))]
        public void GetNonexistentValue()
        {
            var dictionary = this.CreateTestDictionary<TestValueType>();
            var key = CreateTestKey();

            var invalid = dictionary[key];
        }

        /// <summary>Tests handling of two dictionary instances accessing the same blob container</summary>
        [TestMethod]
        public void TestVersionConcurrencyHandling()
        {
            var dictionaryA = this.CreateTestDictionary<TestValueType>();
            var dictionaryB = this.CreateTestDictionary<TestValueType>();
            var key = CreateTestKey();
            var value = CreateTestValue();
            
            // Valid set, dictionaryA has no ETag for key yet and gets the new ETag after the blob is written
            dictionaryA[key] = value;

            // Valid set, dictionaryA has the current ETag for key
            dictionaryA[key] = value;

            // Valid set again, dictionaryA still has the current ETag for key
            dictionaryA[key] = value;

            // dictionaryB gets/sets behind dictionaryA's back which updates the ETag
            value = dictionaryB[key];
            dictionaryB[key] = value;

            try
            {
                // Invalid set, dictionaryA's ETag for key is outdated
                dictionaryA[key] = value;
                Assert.Fail("Expected InvalidOperationException was not thrown.");
            }
            catch (InvalidOperationException ioe)
            {
                Assert.IsTrue(ioe.Message.Contains("ETag"), "InvalidOperationException did not have expected message");
            }
        }

        /// <summary>Test enumerating the dictionary with linq</summary>
        [TestMethod]
        public void EnumerateWithLinq()
        {
            var dictionary = this.CreateTestDictionary<TestValueType>();
            var values = new TestValueType[]
            {
                new TestValueType { StringValue = "foo", IntegerValue = 137 },
                new TestValueType { StringValue = "bar", IntegerValue = -137 },
                new TestValueType { StringValue = "oop", IntegerValue = 42 },
                new TestValueType { StringValue = "ack", IntegerValue = -42 },
                new TestValueType { StringValue = "fnarb", IntegerValue = 999 },
            };

            foreach (var value in values)
            {
                dictionary.Add(value.StringValue, value);
            }

            Assert.AreEqual(5, dictionary.Count);
            Assert.AreEqual(3, dictionary.Where(kvp => kvp.Value.IntegerValue > 0).Count());
            Assert.AreEqual(2, dictionary.Values.Where(v => v.IntegerValue < 0).Count());
            Assert.AreEqual(4, dictionary.Where(kvp => kvp.Key.Length == 3).Count());
            Assert.AreEqual(1, dictionary.Keys.Where(k => k.Length > 3).Count());
            Assert.AreEqual(999, dictionary.Values.Sum(v => v.IntegerValue));
        }

        /// <summary>
        /// Test roundtripping an entity larger than the compression threshold and then
        /// replacing it with a non-compressed value
        /// </summary>
        [TestMethod]
        public void CompressedEntries()
        {
            TestValueType result = null;
            var compressionThreshold = AbstractPersistentDictionary<object>.CompressionThresholdBytes;
            var compressedExpected = new TestValueType
            {
                IntegerValue = LoremIpsum.Length,
                StringValue = LoremIpsum
            };
            while (compressedExpected.StringValue.Length < compressionThreshold * 2)
            {
                compressedExpected.StringValue += LoremIpsum;
            }

            var uncompressedExpected = new TestValueType
            {
                IntegerValue = 42,
                StringValue = "Don't Panic"
            };

            var dictionary = this.CreateTestDictionary<TestValueType>();
            
            // Round-trip a value that should get compressed
            dictionary["LoremIpsum"] = compressedExpected;
            result = dictionary["LoremIpsum"] as TestValueType;
            Assert.IsNotNull(result);
            Assert.AreEqual(compressedExpected.IntegerValue, result.IntegerValue);
            Assert.IsNotNull(result.StringValue);
            Assert.AreEqual(compressedExpected.StringValue.Length, result.StringValue.Length);
            Assert.AreEqual(0, string.CompareOrdinal(compressedExpected.StringValue, result.StringValue));

            // Round-trip a value that should NOT get compressed using the same dictionary entry
            dictionary["LoremIpsum"] = uncompressedExpected;
            result = dictionary["LoremIpsum"] as TestValueType;
            Assert.IsNotNull(result);
            Assert.AreEqual(uncompressedExpected.IntegerValue, result.IntegerValue);
            Assert.IsNotNull(result.StringValue);
            Assert.AreEqual(uncompressedExpected.StringValue.Length, result.StringValue.Length);
            Assert.AreEqual(0, string.CompareOrdinal(uncompressedExpected.StringValue, result.StringValue));

            // Round-trip a value that SHOULD get compressed again using the same dictionary entry
            dictionary["LoremIpsum"] = compressedExpected;
            result = dictionary["LoremIpsum"] as TestValueType;
            Assert.IsNotNull(result);
            Assert.AreEqual(compressedExpected.IntegerValue, result.IntegerValue);
            Assert.IsNotNull(result.StringValue);
            Assert.AreEqual(compressedExpected.StringValue.Length, result.StringValue.Length);
            Assert.AreEqual(0, string.CompareOrdinal(compressedExpected.StringValue, result.StringValue));
        }

        /// <summary>Round-trip test of raw mode</summary>
        [TestMethod]
        public void RoundtripRaw()
        {
            var dictionary = this.CreateTestDictionary<byte[]>(true);
            dictionary["LoremIpsum"] = Encoding.UTF8.GetBytes(LoremIpsum);
            var bytes = dictionary["LoremIpsum"];
            var result = Encoding.UTF8.GetString(bytes);
            Assert.AreEqual(LoremIpsum, result);
        }

        /// <summary>Round-trip a very large value</summary>
        [TestMethod]
        public void RoundtripLarge()
        {
            const int LongLength = 52428800;
            byte[] longLorem = null;
            using (var stream = new MemoryStream(LongLength))
            {
                using (var writer = new StreamWriter(stream))
                {
                    while (stream.Length < LongLength)
                    {
                        writer.WriteLine(LoremIpsum);
                    }
                }

                longLorem = stream.GetBuffer();
            }

            var dictionary = this.CreateTestDictionary<byte[]>(true);
            dictionary["LargeLorem"] = longLorem;
            var result = dictionary["LargeLorem"];
            Assert.AreEqual(longLorem.Length, result.Length);
            foreach (var pair in longLorem.Zip(result))
            {
                Assert.AreEqual(pair.Item1, pair.Item2);
            }
        }

        /// <summary>Asserts the underlying store for the dictionary was created</summary>
        protected abstract void AssertPersistentStoreCreated();

        /// <summary>Asserts the value with the specified <paramref name="key"/> was persisted</summary>
        /// <param name="key">Key for the value</param>
        protected abstract void AssertValuePersisted(string key);

        /// <summary>Creates a new IPersistentDictionary for testing</summary>
        /// <param name="raw">Whether or not to skip serialization. Only valid for <typeparamref name="TValue"/> of byte[]</param>
        /// <returns>The IPersistentDictionary</returns>
        /// <typeparam name="TValue">Entry type to create the dictionary for</typeparam>
        protected abstract IPersistentDictionary<TValue> CreateTestDictionary<TValue>(bool raw);

        /// <summary>Creates a new IPersistentDictionary for testing</summary>
        /// <returns>The IPersistentDictionary</returns>
        /// <typeparam name="TValue">Entry type to create the dictionary for</typeparam>
        protected IPersistentDictionary<TValue> CreateTestDictionary<TValue>()
        {
            return this.CreateTestDictionary<TValue>(false);
        }

        /// <summary>Round trips a value through the dictionary and asserts it comes out equal</summary>
        /// <typeparam name="TValue">Type of the dictionary entries</typeparam>
        /// <param name="dictionary">The dictionary to round trip through</param>
        /// <param name="value">The value to round trip</param>
        private static void AssertRoundtripValueEqual<TValue>(IPersistentDictionary<TValue> dictionary, TValue value)
        {
            var key = CreateTestKey();
            dictionary[key] = value;
            var result = dictionary[key];
            Assert.AreEqual(value, result);
        }

        /// <summary>Creates a new, unique key value.</summary>
        /// <returns>The unique key value</returns>
        private static string CreateTestKey()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>Creates a new, unique instance of TestValueType.</summary>
        /// <returns>The unique test value</returns>
        private static TestValueType CreateTestValue()
        {
            return new TestValueType
            {
                IntegerValue = new Random().Next(),
                StringValue = Guid.NewGuid().ToString()
            };
        }

        /// <summary>Value type for testing</summary>
        [DataContract]
        private class TestValueType
        {
            /// <summary>Gets or sets the string value</summary>
            [DataMember]
            public string StringValue { get; set; }

            /// <summary>Gets or sets the int value</summary>
            [DataMember]
            public int IntegerValue { get; set; }
        }

        /// <summary>Invalid value type for testing</summary>
        /// <remarks>Cannot be used due to lack of a default constructor</remarks>
        private class NonDataContractValueType
        {
            /// <summary>Initializes a new instance of the NonDataContractValueType class.</summary>
            /// <param name="value">The value</param>
            public NonDataContractValueType(int value)
            {
                this.IntegerValue = value;
                this.StringValue = value.ToString();
            }

            /// <summary>Prevents a default instance of the NonDataContractValueType class from being created.</summary>
            private NonDataContractValueType()
            {
            }

            /// <summary>Gets or sets the string value</summary>
            public string StringValue { get; set; }

            /// <summary>Gets or sets the int value</summary>
            public int IntegerValue { get; set; }
        }
    }
}
