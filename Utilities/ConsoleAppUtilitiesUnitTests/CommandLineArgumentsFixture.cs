using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using ConsoleAppUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConsoleAppUtilitiesUnitTests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class CommandLineArgumentsFixture
    {
        /// <summary>Minimal smoke test for CommandLineArguments</summary>
        [TestMethod]
        public void SmokeTest()
        {
            var args = @"-b -s SomeString -i 42 -d 98.6 -dt 2012/2/12".Split(new[] { ' ' });
            var testArgs = CommandLineArguments.Create<TestArgs>(args);
            Assert.AreEqual(true, testArgs.TestBool);
            Assert.AreEqual(42, testArgs.TestInteger);
            Assert.AreEqual(98.6, testArgs.TestDouble);
            Assert.AreEqual(new DateTime(2012, 2, 12), testArgs.TestDateTime);
            Assert.AreEqual("SomeString", testArgs.TestString);
        }

        /// <summary>Test default argument value</summary>
        [TestMethod]
        public void DefaultArgumentValue()
        {
            var testArgs = CommandLineArguments.Create<TestArgs>(new string[0]);
            Assert.AreEqual(new DateTime(1955, 11, 5, 17, 7, 0), testArgs.TestDateTime);
        }

        /// <summary>Test default argument value</summary>
        [TestMethod]
        public void DefaultArgumentFromAppSetting()
        {
            var testArgs = CommandLineArguments.Create<TestArgs>(new string[0]);
            Assert.AreEqual(3.14159, testArgs.TestDouble);
        }

        /// <summary>CommandLineArguments class for testing</summary>
        private class TestArgs : CommandLineArguments
        {
            /// <summary>Boolean test property</summary>
            [CommandLineArgument("-b", "Test Boolean")]
            public bool TestBool { get; set; }

            /// <summary>String test property</summary>
            [CommandLineArgument("-s", "Test String")]
            public string TestString { get; set; }

            /// <summary>Integer test property</summary>
            [CommandLineArgument("-i", "Test Int")]
            public int TestInteger { get; set; }

            /// <summary>Double test property</summary>
            [CommandLineArgument("-d", "Test Double", DefaultAppSetting = "DefaultTestDouble")]
            public double TestDouble { get; set; }

            /// <summary>DateTime test property</summary>
            [CommandLineArgument("-dt", "Test DateTime", DefaultValue = "1955-11-05T17:07:00")]
            public DateTime TestDateTime { get; set; }

            /// <summary>
            /// Gets a value indicating whether the arguments are valid
            /// </summary>
            public override bool ArgumentsValid
            {
                get { return true; }
            }
        }
    }
}
