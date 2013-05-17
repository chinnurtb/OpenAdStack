// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityServiceFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.ServiceModel.Web;
using System.Text;
using Activities;
using ApiLayer;
using DataAccessLayer;
using EntityTestUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Queuing;
using ResourceAccess;
using Rhino.Mocks;
using Rhino.Mocks.Constraints;
using TestUtilities;
using WorkItems;

namespace ApiLayerUnitTests
{
    using Utilities.IdentityFederation;

    /// <summary>Test fixture for the Entity Service</summary>
    [TestClass]
    public class EntityServiceFixture : EntityService
    {
        /// <summary>Activity result timeout</summary>
        private const long ActivityResultTimeout = 10;

        /// <summary>
        /// Mock for ClaimRetriever
        /// </summary>
        private static IClaimRetriever claimRetriever;

        /// <summary>
        /// The expected user
        /// </summary>
        private static UserEntity expectedUser;

        /// <summary>
        /// Mock queuer used for tests
        /// </summary>
        private IQueuer queuerMock;

        /// <summary>
        /// Mock for outgoing web response
        /// </summary>
        private IOutgoingWebResponseContext outgoingWebResponseContextMock;

        /// <summary>Mock Web contect used for testing
        /// </summary>
        private IWebOperationContext webContextMock;

        /// <summary>
        /// Mock entity repository used for tests
        /// </summary>
        private IEntityRepository repository;

        /// <summary>
        /// Mock user access repository used for tests
        /// </summary>
        private IUserAccessRepository userAccessRepository;

        /// <summary>
        /// Mock access handler
        /// </summary>
        private IResourceAccessHandler accessHandler;

        /// <summary>
        /// User ExternalEntityId used in the tests
        /// </summary>
        private string userEntityId;

        /// <summary>
        /// User UserId used in the tests
        /// </summary>
        private string userId;

        /// <summary>
        /// uritemplateMatch value
        /// </summary>
        private UriTemplateMatch uriTemplateMatch;

