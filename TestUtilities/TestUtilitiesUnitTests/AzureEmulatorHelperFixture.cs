//-----------------------------------------------------------------------
// <copyright file="AzureEmulatorHelperFixture.cs" company="Rare Crowds Inc">
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace TestUtilitiesUnitTests
{
    /// <summary>
    /// Unit tests for the AzureEmulatorHelper
    /// </summary>
    [TestClass]
    public class AzureEmulatorHelperFixture
    {
        /// <summary>Running an executable that will succeed</summary>
        [TestMethod]
        public void RunExecutableSuccess()
        {
            var comspec = Environment.GetEnvironmentVariable("ComSpec");
            AzureEmulatorHelper.StartCommandLineToolAndWaitForExit(
                comspec,
                " /c dir \\");
        }

        /// <summary>Running an executable that will fail</summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void RunExecutableFailure()
        {
            var comspec = Environment.GetEnvironmentVariable("ComSpec");
            AzureEmulatorHelper.StartCommandLineToolAndWaitForExit(
                comspec,
                " /c dir /!");
        }
    }
}
