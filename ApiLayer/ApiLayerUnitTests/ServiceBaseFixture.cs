// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceBaseFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Activities;
using ApiLayer;
using Microsoft.Practices.Unity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using WorkItems;

namespace ApiLayerUnitTests
{
    using System.Net;

    /// <summary>Test fixture for the Service Base</summary>
    [TestClass]
    public class ServiceBaseFixture : ServiceBase
    {
        /// <summary>Activity result timeout</summary>
        private const long ActivityResultTimeout = 10;

        /// <summary>
        /// Tests if RunActivity responsds with expected result
        /// </summary>
        [TestMethod]
        public void RunActivityTest()
        {
            var resultWorkItem = new WorkItem();
            ConfigurationManager.AppSettings["ApiLayer.QueueResponsePollTime"] = "0";
            ConfigurationManager.AppSettings["ApiLayer.MaxQueueResponseWaitTime"] = ActivityResultTimeout.ToString(CultureInfo.InvariantCulture);
            IQueuer queuerMock = MockRepository.GenerateStub<IQueuer>();
            queuerMock.Stub(f => f.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), resultWorkItem).Dummy))
                .Return(true);
            queuerMock.Stub(f => f.CheckWorkItem(Arg<string>.Is.Anything));
            ServiceBase.Queuer = queuerMock;
            ActivityRequest request = new ActivityRequest();
            ActivityResult result = RunActivity(request, true, ActivityResultTimeout);
            Assert.IsFalse(this.Context.Success);
            Assert.AreEqual(this.Context.ErrorDetails.Message, "Message Accepted and Queued successfully");
        }

        /// <summary>
        /// Tests if success json response is built correctly
        /// </summary>
        [TestMethod]
        public void BuildSuccessResponseTest()
        {
            ActivityResult result = new ActivityResult();
            result.Succeeded = true;
            result.Values.Add("Test", @"{""ExternalEntityId"":""1fc563c0ae5c409d9c2a767f2bfe66b1"",""EntityCategory"":""User""}");
            Context.Success = true;
            using (var writer = new StringWriter())
            {
                WriteResponse(result, writer);
                Assert.AreEqual(
                    @"{""Test"":{""ExternalEntityId"":""1fc563c0ae5c409d9c2a767f2bfe66b1"",""EntityCategory"":""User""}}",
                    writer.ToString());
            }
        }

        /// <summary>
        /// Tests if failure json response is built correctly
        /// </summary>
        [TestMethod]
        public void BuildFailResponseTest()
        {
            Context.Success = false;
            Context.ErrorDetails.Message = "Fail JSON response";

            using (var writer = new StringWriter())
            {
                WriteResponse(new ActivityResult(), writer);
                Assert.IsTrue(writer.ToString().Contains("Fail JSON response"));
            }
        }

        /// <summary>Builds the response from the activity result.</summary>
        /// <remarks>This is the only place from which the response needs to be built.</remarks>
        /// <param name="result">Result returned from the activity</param>
        /// <returns>Stream that contains the json response to be returned</returns>
        protected override Stream BuildResponse(ActivityResult result)
        {
            throw new NotImplementedException();
        }
    }
}
