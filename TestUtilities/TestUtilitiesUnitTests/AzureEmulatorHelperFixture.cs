//-----------------------------------------------------------------------
// <copyright file="AzureEmulatorHelperFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
