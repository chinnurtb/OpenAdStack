// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppsGenericExceptionFixture.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities;

namespace CommonUnitTests
{
    /// <summary>Test fixture for base exception class</summary>
    [TestClass]
    public class AppsGenericExceptionFixture
    {
        /// <summary>Exception to make basic assertions against.</summary>
        /// <param name="exception">The exception to test.</param>
        /// <typeparam name="T">The type of the exception.</typeparam>
        public static void AssertAppsException<T>(T exception) where T : AppsGenericException
        {
            AssertThrowDefault(exception);
            AssertRoundtripSerialization(exception);
        }
        
        /// <summary>Exception to make basic assertions against.</summary>
        /// <param name="exception">The exception to test.</param>
        /// <typeparam name="T">The type of the exception.</typeparam>
        /// <returns>The roundtrip-serialized exception.</returns>
        public static T GetRoundtripException<T>(T exception) where T : AppsGenericException
        {
            var stream = new MemoryStream();
            var formatter = new SoapFormatter(null, new StreamingContext(StreamingContextStates.Other));
            formatter.Serialize(stream, exception);
            stream.Seek(0, SeekOrigin.Begin);
            return formatter.Deserialize(stream) as T;
        }

        /// <summary>Test default exception.</summary>
        [TestMethod]
        public void TestBaseConstructors()
        {
            AssertAppsException(new AppsGenericException());
            AssertAppsException(new AppsGenericException("the message"));
            var inner = new InvalidOperationException("the inner message");
            AssertAppsException(new AppsGenericException("the message", inner));
            var e = new AppsGenericException();
            e.Data.Add("somedatakey", "somedatavalue");
            AssertAppsException(e);
        }

        /// <summary>Throw the exception and verify we catch it.</summary>
        /// <param name="exception">The exception to test.</param>
        /// <typeparam name="T">The type of the exception.</typeparam>
        private static void AssertThrowDefault<T>(T exception) where T : AppsGenericException
        {
            // Verify we catch the actual type of the exception
            try
            {
                throw exception;
            }
            catch (T e)
            {
                AssertExceptionsEqual(exception, e);
            }
            catch (Exception)
            {
                // Should never be here
                Assert.Fail();
            }

            // Verify we catch the base type for exceptions derived from it
            try
            {
                throw exception;
            }
            catch (AppsGenericException e)
            {
                AssertExceptionsEqual(exception, e);
            }
            catch (Exception)
            {
                // Should never be here
                Assert.Fail();
            }
        }

        /// <summary>Assert that exception serialization occurs.</summary>
        /// <param name="exception">The exception to serialize.</param>
        /// <typeparam name="T">The type of the exception.</typeparam>
        private static void AssertRoundtripSerialization<T>(T exception) where T : AppsGenericException
        {
            var roundTripException = GetRoundtripException(exception);
            AssertExceptionsEqual(exception, roundTripException);
        }

        /// <summary>Assert the members of the exception we care about are preserved.</summary>
        /// <param name="expectedException">The expected exception.</param>
        /// <param name="actualException">The acutal exception.</param>
        /// <typeparam name="T">The type of the exception.</typeparam>
        private static void AssertExceptionsEqual<T>(T expectedException, T actualException) where T : AppsGenericException
        {
            Assert.AreEqual(expectedException.Message, actualException.Message);
            if (expectedException.InnerException != null)
            {
                Assert.AreEqual(expectedException.InnerException.GetType(), actualException.InnerException.GetType());
            }
            else
            {
                Assert.IsNull(actualException.InnerException);
            }

            Assert.AreEqual(expectedException.Data.Count, actualException.Data.Count);
            foreach (var dataKey in expectedException.Data.Keys)
            {
                Assert.AreEqual(expectedException.Data[dataKey], actualException.Data[dataKey]);
            }
        }
    }
}
