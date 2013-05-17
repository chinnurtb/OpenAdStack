// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataAccessExceptionFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using CommonUnitTests;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Test fixture for DAL Exception classes</summary>
    [TestClass]
    public class DataAccessExceptionFixture
    {
        /// <summary>Test DataAccessException.</summary>
        [TestMethod]
        public void TestBaseConstructorsDataAccessException()
        {
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessException());
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessException("the message"));
            var inner = new InvalidOperationException("the inner message");
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessException("the message", inner));
        }

        /// <summary>Test DataAccessEntityNotFoundException.</summary>
        [TestMethod]
        public void TestBaseConstructorsEntityNotFoundException()
        {
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessEntityNotFoundException());
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessEntityNotFoundException("the message"));
            var inner = new InvalidOperationException("the inner message");
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessEntityNotFoundException("the message", inner));
        }

        /// <summary>Test DataAccessTypeMismatchException.</summary>
        [TestMethod]
        public void TestBaseConstructorsTypeMismatchException()
        {
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessTypeMismatchException());
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessTypeMismatchException("the message"));
            var inner = new DataAccessTypeMismatchException("the inner message");
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessTypeMismatchException("the message", inner));
        }

        /// <summary>Test DataAccessStaleEntityException.</summary>
        [TestMethod]
        public void TestBaseConstructorsStaleEntityException()
        {
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessStaleEntityException());
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessStaleEntityException("the message"));
            var inner = new InvalidOperationException("the inner message");
            AppsGenericExceptionFixture.AssertAppsException(new DataAccessStaleEntityException("the message", inner));
        }
    }
}
