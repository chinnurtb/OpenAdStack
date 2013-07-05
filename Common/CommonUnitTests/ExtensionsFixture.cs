//-----------------------------------------------------------------------
// <copyright file="ExtensionsFixture.cs" company="Rare Crowds Inc">
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities;

namespace CommonUnitTests
{
    /// <summary>Unit tests for extensions</summary>
    [TestClass]
    public class ExtensionsFixture
    {
        /// <summary>Test for uint.CountSetBits()</summary>
        [TestMethod]
        public void BitCountTest()
        {
            var tests = new uint[][]
            {
                new uint[] { 0, 0x00000000 },
                new uint[] { 1, 0x00000001 },
                new uint[] { 1, 0x00000002 },
                new uint[] { 2, 0x00000003 },
                new uint[] { 2, 0x00003000 },
                new uint[] { 4, 0x0000000F },
                new uint[] { 4, 0x000000F0 },
                new uint[] { 4, 0x00000F00 },
                new uint[] { 4, 0x0000F000 },
                new uint[] { 8, 0x000000FF },
                new uint[] { 8, 0x000FF000 },
                new uint[] { 8, 0x0FF00000 },
                new uint[] { 16, 0xF0F0F0F0 },
                new uint[] { 32, 0xFFFFFFFF },
            };

            foreach (var set in tests)
            {
                var expected = (int)set[0];
                var number = set[1];
                var actual = number.CountSetBits();
                Assert.AreEqual(expected, actual, "{0:X8}".FormatInvariant(number));
            }
        }

