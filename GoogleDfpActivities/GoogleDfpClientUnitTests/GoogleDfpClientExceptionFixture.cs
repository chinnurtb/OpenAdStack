//-----------------------------------------------------------------------
// <copyright file="GoogleDfpClientExceptionFixture.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Google.Api.Ads.Dfp.Lib;
using Google.Api.Ads.Dfp.v201206;
using GoogleDfpClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleDfpClientUnitTests
{
    /// <summary>Tests for the GoogleDfpClientException class</summary>
    [TestClass]
    public class GoogleDfpClientExceptionFixture
    {
        /// <summary>
        /// Test creating an exception with null parameters
        /// </summary>
        [TestMethod]
        public void CreateWithNulls()
        {
            var exception = new GoogleDfpClientException(null, null, null);
            Assert.IsNotNull(exception.Message);
        }

        /// <summary>
        /// Test creating an exception using only a message
        /// </summary>
        [TestMethod]
        public void CreateWithMessageOnly()
        {
            var exception = new GoogleDfpClientException("Message {0}", 42);
            Assert.AreEqual("Message 42", exception.Message);
            Assert.IsFalse(exception.IsApiException);
            Assert.IsNull(exception.ApiException);
        }

        /// <summary>
        /// Test creating an exception using only a DfpException
        /// </summary>
        [TestMethod]
        public void CreateWithDfpExceptionOnly()
        {
            var dfpExceptionMessage = Guid.NewGuid().ToString();
            var expectedMessage = "DfpException: " + dfpExceptionMessage;

            var dfpException = new DfpException(dfpExceptionMessage);
            var exception = new GoogleDfpClientException(dfpException);
            
            Assert.AreEqual(expectedMessage, exception.Message);
            Assert.AreSame(dfpException, exception.InnerException);
            Assert.IsFalse(exception.IsApiException);
            Assert.IsNotNull(exception.ApiErrors);
            Assert.AreEqual(0, exception.ApiErrors.Count());
        }

        /// <summary>
        /// Test creating an exception using a DfpException and a message
        /// </summary>
        [TestMethod]
        public void CreateWithMessageAndDfpException()
        {
            var dfpExceptionMessage = Guid.NewGuid().ToString();
            var exceptionMessage = Guid.NewGuid().ToString();
            var expectedMessage =
                exceptionMessage +
                GoogleDfpClientException.MessageSeparator +
                "DfpException: " + dfpExceptionMessage;

            var dfpException = new DfpException(dfpExceptionMessage);
            var exception = new GoogleDfpClientException(dfpException, exceptionMessage);

            Assert.AreEqual(expectedMessage, exception.Message);
            Assert.AreSame(dfpException, exception.InnerException);
            Assert.IsFalse(exception.IsApiException);
            Assert.IsNotNull(exception.ApiErrors);
            Assert.AreEqual(0, exception.ApiErrors.Count());
        }

        /// <summary>
        /// Test creating an exception using a DfpApiException and a message
        /// </summary>
        [TestMethod]
        public void CreateWithMessageAndDfpApiException()
        {
            var dfpApiExceptionMessage = Guid.NewGuid().ToString();
            var apiExceptionMessage = Guid.NewGuid().ToString();
            var exceptionMessage = Guid.NewGuid().ToString();
            var expectedMessageStart =
                exceptionMessage +
                GoogleDfpClientException.MessageSeparator +
                "DfpException: " + dfpApiExceptionMessage +
                GoogleDfpClientException.MessageSeparator +
                "ApiException: " + apiExceptionMessage;

            var apiErrors = new ApiError[]
            {
                new CommonError
                {
                    errorString = Guid.NewGuid().ToString()
                },
                new ParseError
                {
                    errorString = Guid.NewGuid().ToString()
                }
            };
            var dfpApiException = new DfpApiException(
                new ApiException
                {
                    message = apiExceptionMessage,
                    errors = apiErrors
                },
                dfpApiExceptionMessage);

            var exception = new GoogleDfpClientException(dfpApiException, exceptionMessage);

            Assert.IsNotNull(exception.Message);
            Assert.IsTrue(exception.Message.StartsWith(expectedMessageStart));
            Assert.AreSame(dfpApiException, exception.InnerException);
            Assert.IsTrue(exception.IsApiException);
            Assert.IsNotNull(exception.ApiErrors);
            Assert.AreEqual(apiErrors.Length, exception.ApiErrors.Count());
            Assert.IsTrue(apiErrors.All(e => exception.Message.Contains(e.errorString)));
        }
    }
}
