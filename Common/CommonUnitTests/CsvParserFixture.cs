//-----------------------------------------------------------------------
// <copyright file="CsvParserFixture.cs" company="Rare Crowds Inc">
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

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Serialization;

namespace CommonUnitTests
{
    /// <summary>Unit tests for the CSV parser</summary>
    [TestClass]
    public class CsvParserFixture
    {
        /// <summary>Test parsing a CSV</summary>
        [TestMethod]
        public void ParseSimpleCsv()
        {
            const string CsvData =
@"Column 1,Column 2, Column 3 ,Column 4
this, that, the,other
it,itself,them,themselves";

            var values = CsvParser.Parse(CsvData);
            Assert.IsNotNull(values);
            Assert.AreEqual(2, values.Count());

            var expectedKeys = new[] { "Column 1", "Column 2", "Column 3", "Column 4" };
            Assert.IsTrue(values.First().Keys.SequenceEqual(expectedKeys));

            var record = values.First();
            Assert.AreEqual("this", record["Column 1"]);
            Assert.AreEqual("that", record["Column 2"]);
            Assert.AreEqual("the", record["Column 3"]);
            Assert.AreEqual("other", record["Column 4"]);

            record = values.Last();
            Assert.AreEqual("it", record["Column 1"]);
            Assert.AreEqual("itself", record["Column 2"]);
            Assert.AreEqual("them", record["Column 3"]);
            Assert.AreEqual("themselves", record["Column 4"]);
        }

        /// <summary>Test parsing a CSV</summary>
        [TestMethod]
        public void ParseCsvWithQuotedValues()
        {
            const string CsvData =
@"Column 1,Column 2, Column 3 ,Column 4
this, ""Redmond, WA"", ""the"",""other
it,""New York, NY"" ,""\""them"",themselves";

            var values = CsvParser.Parse(CsvData);
            Assert.IsNotNull(values);
            Assert.AreEqual(2, values.Count());

            var expectedKeys = new[] { "Column 1", "Column 2", "Column 3", "Column 4" };
            Assert.IsTrue(values.First().Keys.SequenceEqual(expectedKeys));

            var record = values.First();
            Assert.AreEqual(@"this", record["Column 1"]);
            Assert.AreEqual(@"Redmond, WA", record["Column 2"]);
            Assert.AreEqual(@"the", record["Column 3"]);
            Assert.AreEqual(@"other", record["Column 4"]);

            record = values.Last();
            Assert.AreEqual(@"it", record["Column 1"]);
            Assert.AreEqual(@"New York, NY", record["Column 2"]);
            Assert.AreEqual(@"""them", record["Column 3"]);
            Assert.AreEqual(@"themselves", record["Column 4"]);
        }
    }
}