        /// <summary>
        /// Test the string.ToStringInvariant(params object[] args) extension
        /// </summary>
        [TestMethod]
        public void FormatInvariant()
        {
            const string Format = @"
Test {0} this {1} extension {2} that {3} alleviates {4} the annoyance {5} of constantly {6}
using string.Format(CultureInfo.InvariantCulture, string, params object[] args)";
            object[] values = new object[]
            {
                "foo",
                null,
                42,
                Guid.NewGuid(),
                DateTime.UtcNow,
                Math.PI,
                float.MaxValue
            };

            var expected = string.Format(CultureInfo.InvariantCulture, Format, values);
            var result = Format.FormatInvariant(values);
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Test the string.Left extension
        /// </summary>
        [TestMethod]
        public void StringLeft()
        {
            const string Text = "Hello world";
            const string Expected = "Hello";
            var substring = Text.Left(Expected.Length);
            Assert.AreEqual(Expected, substring);
            Assert.AreNotSame(Text, substring);
        }

        /// <summary>
        /// Test the string.Left extension with length longer than source string
        /// </summary>
        [TestMethod]
        public void StringLeftLongerThanLength()
        {
            const string Text = "Hello world";
            var substring = Text.Left(Text.Length * 2);
            Assert.AreEqual(Text, substring);
            Assert.AreSame(Text, substring);
        }

        /// <summary>
        /// Test the string.Left extension with length zero
        /// </summary>
        [TestMethod]
        public void StringLeftZero()
        {
            const string Text = "Hello world";
            var substring = Text.Left(0);
            Assert.AreEqual(string.Empty, substring);
        }

        /// <summary>
        /// Test the string.Right extension
        /// </summary>
        [TestMethod]
        public void StringRight()
        {
            const string Text = "Hello world";
            const string Expected = "world";
            var substring = Text.Right(Expected.Length);
            Assert.AreEqual(Expected, substring);
            Assert.AreNotSame(Text, substring);
        }

        /// <summary>
        /// Test the string.Right extension with length longer than source string
        /// </summary>
        [TestMethod]
        public void StringRightLongerThanLength()
        {
            const string Text = "Hello world";
            var substring = Text.Right(Text.Length * 2);
            Assert.AreEqual(Text, substring);
            Assert.AreSame(Text, substring);
        }

        /// <summary>
        /// Test the string.Right extension with length zero
        /// </summary>
        [TestMethod]
        public void StringRightZero()
        {
            const string Text = "Hello world";
            var substring = Text.Right(0);
            Assert.AreEqual(string.Empty, substring);
        }

        /// <summary>
        /// Test the IDictionary&lt;string, int&gt;.ToString() extension
        /// </summary>
        [TestMethod]
        public void IDictionaryStringIntToString()
        {
            const string Expected = "[\n\tfoo=0,\n\tbar=42\n]";
            var dictionary = new Dictionary<string, int>
            {
                { "foo", 0 },
                { "bar", 42 }
            };

            var result = dictionary.ToString<string, int>();
            Assert.AreEqual(Expected, result);
        }

        /// <summary>
        /// Test the IDictionary&lt;int, int&gt;.ToString() extension
        /// </summary>
        [TestMethod]
        public void IDictionaryIntIntToString()
        {
            const string Expected = "[\n\t0=25,\n\t57=42\n]";
            var dictionary = new Dictionary<int, int>
            {
                { 0, 25 },
                { 57, 42 }
            };

            var result = dictionary.ToString<int, int>();
            Assert.AreEqual(Expected, result);
        }

        /// <summary>
        /// Test the IDictionary&lt;int, string&gt;.ToString() extension with null values
        /// </summary>
        [TestMethod]
        public void IDictionaryIntStringToStringWithNullValues()
        {
            const string Expected = "[\n\t0=foo,\n\t57=,\n\t42=,\n\t9=bar\n]";
            var dictionary = new Dictionary<int, string>
            {
                { 0, "foo" },
                { 57, null },
                { 42, null },
                { 9, "bar" }
            };

            var result = dictionary.ToString<int, string>();
            Assert.AreEqual(Expected, result);
        }

        /// <summary>
        /// Roundtrip test of the byte[].Deflate() and byte[].Inflate() extensions
        /// </summary>
        [TestMethod]
        public void ByteArrayDeflateInflateRoundtrip()
        {
            const int Count = 4096;
            var bytes = new byte[Count];
            new Random().NextBytes(bytes);

            var compressed = bytes.Deflate();
            var decompressed = compressed.Inflate();

            Assert.AreEqual(bytes.Length, decompressed.Length);
            for (int i = 0; i < bytes.Length; i++)
            {
                Assert.AreEqual(bytes[i], decompressed[i], "Byte {0} of {1} did not match".FormatInvariant(i, bytes.Length));
            }
        }

        /// <summary>
        /// Zip two sequences into a sequence of tuples
        /// </summary>
        [TestMethod]
        public void DefaultZipToTuples()
        {
            var random = new Random();
            var first = new[]
            {
                random.Next(), random.Next(), random.Next(), random.Next(), 
                random.Next(), random.Next(), random.Next(), random.Next(), 
                random.Next(), random.Next(), random.Next(), random.Next(), 
                random.Next(), random.Next(), random.Next(), random.Next(), 
            };
            var second = new[]
            {
                random.Next(), random.Next(), random.Next(), random.Next(), 
                random.Next(), random.Next(), random.Next(), random.Next(), 
                random.Next(), random.Next(), random.Next(), random.Next(), 
                random.Next(), random.Next(), random.Next(), random.Next(), 
            };
            Assert.AreEqual(first.Length, second.Length);

            var zipped = first.Zip(second);

            Assert.AreEqual(first.Length, zipped.Count());
            int i = 0;
            foreach (var tuple in zipped)
            {
                Assert.AreEqual(first[i], tuple.Item1);
                Assert.AreEqual(second[i], tuple.Item2);
                i++;
            }
        }

        /// <summary>
        /// Convert an enumerable to a dictionary
        /// </summary>
        [TestMethod]
        public void IEnumerationOfKeyValuePairToDictionary()
        {
            var random = new Random();
            var expected = new Dictionary<string, int>
            {
                { Guid.NewGuid().ToString(), random.Next() },
                { Guid.NewGuid().ToString(), random.Next() },
                { Guid.NewGuid().ToString(), random.Next() }
            };
            var enumerable = expected as IEnumerable<KeyValuePair<string, int>>;

            var result = enumerable.ToDictionary();

            Assert.IsInstanceOfType(result, typeof(IDictionary<string, int>));
            Assert.AreEqual(expected.Count, result.Count);
            foreach (var pair in expected.Zip(result))
            {
                Assert.AreEqual(pair.Item1.Key, pair.Item2.Key);
                Assert.AreEqual(pair.Item1.Value, pair.Item2.Value);
            }
        }

        /// <summary>
        /// Add values from source sequence to destination list
        /// </summary>
        [TestMethod]
        public void AddSequenceToList()
        {
            var source = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            var initialValues = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var destination = new List<Guid>(initialValues);
            var result = destination.Add(source);
            Assert.AreSame(destination, result);
            Assert.AreEqual(source.Length + initialValues.Length, result.Count);

            foreach (var initialValue in initialValues)
            {
                Assert.IsTrue(result.Contains(initialValue));
            }
            
            foreach (var sourceValue in source)
            {
                Assert.IsTrue(result.Contains(sourceValue));
            }
        }

        /// <summary>
        /// Get a distinct sequence using a custom lambda comparer
        /// </summary>
        [TestMethod]
        public void LambdaEqualityComparerDistinct()
        {
            var values = new[]
            {
                new Tuple<int, int>(1, 2),
                new Tuple<int, int>(0, 3),
                new Tuple<int, int>(2, 3),
                new Tuple<int, int>(1, 4),
                new Tuple<int, int>(1, 7),
            };

            var distinct = values
                .Distinct(value => value.Item1 + value.Item2)
                .ToArray();

            Assert.AreEqual(3, distinct.Length);
        }

        /// <summary>
        /// Add values from one dictionary to another (no overlap)
        /// </summary>
        [TestMethod]
        public void AddDictionaries()
        {
            var setA = new[] { "Alpha", "Beta", "Gamma" }.ToDictionary(key => key, key => Guid.NewGuid());
            var setB = new[] { "Chi", "Psi", "Omega" }.ToDictionary(key => key, key => Guid.NewGuid());

            var dictionary = new Dictionary<string, Guid>(setA);
            dictionary.Add(setB, false);

            Assert.AreEqual(setA.Count + setB.Count - setA.Intersect(setB).Count(), dictionary.Count);

            Assert.AreEqual(setA["Alpha"], dictionary["Alpha"]);
            Assert.AreEqual(setA["Beta"], dictionary["Beta"]);
            Assert.AreEqual(setA["Gamma"], dictionary["Gamma"]);
            Assert.AreEqual(setB["Chi"], dictionary["Chi"]);
            Assert.AreEqual(setB["Psi"], dictionary["Psi"]);
            Assert.AreEqual(setB["Omega"], dictionary["Omega"]);
        }

        /// <summary>
        /// Add values from one dictionary to another (overlap, overwite)
        /// </summary>
        [TestMethod]
        public void AddDictionariesOverwrite()
        {
            var setA = new[] { "Alpha", "Beta", "Gamma", "Sigma", "Tau" }.ToDictionary(key => key, key => Guid.NewGuid());
            var setB = new[] { "Tau", "Sigma", "Chi", "Psi", "Omega" }.ToDictionary(key => key, key => Guid.NewGuid());

            var dictionary = new Dictionary<string, Guid>(setA);
            dictionary.Add(setB, true);

            var expectedCount = setA.Keys.Count
                              + setB.Keys.Count
                              - setA.Keys.Intersect(setB.Keys).Count();
            Assert.AreEqual(expectedCount, dictionary.Count);

            Assert.AreEqual(setA["Alpha"], dictionary["Alpha"]);
            Assert.AreEqual(setA["Beta"], dictionary["Beta"]);
            Assert.AreEqual(setA["Gamma"], dictionary["Gamma"]);
            Assert.AreNotEqual(setA["Sigma"], dictionary["Sigma"]);
            Assert.AreNotEqual(setA["Tau"], dictionary["Tau"]);
            Assert.AreEqual(setB["Sigma"], dictionary["Sigma"]);
            Assert.AreEqual(setB["Tau"], dictionary["Tau"]);
            Assert.AreEqual(setB["Chi"], dictionary["Chi"]);
            Assert.AreEqual(setB["Psi"], dictionary["Psi"]);
            Assert.AreEqual(setB["Omega"], dictionary["Omega"]);
        }

        /// <summary>
        /// Add values from one dictionary to another (overlap, no overwrite)
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddDictionariesNoOverwriteError()
        {
            var setA = new[] { "Alpha", "Beta", "Gamma", "Sigma", "Tau" }.ToDictionary(key => key, key => Guid.NewGuid());
            var setB = new[] { "Tau", "Sigma", "Chi", "Psi", "Omega" }.ToDictionary(key => key, key => Guid.NewGuid());

            var dictionary = new Dictionary<string, Guid>(setA);
            dictionary.Add(setB, false);
        }

        /// <summary>
        /// Test that the default IDictionary.Add overload defaults to overwrite
        /// </summary>
        [TestMethod]
        public void AddDictionariesOverwriteDefault()
        {
            var setA = new[] { "Alpha", "Beta", "Gamma", "Sigma", "Tau" }.ToDictionary(key => key, key => Guid.NewGuid());
            var setB = new[] { "Tau", "Sigma", "Chi", "Psi", "Omega" }.ToDictionary(key => key, key => Guid.NewGuid());

            var dictionary = new Dictionary<string, Guid>(setA);
            dictionary.Add(setB);

            var expectedCount = setA.Keys.Count
                              + setB.Keys.Count
                              - setA.Keys.Intersect(setB.Keys).Count();
            Assert.AreEqual(expectedCount, dictionary.Count);
        }

        /// <summary>Test converting a DateTime to a dictionary</summary>
        [TestMethod]
        public void DateTimeToDictionary()
        {
            var now = DateTime.UtcNow;
            var nowValues = now.ToDictionaryValues();

            Assert.AreEqual(now.Year, nowValues["Year"]);
            Assert.AreEqual(now.Month, nowValues["Month"]);
            Assert.AreEqual(now.Day, nowValues["Day"]);
            Assert.AreEqual(now.Hour, nowValues["Hour"]);
            Assert.AreEqual(now.Minute, nowValues["Minute"]);
            Assert.AreEqual(now.Second, nowValues["Second"]);
            Assert.AreEqual(now.Millisecond, nowValues["Millisecond"]);
            Assert.AreEqual(now.DayOfWeek, nowValues["DayOfWeek"]);
            Assert.AreEqual(now.DayOfYear, nowValues["DayOfYear"]);
            Assert.AreEqual(now.Kind, nowValues["Kind"]);
            Assert.AreEqual(now.Ticks, nowValues["Ticks"]);
            Assert.AreEqual(now.TimeOfDay, nowValues["TimeOfDay"]);
            Assert.AreEqual(now.Date, nowValues["Date"]);
        }

        /// <summary>Smoke test for object.ToDictionary</summary>
        [TestMethod]
        public void ObjectToDictionary()
        {
            var r = new Random();
            var obj = new ToDictionaryTestType
            {
                Id = Guid.NewGuid().ToString("N"),
                Value = r.Next(),
                AnotherValue = (decimal)r.NextDouble(),
                YetAnotherValue = r.NextDouble()
            };

            var dict = obj.ToDictionaryValues();
            Assert.IsNotNull(dict);
            Assert.AreEqual(4, dict.Count);
        }

        /// <summary>Class for testing object.ToDictionary</summary>
        private class ToDictionaryTestType
        {
            /// <summary>Gets or sets an identifier</summary>
            public string Id { get; set; }

            /// <summary>Gets or sets a value</summary>
            public int Value { get; set; }

            /// <summary>Gets or sets another value</summary>
            public decimal AnotherValue { get; set; }

            /// <summary>Gets or sets yet another value</summary>
            public double YetAnotherValue { get; set; }
        }
    }
}
