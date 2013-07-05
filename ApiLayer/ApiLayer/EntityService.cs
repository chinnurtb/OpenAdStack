// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityService.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Activities;
using ConfigManager;
using DataAccessLayer;
////using Doppler.TraceListeners;
using Microsoft.Practices.Unity;
using Queuing;
using ResourceAccess;
using RuntimeIoc.WebRole;
using Utilities.IdentityFederation;
using WorkItems;

namespace ApiLayer
{
    /// <summary>Common base class for ApiLayer services</summary>
    /// <remarks>The ServiceBase assumes InstanceContextMode.PerCall</remarks>
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class EntityService : ServiceBase
    {
        /// <summary>Entity Respository</summary>
        private static IEntityRepository repository;

        /// <summary>User Access Respository</summary>
        private static IUserAccessRepository userAccessRepository;

        /// <summary>Resource Handler/// </summary>
        private static IResourceAccessHandler accessResourceHandler;

        /// <summary>
        /// Gets Dictionary that holds all activity mappings for all namespace, subnamespace, method combos
        /// </summary>
        internal static Dictionary<string, string> ActivityMap
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "COMPANY/CAMPAIGN:RESOURCE:GET:VALUATIONS", "DAGetValuations" },
                    { "USER:POST", "SaveUser" },
                    { "USER:PUT", "SaveUser" },
                    { "USER:POST:INVITE", "SendUserInviteMail" },
                    { "USER:POST:VERIFY", "UserMessageVerify" },
                    { "USER:NAMESPACE:GET", "GetUsers" },
                    { "USER:RESOURCE:GET", "GetUser" },
                    { "COMPANY:POST", "CreateCompany" },
                    { "COMPANY/COMPANY:POST", "CreateCompany" },
                    { "COMPANY:PUT", "SaveCompany" },
                    { "COMPANY:NAMESPACE:GET", "GetCompaniesForUser" },
                    { "COMPANY:RESOURCE:GET", "GetCompanyByEntityId" },
                    { "COMPANY/COMPANY:RESOURCE:GET", "GetCompanyByEntityId" },
                    { "COMPANY/CAMPAIGN:POST", "CreateCampaign" },
                    { "COMPANY/CAMPAIGN:PUT", "SaveCampaign" },
                    { "COMPANY/CAMPAIGN:RESOURCE:GET", "GetCampaignByEntityId" },
                    { "COMPANY/CAMPAIGN:RESOURCE:GET:CREATIVES", "GetCreativesForCampaign" },
                    { "COMPANY/CAMPAIGN:RESOURCE:GET:BLOB", "GetBlobByEntityId" },
                    { "COMPANY/CAMPAIGN:NAMESPACE:GET", "GetCampaignsForCompany" },
                    { "COMPANY/CREATIVE:POST", "SaveCreative" },
                    { "COMPANY/CREATIVE:PUT", "SaveCreative" },
                    { "COMPANY/CREATIVE:RESOURCE:GET", "GetCreativeByEntityId" },
                    { "COMPANY:POST:ADDADVERTISER", "AssociateEntities" },
                    { "COMPANY:POST:UPDATEBILLINGINFO", "SaveBillingInfo" },
                    { "COMPANY/CAMPAIGN:POST:ADDASSOCIATION", "AssociateEntities" },
                    { "COMPANY/CAMPAIGN:POST:REMOVEASSOCIATION", "DisassociateEntities" },
                    { "COMPANY/CAMPAIGN:POST:ADDCREATIVE", "AssociateEntities" },
                    { "COMPANY/CAMPAIGN:POST:REMOVECREATIVE", "DisassociateEntities" },
                };
            }
        }

        /// <summary>Gets or sets the Entity Repository</summary>
        internal static IEntityRepository Repository
        {
            get { return repository = repository ?? RuntimeIocContainer.Instance.Resolve<IEntityRepository>(); }
            set { repository = value; }
        }

        /// <summary>Gets or sets the UserAccessRepository</summary>
        internal static IUserAccessRepository UserAccessRepository
        {
            get { return userAccessRepository = userAccessRepository ?? RuntimeIocContainer.Instance.Resolve<IUserAccessRepository>(); }
            set { userAccessRepository = value; }
        }

        /// <summary>
        /// Gets or sets the accessResourceHandler
        /// </summary>
        internal static IResourceAccessHandler AccessResourceHandler
        {
            get { return accessResourceHandler ?? new ResourceAccessHandler(UserAccessRepository, Repository); }
            set { accessResourceHandler = value; }
        }

        /// <summary>
        /// general resource post handler message processing
        /// </summary>
        /// <param name="parentNamespace">namespace for parent entity</param>
        /// <param name="parentId">entity id for parent entity</param>
        /// <param name="resourceNamespace">Namespace of the resource being fetched</param>
        /// <param name="id">resource id</param>
        /// <returns>response stream for HTTP response</returns>
        [WebGet(UriTemplate = "{parentNamespace}/{parentId}/{resourceNamespace}/{id}", ResponseFormat = WebMessageFormat.Json)]
        public Stream GetSubResourceHandler(string parentNamespace, string parentId, string resourceNamespace, string id)
        {
            NameValueCollection nameValueCollection = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters;
            return this.ProcessActivity(this.GetActivityRequestFromSubResourceGet(parentNamespace, parentId, resourceNamespace, id, nameValueCollection), true);
        }

        /// <summary>
        /// general resource post handler message processing
        /// </summary>
        /// <param name="resourceNamespace">Namespace of the resource being posted</param>
        /// <returns>response stream for HTTP response</returns>
        [WebGet(UriTemplate = "{resourceNamespace}/", ResponseFormat = WebMessageFormat.Json)]
        public Stream GetNamespaceHandler(string resourceNamespace)
        {
            NameValueCollection nameValueCollection = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters;
            return this.ProcessActivity(this.GetActivityRequestFromNamespaceGet(resourceNamespace, nameValueCollection), true);
        }

        /// <summary>
        /// general resource post handler message processing
        /// </summary>
        /// <param name="parentNamespace">Namespace of the parent resource</param>
        /// <param name="parentId">entity id of the parent entity</param>
        /// <param name="subNamespace">Namespace of the type of entities to be fetched</param>
        /// <returns>response stream for HTTP response</returns>
        [WebGet(UriTemplate = "{parentNamespace}/{parentId}/{subNamespace}", ResponseFormat = WebMessageFormat.Json)]
        public Stream GetSubNamespaceHandler(string parentNamespace, string parentId, string subNamespace)
        {
            NameValueCollection nameValueCollection = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters;
            return this.ProcessActivity(this.GetActivityRequestFromSubNamespaceGet(parentNamespace, parentId, subNamespace, string.Empty, nameValueCollection), true);
        }

        /// <summary>
        /// general resource post handler message processing
        /// </summary>
        /// <param name="resourceNamespace">Namespace of the resource being posted</param>
        /// <param name="id">resource id</param>
        /// <returns>response stream for HTTP response</returns>
        [WebGet(UriTemplate = "{resourceNamespace}/{id}", ResponseFormat = WebMessageFormat.Json)]
        public Stream GetResourceHandler(string resourceNamespace, string id)
        {
            NameValueCollection nameValueCollection = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters;
            return this.ProcessActivity(this.GetActivityRequestFromResourceGet(resourceNamespace, id, nameValueCollection), true);
        }

        /// <summary>
        /// general resource post handler message processing
        /// </summary>
        /// <param name="resourceNamespace">Namespace of the resource being posted</param>
        /// <returns>response stream for HTTP response</returns>
        [WebGet(UriTemplate = "{resourceNamespace}/id", ResponseFormat = WebMessageFormat.Json)]
        public Stream GetResourceHandlerId(string resourceNamespace)
        {
            // trying to get the user entity for the current user
            NameValueCollection nameValueCollection = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters;
            string task = this.TryGetActivity(resourceNamespace, "RESOURCE:GET", string.Empty);
            this.Context.BuildRequestContext(string.Empty);
            return this.ProcessActivity(this.BuildActivityRequest(task, string.Empty, nameValueCollection), true);
        }

        /// <summary>
        /// general resource post handler with subNamespace message processing
        /// </summary>
        /// <param name="parentNamespace">Namespace of the parent resource being posted</param>
        /// <param name="parentResource">Id of the parent resource being posted</param>
        /// <param name="subNamespace">Subnamespace of the  resource being posted</param>
        /// <param name="subNamespaceId">Id of the Subnamespace resource being posted</param>
        /// <param name="message">message being posted to the resource</param>
        /// <param name="postBody">JSON string of the post</param>
        [WebInvoke(UriTemplate = "{parentNamespace}/{parentResource}/{subNamespace}/{subNamespaceId}?Message={message}", Method = "POST")]
        public void PostSubNamespaceMessageHandler(string parentNamespace, string parentResource, string subNamespace, string subNamespaceId, string message, Stream postBody)
        {
            if (!this.CheckAuthorization("https://localhost/api/entity/{0}/{1}/{2}/{3}".FormatInvariant(parentNamespace, parentResource, subNamespace, subNamespaceId), "POST"))
            {
                WebContext.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return;
            }

            var serverName = WebContext.IncomingRequest.UriTemplateMatch.BaseUri;
            var activityRequest = this.GetActivityRequestFromSubNamespacePostOrPut(
                parentNamespace, parentResource, subNamespace, message, subNamespaceId, "POST", postBody, null);
            this.ProcessActivity(activityRequest, false);

            WebContext.OutgoingResponse.StatusCode = HttpStatusCode.SeeOther;
            WebContext.OutgoingResponse.Location = serverName + "/" + parentNamespace + "/" + parentResource + "/"
                                                                    + subNamespace + "/" + subNamespaceId;
        }

        /// <summary>
        /// general resource post handler message processing
        /// </summary>
        /// <param name="resourceNamespace">Namespace of the resource being posted</param>
        /// <param name="id">resource id</param>
        /// <param name="message">message being posted to the resource</param>
        /// <param name="postBody">JSON string of the post</param>
        [WebInvoke(UriTemplate = "{resourceNamespace}/{id}?Message={message}", Method = "POST")]
        public void PostResourceMessageHandler(string resourceNamespace, string id, string message, Stream postBody)
        {
            // verify caller has permission to write to the parent
            // CAVEAT: need to allow user to post verify message before the user is created, so don't check on resourceNamespace == "user"
            if (resourceNamespace.ToLower(CultureInfo.InvariantCulture) != "user" && !this.CheckAuthorization("https://localhost/api/entity/{0}/{1}".FormatInvariant(resourceNamespace, id), "POST"))
            {
                WebContext.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return;
            }

            var serverName = WebContext.IncomingRequest.UriTemplateMatch.BaseUri;
            var activtyRequest = this.GetActivityRequestFromPostOrPut(resourceNamespace, message, id, "POST", postBody, null);
            this.ProcessActivity(activtyRequest, false);

            WebContext.OutgoingResponse.StatusCode = HttpStatusCode.SeeOther;
            WebContext.OutgoingResponse.Location = serverName + "/" + resourceNamespace + "/"
                                                   + activtyRequest.Values["EntityId"];
        }

        /// <summary> Creates a new campaign </summary>
        /// <param name="parentNamespace">Namespace (Type) of the parent entity</param>
        /// <param name="parentResource">Company id</param>
        /// <param name="subNamespace">Namespace (Type) of the entity to be created</param>
        /// <param name="postBody"> json string value containing new entity values </param>
        [WebInvoke(UriTemplate = "{parentNamespace}/{parentResource}/{subNamespace}", Method = "POST")]
        public void PostSubNamespaceHandler(string parentNamespace, string parentResource, string subNamespace, Stream postBody)
        {
            // verify caller has permission to write to the parent
            if (!this.CheckAuthorization("https://localhost/api/entity/{0}/{1}".FormatInvariant(parentNamespace, parentResource), "POST"))
            {
                WebContext.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return;
            }

            var serverName = WebContext.IncomingRequest.UriTemplateMatch.BaseUri;
            var activityRequest = this.GetActivityRequestFromSubNamespacePostOrPut(
                parentNamespace, parentResource, subNamespace, string.Empty, string.Empty, "POST", postBody, null);

            this.ProcessActivity(activityRequest, false);

            WebContext.OutgoingResponse.StatusCode = HttpStatusCode.SeeOther;
            WebContext.OutgoingResponse.Location = serverName + "/" + parentNamespace + "/" + parentResource + "/"
                                                                    + subNamespace + "/"
                                                                    + activityRequest.Values["EntityId"];
        }

        /// <summary> Update a company </summary>
        /// <param name="parentNamespace">Namespace (Type) of the parent entity</param>
        /// <param name="parentResource">Company id</param>
        /// <param name="postBody"> json string value containing new entity values </param>
        /// <returns> new Campaign </returns>
        [WebInvoke(UriTemplate = "{parentNamespace}/{parentResource}", Method = "PUT", ResponseFormat = WebMessageFormat.Json)]
        public Stream PutNamespaceHandler(string parentNamespace, string parentResource, Stream postBody)
        {
            if (!this.CheckAuthorization("https://localhost/api/entity/{0}/{1}".FormatInvariant(parentNamespace, parentResource), "PUT"))
            {
                WebContext.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                this.SetContextErrorState(HttpStatusCode.Unauthorized, "User is not authorized to update entity type: {0}, id: {1}".FormatInvariant(parentNamespace, parentResource));
                return this.BuildResponse(null);
            }

            return this.ProcessActivity(this.GetActivityRequestFromPostOrPut(parentNamespace, string.Empty, parentResource, "PUT", postBody, null), false);
        }

        /// <summary> Update a new campaign </summary>
        /// <param name="parentNamespace">Namespace (Type) of the parent entity</param>
        /// <param name="parentResource">Company id</param>
        /// <param name="subNamespace">Namespace (Type) of the entity to be created</param>
        /// <param name="resourceId">entity id being updated</param>
        /// <param name="postBody"> json string value containing new entity values </param>
        /// <returns> new Campaign </returns>
        [WebInvoke(UriTemplate = "{parentNamespace}/{parentResource}/{subNamespace}/{resourceId}", Method = "PUT", ResponseFormat = WebMessageFormat.Json)]
        public Stream PutSubNamespaceHandler(string parentNamespace, string parentResource, string subNamespace, string resourceId, Stream postBody)
        {
            if (!this.CheckAuthorization("https://localhost/api/entity/{0}/{1}/{2}/{3}".FormatInvariant(parentNamespace, parentResource, subNamespace, resourceId), "PUT"))
            {
                WebContext.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                this.SetContextErrorState(HttpStatusCode.Unauthorized, "User is not authorized to update entity type: {0}, id: {1}, subentity type: {2}, subentity id: {3}".FormatInvariant(parentNamespace, parentResource, subNamespace, resourceId));
                return this.BuildResponse(null);
            }

            return this.ProcessActivity(this.GetActivityRequestFromSubNamespacePostOrPut(parentNamespace, parentResource, subNamespace, string.Empty, resourceId, "PUT", postBody, null), false);
        }

        /// <summary>
        /// general namespace post handler -- Resource Creation
        /// </summary>
        /// <param name="resourceNamespace">Namespace of the resource being posted</param>
        /// <param name="postBody">JSON string of the post</param>
        [WebInvoke(UriTemplate = "{resourceNamespace}", Method = "POST")]
        public void PostNamespaceHandler(string resourceNamespace, Stream postBody)
        {
            if (!this.CheckAuthorization("https://localhost/api/entity/{0}".FormatInvariant(resourceNamespace), "POST"))
            {
                WebContext.OutgoingResponse.StatusCode = HttpStatusCode.Unauthorized;
                return;
            }

            var serverName = WebContext.IncomingRequest.UriTemplateMatch.BaseUri;
            var activityReqest = this.GetActivityRequestFromPostOrPut(
                resourceNamespace, string.Empty, string.Empty, "POST", postBody, null);
            this.ProcessActivity(activityReqest, false);

            // build the return destination for the 303 response
            WebContext.OutgoingResponse.StatusCode = HttpStatusCode.SeeOther;
            WebContext.OutgoingResponse.Location = serverName + "/" + resourceNamespace + "/"
                                                                    + activityReqest.Values["EntityId"];
        }

        /// <summary>
        /// Creates a memory stream, writes the result, flushes and rewinds before returning the stream
        /// </summary>
        /// <param name="result">The result text</param>
        /// <returns>MemoryStream containing the result</returns>
        internal static Stream WriteResultToStream(string result)
        {
            var resultStream = new MemoryStream();
            var resultWriter = new StreamWriter(resultStream, Encoding.ASCII);
            resultWriter.Write(result);
            resultWriter.Flush();
            resultStream.Seek(0, SeekOrigin.Begin);
            return resultStream;
        }

        /// <summary>
        /// try/get activity from map
        /// validate resource name, sets context success to false on failure
        /// </summary>
        /// <param name="resourceNamespace">namespace of resource to process</param>
        /// <param name="verb">http method used GET/POST/PUT/DELETE</param>
        /// <param name="message">message to be processed on resource, blank if none</param>
        /// <returns>activity name</returns>
        internal string TryGetActivity(string resourceNamespace, string verb, string message)
        {
            ////Namespace is not in the map
            string task = null;
            string requestTypeForLookup = resourceNamespace.ToUpper(CultureInfo.CurrentCulture) + ":" + verb.ToUpper(CultureInfo.CurrentCulture);
            requestTypeForLookup += string.IsNullOrWhiteSpace(message) ? string.Empty : ":" + message.ToUpper(CultureInfo.CurrentCulture);
            if (!ActivityMap.TryGetValue(requestTypeForLookup, out task))
            {
                this.SetContextErrorState(HttpStatusCode.BadRequest, "Invalid Namespace - " + resourceNamespace);
            }

            ////empty name space
            if (string.IsNullOrWhiteSpace(resourceNamespace))
            {
                this.SetContextErrorState(HttpStatusCode.BadRequest, "Empty Namespace");
            }

            return task;
        }

        /// <summary>
        /// General Get handler for the class NO Flags
        /// </summary>
        /// <param name="resourceNamespace">namespace of the entity</param>
        /// <param name="id">id for the entity to get</param>
        /// <param name="nameValueCollection">Query string passed in by caller</param>
        /// <returns>Stream ready for HTTP out</returns>
        internal ActivityRequest GetActivityRequestFromResourceGet(string resourceNamespace, string id, NameValueCollection nameValueCollection) // TODO make override with flags 
        {
            string task = this.TryGetActivity(resourceNamespace, "RESOURCE:GET", string.Empty);
            this.ValidateAndBuildRequest(id);
            return this.BuildActivityRequest(task, id, nameValueCollection);
        }

        /// <summary>
        /// General Post handler for the class
        /// </summary>
        /// <param name="resourceNamespace">namespace of the entity</param>
        /// <param name="message">message if any to be performed on the entity</param>
        /// <param name="id">id for the entity to perform the message</param>
        /// <param name="verb">Verb indicating either POST or PUT</param>
        /// <param name="postBody">posted body, either new entity or message</param>
        /// <param name="nameValueCollection">Query string passed in by caller</param>
        /// <returns>Stream ready for HTTP out</returns>
        internal ActivityRequest GetActivityRequestFromPostOrPut(string resourceNamespace, string message, string id, string verb, Stream postBody, NameValueCollection nameValueCollection)
        {
            string task = this.TryGetActivity(resourceNamespace, verb, message);
            if (!string.IsNullOrWhiteSpace(message))
            {
                this.ValidateAndBuildRequest(id); // need ID if message is passed in, else this is a create scenario and all is good.
            }

            this.ValidateAndBuildRequest(postBody); // invalid JSON body
            return this.BuildActivityRequest(task, id, nameValueCollection);
        }

        /// <summary>
        /// General Get handler for the class NO Flags
        /// </summary>
        /// <param name="resourceNamespace">namespace of the entity</param>
        /// <param name="nameValueCollection">Query string passed in by caller</param>
        /// <returns>Stream ready for HTTP out</returns>
        internal ActivityRequest GetActivityRequestFromNamespaceGet(string resourceNamespace, NameValueCollection nameValueCollection)
        {
            string task = this.TryGetActivity(resourceNamespace, "NAMESPACE:GET", string.Empty);
            return this.BuildActivityRequest(task, string.Empty, nameValueCollection);
        }

        /// <summary>
        /// General Get handler for the class NO Flags
        /// </summary>
        /// <param name="parentNamespace">namespace for parent entity</param>
        /// <param name="parentId">entity id for parent entity</param>
        /// <param name="resourceNamespace">namespace of the entity</param>
        /// <param name="id">id for the entity to get</param>
        /// <param name="nameValueCollection">Query string passed in by caller</param>
        /// <returns>Stream ready for HTTP out</returns>
        internal ActivityRequest GetActivityRequestFromSubResourceGet(string parentNamespace, string parentId, string resourceNamespace, string id, NameValueCollection nameValueCollection) // TODO make override with flags 
        {
            string verbString = "RESOURCE:GET";
            if (nameValueCollection["Flags"] != null && nameValueCollection["Flags"].ToLower(CultureInfo.InvariantCulture).Contains("creatives"))
            {
                verbString += ":CREATIVES";
            }

            if (nameValueCollection["blob"] != null)
            {
                verbString += ":BLOB";

                // on blob gets, the id is in the nameValueCollection
                id = nameValueCollection["blob"];
            }

            if (nameValueCollection["valuations"] != null)
            {
                verbString += ":VALUATIONS";
            }

            string task = this.TryGetActivity(parentNamespace + "/" + resourceNamespace, verbString, string.Empty);
            this.ValidateParentIdAndBuildRequest(parentId);
            this.ValidateAndBuildRequest(id);
            return this.BuildActivityRequest(task, parentId, id, nameValueCollection);
        }

        /// <summary>
        /// General Put/Post handler for the class
        /// </summary>
        /// <param name="parentNamespace">namespace of the entity</param>
        /// <param name="parentId">entity id of the parent entity/resource</param>
        /// <param name="subNamespace">namespace of the entity being posted to</param>
        /// <param name="message">message if any to be performed on the entity</param>
        /// <param name="id">id for the entity to perform the message, empty if new entity</param>
        /// <param name="verb">PUT or POST</param>
        /// <param name="postBody">posted body, either new entity or message</param>
        /// <param name="nameValueCollection">Query string passed in by caller</param>
        /// <returns>Stream ready for HTTP out</returns>
        internal ActivityRequest GetActivityRequestFromSubNamespacePostOrPut(string parentNamespace, string parentId, string subNamespace, string message, string id, string verb, Stream postBody, NameValueCollection nameValueCollection)
        {
            string task = this.TryGetActivity(parentNamespace + "/" + subNamespace, verb, message);
            if (!string.IsNullOrWhiteSpace(message))
            {
                this.ValidateAndBuildRequest(id); // need ID if message is passed in, else this is a create scenario and all is good.
            }

            this.ValidateParentIdAndBuildRequest(parentId);
            this.ValidateAndBuildRequest(postBody); // invalid JSON body
            return this.BuildActivityRequest(task, parentId, id, nameValueCollection);
        }

        /// <summary>
        /// Subnamespace GET handler for the class
        /// </summary>
        /// <param name="parentNamespace">namespace of the entity</param>
        /// <param name="parentId">entity id of the parent entity/resource</param>
        /// <param name="subNamespace">namespace of the entity being posted to</param>
        /// <param name="id">id for the entity to perform the message, empty if new entity</param>
        /// <param name="nameValueCollection">Query string passed in by caller</param>
        /// <returns>Stream ready for HTTP out</returns>
        internal ActivityRequest GetActivityRequestFromSubNamespaceGet(string parentNamespace, string parentId, string subNamespace, string id, NameValueCollection nameValueCollection)
        {
            string task = this.TryGetActivity(parentNamespace + "/" + subNamespace, "NAMESPACE:GET", string.Empty);

            this.ValidateParentIdAndBuildRequest(parentId);
            return this.BuildActivityRequest(task, parentId, id, nameValueCollection);
        }

        /// <summary>
        /// Builds, validates and sets the status of request based on request id 
        /// Validates for valid GUID string
        /// </summary>
        /// <param name="id">External entity id</param>
        internal void ValidateAndBuildRequest(string id)
        {
            this.Context.BuildRequestContext(id);
            this.ValidatePassedInEntityId(id, string.Empty);
        }

        /// <summary>
        /// Builds, validates and sets the status of request based on input stream
        /// </summary>
        /// <param name="id">parent entity id</param>
        internal void ValidateParentIdAndBuildRequest(string id)
        {
            this.Context.BuildRequestParentContext(id);
            this.ValidatePassedInEntityId(id, "Parent ");
        }

        /// <summary>
        /// Builds, validates and sets the status of request based on input stream
        /// </summary>
        /// <param name="inputStream">Request stream</param>
        internal void ValidateAndBuildRequest(Stream inputStream)
        {
            this.Context.BuildRequestContext(inputStream);
            if (string.IsNullOrWhiteSpace(this.Context.RequestData))
            {
                this.SetContextErrorState(HttpStatusCode.BadRequest, "Data not passed in");
            }
        }

        /// <summary>
        /// Builds the json response by inspecting callcontext and sets the response code
        /// This is the only place where the response needs to be built from
        /// </summary>
        /// <param name="result">
        /// Result returned from the activity
        /// </param>
        /// <returns>
        /// Stream that contains the json response to be returned
        /// </returns>
        protected override Stream BuildResponse(ActivityResult result)
        {
            Stream toReturn = null;
            WebContext.OutgoingResponse.ContentType = "application/json";
            WebContext.OutgoingResponse.StatusCode = this.Context.ResponseCode;

            if (this.Context.Success)
            {
                StringBuilder response = new StringBuilder();
                if (result.Values != null && result.Values.Keys.Count > 0)
                {
                    response.Append("{");
                    foreach (KeyValuePair<string, string> entry in result.Values)
                    {
                        if (entry.Key != ActivityResultProcessingTimesKey)
                        {
                            response.Append("\"").Append(entry.Key).Append("\"").Append(":").Append(entry.Value);
                        }
                    }

                    response.Append("}");
                }

                toReturn = WriteResultToStream(response.ToString());
            }
            else
            {
                toReturn = WriteResultToStream(new JavaScriptSerializer().Serialize(this.Context.ErrorDetails));
            }

            return toReturn;
        }

        /// <summary>
        /// builds standard activity request. Payload, AuthUserId, EntityId, Task are always set
        /// </summary>
        /// <param name="activityName">name of the activity to request, must NOT be empty</param>
        /// <param name="id">entityId, this is blank when accessing a namespace</param>
        /// <param name="nameValueCollection">Query string passed in bu caller</param>
        /// <returns>ActivityRequest or null</returns>
        protected ActivityRequest BuildActivityRequest(string activityName, string id, NameValueCollection nameValueCollection)
        {
            if (!this.Context.Success)
            {
                // don't create activity if context has error, bound to fail later down hill
                return null;
            }

            if (string.IsNullOrWhiteSpace(activityName))
            {
                return null;
            }

            var activityRequest = new ActivityRequest
            {
                Task = activityName,
                Values = 
                {
                    { "AuthUserId", this.NameIdentifierClaimValue },
                    { "Payload", this.Context.RequestData }
                }
            };
            if (nameValueCollection != null)
            {
                activityRequest.QueryValues.Add(
                    nameValueCollection.AllKeys
                    .Select(key =>
                        new KeyValuePair<string, string>(key.ToLower(CultureInfo.InvariantCulture), nameValueCollection[key])));
            }

            //// now add the entity id, a new one for creates, the exiting one for messages
            // TODO dont create entityId on Namespace gets, not critical as it is ingnored in the activity.
            string entityId = string.IsNullOrWhiteSpace(id) ? entityId = new EntityId() : id;
            activityRequest.Values.Add("EntityId", entityId);
            return activityRequest;
        }

        /// <summary>
        /// builds standard activity request. Payload, AuthUserId, EntityId, Task are always set
        /// </summary>
        /// <param name="activityName">name of the activity to request, must NOT be empty</param>
        /// <param name="parentId">entityId of the parent resource</param>
        /// <param name="id">entityId, this is blank when accessing a namespace</param>
        /// <param name="nameValueCollection">Query string passed in bu caller</param>
        /// <returns>ActivityRequest or null</returns>
        protected ActivityRequest BuildActivityRequest(string activityName, string parentId, string id, NameValueCollection nameValueCollection)
        {
            if (string.IsNullOrWhiteSpace(parentId))
            {
                return null;
            }

            var activityRequest = this.BuildActivityRequest(activityName, id, nameValueCollection);
            if (activityRequest != null)
            {
                activityRequest.Values.Add("ParentEntityId", parentId);
                if (nameValueCollection != null && nameValueCollection["valuations"] != null)
                {
                    activityRequest.Values.Add("Approved",  nameValueCollection["valuations"].ToUpperInvariant().Equals("APPROVED") ? "true" : "false");
                }
            }

            return activityRequest;
        }

        /// <summary>
        /// Check if the caller has authorization to procedd
        /// </summary>
        /// <param name="url">URL for permission checking</param>
        /// <param name="verb">action verb</param>
        /// <returns>true if permission ok, otherwise false</returns>
        private bool CheckAuthorization(string url, string verb)
        {
            var userId = this.NameIdentifierClaimValue;
            UserEntity user = null;
            try
            {
                // Get the user
                user = Repository.GetUser(new RequestContext(), userId);
            }
            catch (ArgumentException)
            {
                return false;
            }

            var canonicalResource = new CanonicalResource(new Uri(url, UriKind.Absolute), verb);

            if (!AccessResourceHandler.CheckAccess(canonicalResource, user.ExternalEntityId))
            {
                return false;
            }

            return true;
        }

        /// <summary>Verify that the InstanceContextMode is per-call</summary>
        private void VerifyPerCallInstanceContextMode()
        {
            var serviceBehavior = this.GetType().GetCustomAttributes(true).OfType<ServiceBehaviorAttribute>().Single();
            if (serviceBehavior.InstanceContextMode != InstanceContextMode.PerCall)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Services inheriting from ServiceBase must use InstanceContextMode.PerCall. '{0}' uses {1}",
                    this.GetType().FullName,
                    serviceBehavior.InstanceContextMode);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>
        /// validates entity Ids for well-formed and sets errorstate on context shoudl it fail validation
        /// </summary>
        /// <param name="id">entityId passed on URI</param>
        /// <param name="entityHierarchyName">String for substitution in the error message. Parent is used to differentiate the error message between parent and children.</param>
        private void ValidatePassedInEntityId(string id, string entityHierarchyName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                this.SetContextErrorState(HttpStatusCode.BadRequest, entityHierarchyName + "Entity Id not passed in");
                return;
            }

            // Now validate GUID
            Guid parsedGuid;
            if (!Guid.TryParse(id, out parsedGuid))
            {
                this.SetContextErrorState(HttpStatusCode.BadRequest, "Invalid {0}Resource Id".FormatInvariant(entityHierarchyName));
            }
        }
    }
}