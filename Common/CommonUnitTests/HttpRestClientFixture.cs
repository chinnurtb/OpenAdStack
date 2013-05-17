//-----------------------------------------------------------------------
// <copyright file="HttpRestClientFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

using Microsoft.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Utilities.Net;
using Utilities.Net.Http;
using Utilities.Serialization;

namespace CommonUnitTests
{
    /// <summary>Unit tests for the HttpRestClient</summary>
    [TestClass]
    public class HttpRestClientFixture
    {
        /// <summary>JavaScript (JSON) serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>Test service endpoint URI</summary>
        private readonly string serviceEndpoint = "http://localhost/api";

        /// <summary>Test client timeout</summary>
        private readonly int timeout = 10;

        /// <summary>Mock IHttpClient for testing</summary>
        private IHttpClient mockHttpClient;

        /// <summary>List of requests received by the mock during the test</summary>
        private IList<HttpRequestMessage> httpRequestMessages;

        /// <summary>The response message to be returned by the mock</summary>
        private HttpResponseMessage httpResponseMessage;

        /// <summary>Initializes mocks, etc</summary>
        [TestInitialize]
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "Exception is thrown by mock stub")]
        public void TestInitialize()
        {
            // Start each test with a fresh list of requests
            this.httpRequestMessages = new List<HttpRequestMessage>();

            // Default to an empty 200 OK response 
            this.httpResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = HttpContent.CreateEmpty()
            };

            // Mock HttpClient stubs IHttpClient.SendAsync to add the request to
            // the request list and return the current response.
            this.mockHttpClient = MockRepository.GenerateMock<IHttpClient>();
            this.mockHttpClient.Stub(f => f.Send(Arg<HttpRequestMessage>.Is.Null))
                .Throw(new ArgumentNullException("request"));
            this.mockHttpClient.Stub(f => f.Send(Arg<HttpRequestMessage>.Is.NotNull))
                .WhenCalled(call =>
                {
                    var request = (HttpRequestMessage)call.Arguments[0];
                    this.httpRequestMessages.Add(request);
                    this.httpResponseMessage.Uri = request.Uri;
                })
                .Return(this.httpResponseMessage);
            this.mockHttpClient.Stub(f => f.BaseAddress)
                .Return(new Uri("http://example.com/"));
            this.mockHttpClient.Stub(f => f.TransportSettings)
                .Return(new HttpWebRequestTransportSettings
                {
                    ConnectionTimeout = new TimeSpan(0, 0, 0, 0, this.timeout)
                });
        }

        /// <summary>Smoke test creating an instance of HttpRestClient</summary>
        [TestMethod]
        public void Create()
        {
            var client = this.CreateTestHttpRestClient();
            Assert.IsNotNull(client);
        }

        /// <summary>Test sending a simple get request</summary>
        [TestMethod]
        public void SendRequest()
        {
            var objectUri = new Uri(this.serviceEndpoint + "/test/");
            var request = new HttpRequestMessage(HttpMethod.GET.ToString(), objectUri);

            var client = this.CreateTestHttpRestClient();
            var result = client.SendMessage(request);

            Assert.AreEqual(1, this.httpRequestMessages.Count);
            var sentRequest = this.httpRequestMessages[0];
            Assert.AreEqual(objectUri, sentRequest.Uri);
        }

        /// <summary>Test sending with retries</summary>
        [TestMethod]
        [Ignore]
        public void SendWithRetries()
        {
            // TODO: mock IHttpClient to throw the first couple times it is called and test
        }

        /// <summary>Test getting an object as a string</summary>
        [TestMethod]
        public void GetString()
        {
            var expectedUri = new Uri(this.serviceEndpoint + "/test/");
            var expectedResult = @"{""foo"":""bar""}";
            this.httpResponseMessage.Content = HttpContent.Create(expectedResult);

            var client = this.CreateTestHttpRestClient();
            var result = client.Get("/test/");

            Assert.AreEqual(expectedResult, result);
        }

        /// <summary>
        /// Test getting an object as a deserialized JSON object</summary>
        [TestMethod]
        public void GetObject()
        {
            var expected = new Dictionary<string, object>
            {
                { "NumberValue", 42 },
                { "TextValue", "Don't Panic." }
            };
            this.httpResponseMessage.Content = HttpContent.Create(JsonSerializer.Serialize(expected));

            var client = this.CreateTestHttpRestClient();
            var result = client.Get<IDictionary<string, object>>("/test/");

            Assert.IsNotNull(result);
            Assert.AreEqual((int)expected["NumberValue"], (int)result["NumberValue"]);
            Assert.AreEqual<string>((string)expected["TextValue"], (string)result["TextValue"]);
        }

        /// <summary>Creates an instance of the test HttpRestClient</summary>
        /// <returns>The test HttpRestClient</returns>
        private HttpRestClient CreateTestHttpRestClient()
        {
            var restClient = new TestHttpRestClient(this.mockHttpClient);
            return restClient;
        }

        /// <summary>Test HttpRestClient</summary>
        private class TestHttpRestClient : HttpRestClient
        {
            /// <summary>Initializes a new instance of the TestHttpRestClient class.</summary>
            /// <param name="client">HttpClient to use</param>
            public TestHttpRestClient(IHttpClient client)
                : base(client)
            {
            }

            /// <summary>Adds authentication to the request</summary>
            /// <param name="httpRequestMessage">The request</param>
            public override void AddAuthentication(ref HttpRequestMessage httpRequestMessage)
            {
            }

            /// <summary>Handles the response and returns the content</summary>
            /// <param name="httpResponseMessage">The response</param>
            /// <returns>The content</returns>
            protected override string HandleResponse(HttpResponseMessage httpResponseMessage)
            {
                return httpResponseMessage.Content.ReadAsString();
            }
        }
    }
}
