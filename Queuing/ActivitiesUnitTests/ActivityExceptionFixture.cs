// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ActivityExceptionFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using Activities;
using CommonUnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ActivityUnitTests
{
    /// <summary>Test fixture for Activity Exception classes</summary>
    [TestClass]
    public class ActivityExceptionFixture
    {
        /// <summary>Test default exception.</summary>
        [TestMethod]
        public void TestBaseConstructorsDataAccessException()
        {
            var e = new ActivityException();
            Assert.AreEqual(ActivityErrorId.None, e.ActivityErrorId);
            AppsGenericExceptionFixture.AssertAppsException(e);

            e = new ActivityException("the message");
            Assert.AreEqual(ActivityErrorId.None, e.ActivityErrorId);
            AppsGenericExceptionFixture.AssertAppsException(e);
            
            var inner = new InvalidOperationException("the inner message");
            e = new ActivityException("the message", inner);
            Assert.AreEqual(ActivityErrorId.None, e.ActivityErrorId);
            AppsGenericExceptionFixture.AssertAppsException(e);
        }

        /// <summary>Test we can set error id.</summary>
        [TestMethod]
        public void ErrorIdActivityException()
        {
            // message and error id
            var e = new ActivityException(ActivityErrorId.InvalidJson, "not default");
            Assert.AreEqual(ActivityErrorId.InvalidJson, e.ActivityErrorId);
            var roundTripException = AppsGenericExceptionFixture.GetRoundtripException(e);
            Assert.AreEqual(e.ActivityErrorId, roundTripException.ActivityErrorId);

            // message and error id and inner exception
            var inner = new InvalidOperationException("the message");
            e = new ActivityException(ActivityErrorId.InvalidJson, "not default", inner);
            Assert.AreEqual(ActivityErrorId.InvalidJson, e.ActivityErrorId);
            roundTripException = AppsGenericExceptionFixture.GetRoundtripException(e);
            Assert.AreEqual(e.ActivityErrorId, roundTripException.ActivityErrorId);
        }

        /// <summary>Test we can serialize the exception.</summary>
        [TestMethod]
        public void SerializeExceptionActivityException()
        {
            var inner = new InvalidOperationException("the message");
            var e = new ActivityException(ActivityErrorId.InvalidJson, "not default", inner);

            var stream = new MemoryStream();
            var formatter = new SoapFormatter(null, new StreamingContext(StreamingContextStates.Other));
            formatter.Serialize(stream, e);
            stream.Seek(0, SeekOrigin.Begin);
            var roundTripException = formatter.Deserialize(stream) as ActivityException;
            
            Assert.IsNotNull(roundTripException);
            Assert.AreEqual(e.ActivityErrorId, roundTripException.ActivityErrorId);
            Assert.AreEqual(e.Message, roundTripException.Message);
            Assert.AreEqual(typeof(InvalidOperationException), roundTripException.InnerException.GetType());
        }
    }
}