        /// <summary>
        /// Initialize the mock queuer before each test and set the timeout values for waiting on the queue
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            this.queuerMock = MockRepository.GenerateStub<IQueuer>();
            Queuer = this.queuerMock;
            ConfigurationManager.AppSettings["ApiLayer.QueueResponsePollTime"] = "1";
            ConfigurationManager.AppSettings["ApiLayer.MaxQueueResponseWaitTime"] = ActivityResultTimeout.ToString(CultureInfo.InvariantCulture);
            this.queuerMock.Stub(f => f.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), new WorkItem()).Dummy))
                .Return(true);
            this.queuerMock.Stub(f => f.CheckWorkItem(Arg<string>.Is.Anything)).Return(new WorkItem());

            this.webContextMock = MockRepository.GenerateStub<IWebOperationContext>();
            WebContext = this.webContextMock;

            this.uriTemplateMatch = new UriTemplateMatch();
            this.outgoingWebResponseContextMock = MockRepository.GenerateStub<IOutgoingWebResponseContext>();
            this.uriTemplateMatch.BaseUri = new Uri("http://localhost/api/entity");

            this.webContextMock.Stub(f => f.IncomingRequest.UriTemplateMatch).Return(this.uriTemplateMatch);
            this.webContextMock.Stub(f => f.OutgoingResponse).Return(this.outgoingWebResponseContextMock);

            this.userEntityId = EntityTestHelpers.NewEntityIdString();
            this.userId = Guid.NewGuid().ToString();

            claimRetriever = MockRepository.GenerateMock<IClaimRetriever>();

            var contactEmail = "foo@example.com";
            expectedUser = EntityTestHelpers.CreateTestUserEntity(EntityTestHelpers.NewEntityIdString(), Guid.NewGuid().ToString(), contactEmail);
            claimRetriever.Stub(f => f.GetClaimValue(Arg<string>.Is.Anything)).Return(expectedUser.UserId);

            this.SetupAccessMocks();
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
            using (StreamReader reader = new StreamReader(BuildResponse(result)))
            {
                Assert.AreEqual(@"{""Test"":{""ExternalEntityId"":""1fc563c0ae5c409d9c2a767f2bfe66b1"",""EntityCategory"":""User""}}", reader.ReadToEnd());
            }
        }

        /// <summary>
        /// Tests if failure json response is built correctly
        /// </summary>
        [TestMethod]
        public void BuildFailResponseTest()
        {
            string actualResult = string.Empty;
            Context.Success = false;
            Context.ErrorDetails.Message = "Fail JSON response";
            using (StreamReader resultStream = new StreamReader(BuildResponse(new ActivityResult())))
            {
                actualResult = resultStream.ReadToEnd();
            }

            Assert.IsTrue(actualResult.Contains("Fail JSON response"));
        }

        /// <summary>
        /// Tests if Context State is set properly. Verifies ResponseCode, Success and Message are set right
        /// </summary>
        [TestMethod]
        public void SetContextErrorStateTest()
        {
            var errorMessage = "Some Error Happened";
            SetContextErrorState(System.Net.HttpStatusCode.InternalServerError, errorMessage);
            Assert.IsTrue(this.Context.ResponseCode == System.Net.HttpStatusCode.InternalServerError);
            Assert.IsFalse(this.Context.Success);
            Assert.IsTrue(this.Context.ErrorDetails.Message == errorMessage);
        }

        /// <summary>
        /// Test for bad request to be returned if empty string is passed in for ID
        /// </summary>
        [TestMethod]
        public void EmptyIdValidationTest()
        {
            ValidateAndBuildRequest(string.Empty);
            Assert.AreEqual(this.Context.ResponseCode, System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(this.Context.ErrorDetails.Message == "Entity Id not passed in");
        }

        /// <summary>
        /// Test for bad request to be returned if empty string is passed in for ID
        /// </summary>
        [TestMethod]
        public void InvalidIdValidationTest()
        {
            ValidateAndBuildRequest("z");
            Assert.IsTrue(this.Context.ResponseCode == System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(this.Context.ErrorDetails.Message == "Invalid Resource Id");
        }

        /// <summary>
        /// Test for bad request to be returned if empty string is passed in for ID
        /// </summary>
        [TestMethod]
        public void InvalidParentIdValidationTest()
        {
            ValidateParentIdAndBuildRequest("z");
            Assert.IsTrue(this.Context.ResponseCode == System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(this.Context.ErrorDetails.Message == "Invalid Parent Resource Id");
        }

        /// <summary>
        /// Test for bad request to be returned if empty string is passed in for ID
        /// </summary>
        [TestMethod]
        public void EmptyParentIdValidationTest()
        {
            ValidateParentIdAndBuildRequest(string.Empty);
            Assert.IsTrue(this.Context.ResponseCode == System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(this.Context.ErrorDetails.Message == "Parent Entity Id not passed in");
        }

        /// <summary>
        /// Reflects on Entity activity namespace and checks to ensure all activities in the dicitionary in API actually exist
        /// </summary>
        [TestMethod]
        public void ActivityExistTest()
        {
            string entityNamespace = "EntityActivities";
            string dynamicAllocationEntityNamespace = "DynamicAllocationActivities";
            string billingActivityNamespace = "BillingActivities";
            string mailEntityNamespace = "EntityActivities.UserMail";
            Assembly assemblyToCheck = Assembly.Load("EntityActivities");
            Assembly dynamicAllocationAssemblyToCheck = Assembly.Load("DynamicAllocationActivities");
            Assembly billingActivityAssemblyToCheck = Assembly.Load("BillingActivities");
            Assert.IsTrue(EntityService.ActivityMap.Count > 0);
            foreach (var activity in EntityService.ActivityMap)
            {
                var q = from t in assemblyToCheck.GetTypes()
                        where t.IsClass
                            && (t.Namespace == entityNamespace || t.Namespace == mailEntityNamespace)
                            && t.GetCustomAttributes(typeof(NameAttribute), false).Length > 0
                            && t.GetCustomAttributes(typeof(NameAttribute), false).Cast<NameAttribute>().Single().Value == activity.Value
                        select t;
                var da = from t in dynamicAllocationAssemblyToCheck.GetTypes()
                         where t.IsClass
                             && (t.Namespace == dynamicAllocationEntityNamespace)
                             && t.GetCustomAttributes(typeof(NameAttribute), false).Length > 0
                             && t.GetCustomAttributes(typeof(NameAttribute), false).Cast<NameAttribute>().Single().Value == activity.Value
                         select t;
                var bill = from t in billingActivityAssemblyToCheck.GetTypes()
                         where t.IsClass
                             && (t.Namespace == billingActivityNamespace)
                             && t.GetCustomAttributes(typeof(NameAttribute), false).Length > 0
                             && t.GetCustomAttributes(typeof(NameAttribute), false).Cast<NameAttribute>().Single().Value == activity.Value
                         select t;
                Assert.IsTrue(q.Count() == 1 || da.Count() == 1 || bill.Count() == 1);
            }
        }

        /// <summary>
        /// Tests to ensure the accessors are reading the right configuration value and returning the values when changed
        /// </summary>
        [TestMethod]
        public void QueueTimeConfigGetterTest()
        {
            var previousResponsePollTime = QueueResponsePollTime;
            var previousMaxQueueWaitTime = DefaultMaxQueueResponseWaitTime;
            var newResponsePollTime = "133";
            var newMaxQueueWaitTime = "144";
            ConfigurationManager.AppSettings["ApiLayer.QueueResponsePollTime"] = newResponsePollTime;
            ConfigurationManager.AppSettings["ApiLayer.MaxQueueResponseWaitTime"] = newMaxQueueWaitTime;
            Assert.IsTrue(QueueResponsePollTime == int.Parse(newResponsePollTime) && DefaultMaxQueueResponseWaitTime == int.Parse(newMaxQueueWaitTime));
            ConfigurationManager.AppSettings["ApiLayer.QueueResponsePollTime"] = previousResponsePollTime.ToString();
            ConfigurationManager.AppSettings["ApiLayer.MaxQueueResponseWaitTime"] = previousMaxQueueWaitTime.ToString();
        }

        /// <summary>
        /// is activity created with proper values for the CREATE POST scenario
        /// </summary>
        [TestMethod]
        public void ActivityFromPostCreateScenarioTest()
        {
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var resourceName = "User"; // use well known activity name
            var postActivityRequest = this.GetActivityRequestFromPostOrPut(resourceName, string.Empty, string.Empty, "POST", postBody, null);
            Assert.IsTrue(postActivityRequest != null);
            Assert.IsTrue(postActivityRequest.Values["EntityId"].Length == 32); // Stupid test to make sure an id was created and set
            Assert.IsTrue(postActivityRequest.Values["Payload"] == postBodyText); // did the payload get set and unmodified
            Assert.IsFalse(string.IsNullOrWhiteSpace(postActivityRequest.Task)); // activity task was set
        }

        /// <summary>
        /// Failure condition, should have null activityRequest if message is unknown
        /// </summary>
        [TestMethod]
        public void ActivityFromPostCreateBadMessageScenarioTest()
        {
            var postBody = this.GetDummyPost();
            var resourceName = "User"; // use well known activity name
            var postActivityRequest = this.GetActivityRequestFromPostOrPut(resourceName, "garbagemessagename2342", new EntityId(), "POST", postBody, null);
            Assert.IsTrue(postActivityRequest == null);
        }

        /// <summary>
        /// Failure condition, should have null activityRequest if Resource is unknown
        /// </summary>
        [TestMethod]
        public void ActivityFromPostCreateBadResourceScenarioTest()
        {
            var postBody = this.GetDummyPost();
            var resourceName = "skhdfe8klhsjkjh23498kjhalkhjlksdg283y"; // use well known activity name
            var postActivityRequest = this.GetActivityRequestFromPostOrPut(resourceName, string.Empty, string.Empty, "POST", postBody, null);
            Assert.IsTrue(postActivityRequest == null);
        }

        /// <summary>
        /// Failure condition, should have null activityRequest if Body is empty
        /// </summary>
        [TestMethod]
        public void ActivityFromPostCreateEmptyBodyScenarioTest()
        {
            var postBodyText = string.Empty;
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var resourceName = "User"; // use well known activity name
            var postActivityRequest = this.GetActivityRequestFromPostOrPut(resourceName, string.Empty, string.Empty, "POST", postBody, null);
            Assert.IsTrue(postActivityRequest == null);
        }

        /// <summary>
        /// is activity created with proper values for the Resource POST scenario
        /// </summary>
        [TestMethod]
        public void ActivityFromPostMessageScenarioTest()
        {
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var resourceId = new EntityId();
            var messageName = "Invite";
            var resourceName = "User"; // use well known activity name
            var postActivityRequest = this.GetActivityRequestFromPostOrPut(resourceName, messageName, resourceId, "POST", postBody, null);
            Assert.IsTrue(postActivityRequest != null);
            Assert.IsTrue(postActivityRequest.Values["EntityId"] == resourceId); // the same resource id is used
            Assert.IsTrue(postActivityRequest.Values["Payload"] == postBodyText); // did the payload get set and unmodified
            Assert.IsFalse(string.IsNullOrWhiteSpace(postActivityRequest.Task)); // activity task was set
        }

        /// <summary>
        /// Failure Scenario, Context should be unsuccessful, with proper error message if Namespace is not recognized
        /// </summary>
        [TestMethod]
        public void InvalidNamespaceTest()
        {
            TryGetActivity("BadNameSpace", "POST", "SomeMessage");
            Assert.IsFalse(this.Context.Success);
            Assert.IsTrue(this.Context.ResponseCode == System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(this.Context.ErrorDetails.Message == "Invalid Namespace - BadNameSpace");
        }

        /// <summary>
        /// Failure Scenario, Context should be unsuccessful, with proper error message if Namespace is blank
        /// </summary>
        [TestMethod]
        public void EmptyNamespaceTest()
        {
            TryGetActivity(string.Empty, "POST", "SomeMessage");
            Assert.IsFalse(this.Context.Success);
            Assert.IsTrue(this.Context.ResponseCode == System.Net.HttpStatusCode.BadRequest);
            Assert.IsTrue(this.Context.ErrorDetails.Message == "Empty Namespace");
        }

        /// <summary>
        /// Test to verify that we report the right status when the queue returns success.
        /// </summary>
        [TestMethod]
        public void RunActivitySuccessTest()
        {
            Queuer = this.SuccessQueue();
            var contextStatusStart = this.Context.ResponseCode;
            var testActivityRequest = BuildActivityRequest("TestActivity", new EntityId(), null);
            var runActivityResult = RunActivity(testActivityRequest, false, ActivityResultTimeout);
            Queuer = null;
            Assert.IsTrue(runActivityResult.Succeeded);
            Assert.IsTrue(this.Context.Success);
            Assert.AreEqual(contextStatusStart, this.Context.ResponseCode); // on success status is not updated on context.
        }

        /// <summary>
        /// Test to verify that we report the right http status when the queue returns failed.
        /// </summary>
        [TestMethod]
        public void RunActivityFailTest()
        {
            Queuer = this.FailedQueue();
            var testActivityRequest = BuildActivityRequest("TestActivity", new EntityId(), null);
            var runActivityResult = RunActivity(testActivityRequest, false, ActivityResultTimeout);
            Queuer = null;
            Assert.IsNotNull(runActivityResult);
            Assert.IsFalse(runActivityResult.Succeeded);
        }

        /// <summary>
        /// Test to verify that we report the right http status when the queue returns failed.
        /// </summary>
        [TestMethod]
        public void RunActivityTimeoutTest()
        {
            Queuer = this.PendingQueue();
            var testActivityRequest = BuildActivityRequest("TestActivity", new EntityId(), null);
            var runActivityResult = RunActivity(testActivityRequest, false, ActivityResultTimeout);
            Queuer = null;
            Assert.IsNull(runActivityResult);
            Assert.IsFalse(this.Context.Success);
            Assert.AreEqual(HttpStatusCode.Accepted, this.Context.ResponseCode);
        }

        /// <summary>
        /// Tests GET on a namespace with empty filter list
        /// </summary>
        [TestMethod]
        public void EmptyFilterGetNamespaceTest()
        {
            var resourceName = "Company"; // use well known activity name
            var postActivityRequest = GetActivityRequestFromNamespaceGet(resourceName, null);
            Assert.IsTrue(postActivityRequest != null);
        }

        /// <summary>
        /// Tests GET on a namespace with empty queryList list
        /// </summary>
        [TestMethod]
        public void GetNamespaceActivityRequestTest()
        {
            var resourceName = "Company"; // use well known activity name
            var postActivityRequest = GetActivityRequestFromNamespaceGet(resourceName, null);
            Assert.IsTrue(postActivityRequest.Task.Equals("GetCompaniesForUser"));
        }

        /// <summary>
        /// Tests GET on a on a subresource with filter list
        /// </summary>
        [TestMethod]
        public void FilteredGetSubResourceHandlerTest()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var resourceName = "Company"; // use well known activity name
            var postActivityRequest = GetSubResourceHandler(resourceName, new EntityId(), "Campaign", new EntityId());
            var reader = new StreamReader(postActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests GET on a on a resource with filter list
        /// </summary>
        [TestMethod]
        public void FilteredGetResourceHandlerTest()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var resourceName = "Company"; // use well known activity name
            var postActivityRequest = GetResourceHandler(resourceName, new EntityId());
            var reader = new StreamReader(postActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests the POST message handler 
        /// </summary>
        [TestMethod]
        public void PostResourceHandlerTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var resourceName = "User"; // use well known activity name
            PostResourceMessageHandler(resourceName, new EntityId(), string.Empty, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.Location.Contains("http://localhost/api/entity"));
        }
        
        /// <summary>
        /// Tests the POST message handler fail User access
        /// </summary>
        [TestMethod]
        public void PostResourceHandlerFailUserAccessTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(false);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var resourceName = "company"; // use well known activity name
            PostResourceMessageHandler(resourceName, new EntityId(), string.Empty, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.StatusCode == HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests the POST Billing Info message handler
        /// </summary>
        [TestMethod]
        public void PostBillingInfoTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            var queryValues = new NameValueCollection { { "Message", "UpdateBillingInfo" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""BillingInfoToken"":""abc123""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var resourceName = "company"; // use well known activity name
            PostResourceMessageHandler(resourceName, new EntityId(), "UpdateBillingInfo", postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.Location.Contains("http://localhost/api/entity"));
        }

        /// <summary>
        /// Tests the POST message handler on subnamespaces 
        /// </summary>
        [TestMethod]
        public void PostSubNamespaceHandlerTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            PostSubNamespaceHandler(parentNamespace, new EntityId(), subNamespace, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.Location.Contains("http://localhost/api/entity"));
        }

        /// <summary>
        /// Tests the POST message handler on subnamespaces User access denied
        /// </summary>
        [TestMethod]
        public void PostSubNamespaceHandlerFailUserAccessTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(false);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            PostSubNamespaceHandler(parentNamespace, new EntityId(), subNamespace, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.StatusCode == HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests the PUT message handler on Namespaces 
        /// </summary>
        [TestMethod]
        public void PutNamespaceHandlerTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var putActivityRequest = PutNamespaceHandler(parentNamespace, new EntityId(), postBody);
            var reader = new StreamReader(putActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests the PUT message handler on Namespaces user access denied
        /// </summary>
        [TestMethod]
        public void PutNamespaceHandlerFailUserAccessTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(false);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var putActivityRequest = PutNamespaceHandler(parentNamespace, new EntityId(), postBody);
            var reader = new StreamReader(putActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(this.webContextMock.OutgoingResponse.StatusCode == HttpStatusCode.Unauthorized);
            Assert.IsTrue(
                 activityResponse.Contains(@"User is not authorized to update entity"));
        }

        /// <summary>
        /// Tests the PUT message handler on subnamespaces 
        /// </summary>
        [TestMethod]
        public void PutSubNamespaceHandlerTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var putActivityRequest = PutSubNamespaceHandler(parentNamespace, new EntityId(), subNamespace, new EntityId(), postBody);
            var reader = new StreamReader(putActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests the PUT message handler on subnamespaces user access denied
        /// </summary>
        [TestMethod]
        public void PutSubNamespaceHandlerFailUserAccessTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(false);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var putActivityRequest = PutSubNamespaceHandler(parentNamespace, new EntityId(), subNamespace, new EntityId(), postBody);
            var reader = new StreamReader(putActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(this.webContextMock.OutgoingResponse.StatusCode == HttpStatusCode.Unauthorized);
            Assert.IsTrue(
                 activityResponse.Contains(@"User is not authorized to update entity"));
        }

        /// <summary>
        /// Tests the POST message handler on namespaces 
        /// </summary>
        [TestMethod]
        public void PostNamespaceHandlerTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            PostNamespaceHandler(parentNamespace, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.Location.Contains("http://localhost/api/entity"));
        }

        /// <summary>
        /// Tests the POST message handler on namespaces 
        /// </summary>
        [TestMethod]
        public void PostNamespaceHandlerUserAccessFailTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(false);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            PostNamespaceHandler(parentNamespace, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.StatusCode == HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests the POST message handler on subnamespaces 
        /// </summary>
        [TestMethod]
        public void PostSubNamespaceMessageHandlerTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(true);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var message = "SomeMessageName";
            PostSubNamespaceMessageHandler(
                parentNamespace, new EntityId(), subNamespace, new EntityId(), message, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.Location.Contains("http://localhost/api/entity"));
        }
       
        /// <summary>
        /// Tests the POST message handler on subnamespaces 
        /// </summary>
        [TestMethod]
        public void PostSubNamespaceMessageHandlerUserAccessFailTest()
        {
            this.accessHandler = MockRepository.GenerateMock<IResourceAccessHandler>();
            this.accessHandler.Stub(f => f.CheckAccess(Arg<CanonicalResource>.Is.Anything, Arg<EntityId>.Is.Anything)).Return(false);
            AccessResourceHandler = this.accessHandler;

            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var message = "SomeMessageName";
            PostSubNamespaceMessageHandler(
                parentNamespace, new EntityId(), subNamespace, new EntityId(), message, postBody);
            Assert.IsTrue(this.webContextMock.OutgoingResponse.StatusCode == HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Tests GET on a namespace with filter list
        /// </summary>
        [TestMethod]
        public void FilteredGetNamespaceTest()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var resourceName = "Company"; // use well known activity name
            var getActivityRequest = GetNamespaceHandler(resourceName);
            var reader = new StreamReader(getActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests GET on a namespace with no filter list
        /// </summary>
        [TestMethod]
        public void NoFilterGetNamespaceTest()
        {
            NameValueCollection queryValues = new NameValueCollection();
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var resourceName = "Company"; // use well known activity name
            var getActivityRequest = GetNamespaceHandler(resourceName);
            var reader = new StreamReader(getActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests GET on a namespace with filter list
        /// </summary>
        [TestMethod]
        public void FilteredGetSubNamespaceTest()
        {
            NameValueCollection queryValues = new NameValueCollection();
            this.uriTemplateMatch.QueryParameters.Add(queryValues);
            var resourceName = "Company"; // use well known activity name
            var getActivityRequest = GetSubNamespaceHandler(resourceName, new EntityId(), "Campaign");
            var reader = new StreamReader(getActivityRequest);
            string activityResponse = reader.ReadToEnd();
            Assert.IsTrue(
                activityResponse.Contains(@"""Id"":null,""Message"":""Message Accepted and Queued successfully"""));
        }

        /// <summary>
        /// Tests GET on a namespace with empty filter list
        /// </summary>
        [TestMethod]
        public void GetActivityWithUnfilteredNamespaceGetTest()
        {
            var resourceName = "Company"; // use well known activity name
            var getActivityRequest = GetActivityRequestFromNamespaceGet(resourceName, null);
            Assert.IsInstanceOfType(getActivityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Tests GET on a namespace with a filter list
        /// </summary>
        [TestMethod]
        public void GetActivityWithFilteredNamespaceGetTest()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var resourceName = "Company"; // use well known activity name
            var getActivityRequest = GetActivityRequestFromNamespaceGet(resourceName, queryValues);
            Assert.IsInstanceOfType(getActivityRequest, typeof(ActivityRequest));
        }

         /// <summary>
        /// Tests getting an activity request for POST on a subNamespace
        /// </summary>
        [TestMethod]
        public void GetActivityWithFilteredSubNamespacePost()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var postActivityRequest = GetActivityRequestFromSubNamespacePostOrPut(parentNamespace, new EntityId(), subNamespace, string.Empty, new EntityId(), "POST", this.GetDummyPost(), queryValues);
            Assert.IsInstanceOfType(postActivityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Tests getting an activity request for GET on a namespace with empty filter list
        /// </summary>
        [TestMethod]
        public void GetActivitySubNamespaceGet()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var getActivityRequest = GetActivityRequestFromSubNamespaceGet(parentNamespace, new EntityId(), subNamespace, new EntityId(), queryValues);
            Assert.IsInstanceOfType(getActivityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Tests getting an activity request for GET on a namespace with empty filter list
        /// </summary>
        [TestMethod]
        public void GetActivityInvalidParentIdSubNamespaceGet()
        {
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var getActivityRequest = GetActivityRequestFromSubNamespaceGet(parentNamespace, "z", subNamespace, new EntityId(), null);
            Assert.IsNull(getActivityRequest);
        }

        /// <summary>
        /// Tests getting an activity request for GET on a namespace with filter list
        /// </summary>
        [TestMethod]
        public void GetActivityWithFilteredSubNamespaceGet()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var getActivityRequest = GetActivityRequestFromSubNamespaceGet(parentNamespace, new EntityId(), subNamespace, new EntityId(), queryValues);
            Assert.IsInstanceOfType(getActivityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Tests getting an activity request for POST on a subNamespace
        /// </summary>
        [TestMethod]
        public void GetActivityRequestSubNamespaceCreationPost()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var postActivityRequest = GetActivityRequestFromSubNamespacePostOrPut(parentNamespace, new EntityId(), subNamespace, string.Empty, new EntityId(), "POST", this.GetDummyPost(), queryValues);
            Assert.IsInstanceOfType(postActivityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Tests posting a message to a subnamespace
        /// </summary>
        [TestMethod]
        public void GetActivityRequestSubNamespaceMessagePost()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var message = "SomeMessageName";
            var postActivityRequest = GetActivityRequestFromSubNamespacePostOrPut(parentNamespace, new EntityId(), subNamespace, message, new EntityId(), "POST", this.GetDummyPost(), queryValues);
            Assert.IsNull(postActivityRequest);
        }

        /// <summary>
        /// Tests posting a message to a subnamespace
        /// </summary>
        [TestMethod]
        public void GetActivityRequestSubNamespaceInvalidParentIdPost()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var message = string.Empty;
            var postActivityRequest = GetActivityRequestFromSubNamespacePostOrPut(parentNamespace, "z", subNamespace, message, new EntityId(), "POST", this.GetDummyPost(), queryValues);
            Assert.IsNull(postActivityRequest);
        }

        /// <summary>
        /// Tests posting a message to a subnamespace
        /// </summary>
        [TestMethod]
        public void GetActivityRequestSubNamespaceInvalidResourceIdPost()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var parentNamespace = "Company"; // use well known activity name
            var subNamespace = "Campaign"; // use well known activity name
            var message = "SomeMessage"; // If you post to a resorce you need a message
            var postActivityRequest = GetActivityRequestFromSubNamespacePostOrPut(parentNamespace, new EntityId(), subNamespace, message, "z", "POST", this.GetDummyPost(), queryValues);
            Assert.IsNull(postActivityRequest);
        }

        /// <summary>
        /// Tests building an Activity Request with empty activity name, should return null object
        /// </summary>
        [TestMethod]
        public void BuildActivityRequestEmpty()
        {
            var emptyActivityRequest = BuildActivityRequest(string.Empty, string.Empty, null);
            Assert.IsNull(emptyActivityRequest);
        }

        /// <summary>
        /// Tests building an Activity Request with empty entityId
        /// </summary>
        [TestMethod]
        public void BuildActivityRequestEmptyEntityId()
        {
            var activityName = "DummyActivity";
            var emptyActivityRequest = BuildActivityRequest(activityName, string.Empty, null);
            Assert.AreEqual(activityName, emptyActivityRequest.Task);

            // make sure the task was set right
            Assert.AreEqual(activityName, emptyActivityRequest.Task);

            // Now make sure there is an entityId element
            var entityId = string.Empty;
            emptyActivityRequest.Values.TryGetValue("EntityId", out entityId);
            Assert.IsNotNull(entityId);

            // And that there is a Payload element
            Assert.IsTrue(emptyActivityRequest.Values.ContainsKey("Payload"));

            // And that there is a Payload element
            Assert.IsTrue(emptyActivityRequest.Values.ContainsKey("AuthUserId"));
        }

        /// <summary>
        /// Tests building an Activity Request
        /// </summary>
        [TestMethod]
        public void BuildActivityRequestTest()
        {
            var activityName = "DummyActivity";
            var sourceEntityId = new EntityId();
            var emptyActivityRequest = BuildActivityRequest(activityName, sourceEntityId.ToString(), null);
            Assert.AreEqual(activityName, emptyActivityRequest.Task);

            // make sure the task was set right
            Assert.AreEqual(activityName, emptyActivityRequest.Task);

            // Now make sure there is an entityId element
            var entityId = string.Empty;
            emptyActivityRequest.Values.TryGetValue("EntityId", out entityId);
            Assert.AreEqual(entityId, sourceEntityId.ToString());

            // And that there is a Payload element
            Assert.IsTrue(emptyActivityRequest.Values.ContainsKey("Payload"));

            // And that there is a Payload element
            Assert.IsTrue(emptyActivityRequest.Values.ContainsKey("AuthUserId"));

            // should only have 3 elements in values (EntityId, Payload and AuthUserId
            Assert.IsTrue(emptyActivityRequest.Values.Count == 3);
        }

        /// <summary>
        /// Tests building an Activity Request
        /// </summary>
        [TestMethod]
        public void BuildActivityRequestWithParentTest()
        {
            var activityName = "DummyActivity";
            var sourceEntityId = new EntityId();
            var parentEntityId = new EntityId();
            var emptyActivityRequest = BuildActivityRequest(activityName, parentEntityId, sourceEntityId.ToString(), null);
            Assert.AreEqual(activityName, emptyActivityRequest.Task);

            // make sure the task was set right
            Assert.AreEqual(activityName, emptyActivityRequest.Task);

            // Now make sure there is an entityId element
            var entityId = string.Empty;
            emptyActivityRequest.Values.TryGetValue("EntityId", out entityId);
            Assert.AreEqual(entityId, sourceEntityId.ToString());

            entityId = string.Empty;
            emptyActivityRequest.Values.TryGetValue("ParentEntityId", out entityId);
            Assert.AreEqual(entityId, parentEntityId.ToString());

            // And that there is a Payload element
            Assert.IsTrue(emptyActivityRequest.Values.ContainsKey("Payload"));

            // And that there is a Payload element
            Assert.IsTrue(emptyActivityRequest.Values.ContainsKey("AuthUserId"));

            // should only have 4 elements in values (EntityId, Payload and AuthUserId, don't let anything else sneak on :)
            Assert.IsTrue(emptyActivityRequest.Values.Count == 4);
        }

        /// <summary>
        /// Tests building an Activity Request
        /// </summary>
        [TestMethod]
        public void BuildActivityRequestWithEmptyParentTest()
        {
            var activityName = "DummyActivity";
            var sourceEntityId = new EntityId();
            var parentEntityId = string.Empty;
            var emptyActivityRequest = BuildActivityRequest(activityName, parentEntityId, sourceEntityId.ToString(), null);
            Assert.IsNull(emptyActivityRequest);
        }

        /// <summary>
        /// Text to ensure error response when activity name is null
        /// </summary>
        [TestMethod]
        public void ProcessActivityEmptyActivity()
        {
            this.ProcessActivity(null, true);
            Assert.AreEqual("Error while creating activity request", this.Context.ErrorDetails.Message);
            Assert.AreEqual(HttpStatusCode.InternalServerError, this.Context.ResponseCode);
        }

        /// <summary>
        /// Text to ensure error response when Queue fails
        /// </summary>
        [TestMethod]
        public void ProcessActivityFailedQueue()
        {
            Queuer = this.FailedToQueueQueue();
            ProcessActivity(new ActivityRequest(), true);
            Assert.AreEqual("Unable to queue message", this.Context.ErrorDetails.Message);
            Assert.AreEqual(HttpStatusCode.InternalServerError, this.Context.ResponseCode);
        }

        /// <summary>
        /// Test if we successfully get an activityRequest object for the resource get
        /// </summary>
        [TestMethod]
        public void GetActivityRequestResourceGetTest()
        {
            var activityRequest = GetActivityRequestFromResourceGet("Company", new EntityId(), null); // well known namespace
            Assert.IsInstanceOfType(activityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Test if we successfully get an activityRequest object for the resource get
        /// </summary>
        [TestMethod]
        public void GetActivityRequestSubResourceGetTest()
        {
            NameValueCollection queryValues = new NameValueCollection { { "foo", "bar" } };
            var activityRequest = GetActivityRequestFromSubResourceGet("Company", new EntityId(), "Campaign", new EntityId(), queryValues); // well known namespace
            Assert.IsInstanceOfType(activityRequest, typeof(ActivityRequest));
        }

        /// <summary>
        /// Checks to see if we get a queuer if the member is currently Null, set queuer to null and then try to get it
        /// </summary>
        [TestMethod]
        public void InitializeQueueTest()
        {
            Queuer = null;
            ConfigurationManager.AppSettings["ApiLayer.QueueResponsePollTime"] = "0";
            ConfigurationManager.AppSettings["Index.ConnectionString"] = "0";
            ConfigurationManager.AppSettings["Entity.ConnectionString"] = "0";
            ConfigurationManager.AppSettings["Dictionary.Blob.ConnectionString"] = "0";
            ConfigurationManager.AppSettings["Dictionary.Sql.ConnectionString"] = "0";
            ConfigurationManager.AppSettings["ApiLayer.MaxQueueResponseWaitTime"] = "0";
            Assert.IsInstanceOfType(Queuer, typeof(IQueuer));
        }

        /// <summary>
        /// Creates a Queuer that is hard coded to success
        /// </summary>
        /// <returns>New Queue</returns>
        private IQueuer SuccessQueue()
        {
            var mockQueuer = this.GetDefaultQueueMock();
            mockQueuer.Stub(q => q.CheckWorkItem(Arg<string>.Is.Anything))
                 .Return(new WorkItem { Status = WorkItemStatus.Processed, Result = new ActivityResult { Succeeded = true }.SerializeToXml() });
            return mockQueuer;
        }

        /// <summary>
        /// Creates a Queuer that is hard coded to pending, used to simulate timeouts
        /// </summary>
        /// <returns>New Queue</returns>
        private IQueuer PendingQueue()
        {
            var mockQueuer = this.GetDefaultQueueMock();
            mockQueuer.Stub(q => q.CheckWorkItem(Arg<string>.Is.Anything))
                 .Return(
                 new WorkItem
                 {
                     Status = WorkItemStatus.Pending,
                     Result = new ActivityResult
                     {
                         Succeeded = false
                     }
                     .SerializeToXml()
                 });
            return mockQueuer;
        }

        /// <summary>
        /// Creates a Queuer that simulate failure to queue activity requests
        /// </summary>
        /// <returns>New Queue</returns>
        private IQueuer FailedToQueueQueue()
        {
            var mockQueuer = MockRepository.GenerateMock<IQueuer>();
            mockQueuer.Stub(q => q.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), null).Dummy))
                .Return(false);                
            return mockQueuer;
        }

        /// <summary>
        /// Creates a Queuer that is hard coded to failed, used to simualate failed activities
        /// </summary>
        /// <returns>New Queue</returns>
        private IQueuer FailedQueue()
        {
            var mockQueuer = this.GetDefaultQueueMock();
            mockQueuer.Stub(q => q.EnqueueWorkItem(ref Arg<WorkItem>.Ref(Is.Anything(), null).Dummy))
                .Return(true);
            mockQueuer.Stub(q => q.CheckWorkItem(Arg<string>.Is.Anything))
                .Return(new WorkItem { Status = WorkItemStatus.Failed, Result = new ActivityResult { Succeeded = false }.SerializeToXml() });
            return mockQueuer;
        }

        /// <summary>
        /// Creates a Queuer that reflects back the workitem passed in.
        /// </summary>
        /// <returns>New Queue</returns>
        private IQueuer GetDefaultQueueMock()
        {
            WorkItem returnedWI = null;
            var workItemCaptureConstraint = new LambdaConstraint<WorkItem>(wi =>
            {
                returnedWI = wi;
                return true;
            });
            var mockQueuer = MockRepository.GenerateMock<IQueuer>();
            mockQueuer.Stub(q => q.EnqueueWorkItem(ref Arg<WorkItem>.Ref(workItemCaptureConstraint, returnedWI).Dummy))
                .Return(true)
                .WhenCalled(call =>
                {
                    call.Arguments[0] = returnedWI;
                });
            return mockQueuer;
        }

        /// <summary>
        /// Set up the mocks for UserAccessRepository and EntityRepository
        /// </summary>
        private void SetupAccessMocks()
        {
            this.userAccessRepository = MockRepository.GenerateMock<IUserAccessRepository>();
            UserAccessRepository = this.userAccessRepository;

            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.repository.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Anything)).Return(expectedUser);
            Repository = this.repository;

            NameIdentifierClaimValue = expectedUser.UserId;
        }

        /// <summary>
        /// builds a dummy JSON stream 
        /// </summary>
        /// <returns>Stream of JSON</returns>
        private Stream GetDummyPost()
        {
            var postBodyText = @"{""Field1"":""Foo"",""Field2"":""Bar""}";
            var postBody = new MemoryStream(Encoding.ASCII.GetBytes(postBodyText));
            return postBody;
        }
    }
}
