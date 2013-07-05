//-----------------------------------------------------------------------
// <copyright file="SqlExceptionFactoryFixture.cs" company="Rare Crowds Inc">
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

using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace TestUtilitiesUnitTests
{
    /// <summary>
    /// Unit tests for the SqlExceptionFactory
    /// </summary>
    [TestClass]
    public class SqlExceptionFactoryFixture
    {
        /// <summary>Test creating a SqlException</summary>
        [TestMethod]
        [ExpectedException(typeof(SqlException))]
        public void Create()
        {
            const string ExpectedExceptionMessage = "Exception Message";
            const byte ExpectedErrorClass = 16;
            const int ExpectedLineNumber = 123;
            const string ExpectedErrorMessage = "Error Message";
            const int ExpectedErrorNumber = 50001;
            const string ExpectedProcedure = "sp_Something";
            const string ExpectedServer = @".\\SQLExpress";
            const byte ExpectedState = 42;

            var sqlException = SqlExceptionFactory.Create(
                    ExpectedExceptionMessage,
                    ExpectedErrorClass,
                    ExpectedLineNumber,
                    ExpectedErrorMessage,
                    ExpectedErrorNumber,
                    ExpectedProcedure,
                    ExpectedServer,
                    ExpectedState);

            Assert.IsNotNull(sqlException);
            Assert.AreEqual(ExpectedExceptionMessage, sqlException.Message);
            Assert.IsTrue(sqlException.StackTrace.Contains(this.GetType().FullName));
            Assert.AreEqual(1, sqlException.Errors.Count);
            var error = sqlException.Errors[0];
            Assert.AreEqual(ExpectedErrorClass, error.Class);
            Assert.AreEqual(ExpectedLineNumber, error.LineNumber);
            Assert.AreEqual(ExpectedErrorMessage, error.Message);
            Assert.AreEqual(ExpectedProcedure, error.Procedure);
            Assert.AreEqual(ExpectedServer, error.Server);
            Assert.AreEqual(ExpectedState, error.State);

            throw sqlException;
        }
    }
}
