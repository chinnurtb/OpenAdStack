// -----------------------------------------------------------------------
// <copyright file="AppsJsonExceptionFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Serialization;

namespace CommonUnitTests
{
    /// <summary>
    /// Test fixture for AppsJsonException
    /// </summary>
    [TestClass]
    public class AppsJsonExceptionFixture
    {
        /// <summary>Test default exception.</summary>
        [TestMethod]
        public void TestBaseConstructors()
        {
            AppsGenericExceptionFixture.AssertAppsException(new AppsJsonException());
            AppsGenericExceptionFixture.AssertAppsException(new AppsJsonException("the message"));
            var inner = new InvalidOperationException("the inner message");
            AppsGenericExceptionFixture.AssertAppsException(new AppsJsonException("the message", inner));
        }
    }
}
