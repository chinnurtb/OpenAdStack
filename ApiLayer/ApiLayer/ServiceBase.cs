// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceBase.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using Activities;
using ConfigManager;
using Diagnostics;
using Microsoft.IdentityModel.Claims;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Queuing;
using RuntimeIoc.WebRole;
using Utilities.IdentityFederation;
using WorkItems;

namespace ApiLayer
{
    /// <summary>Common base class for ApiLayer services</summary>
    /// <remarks>The ServiceBase assumes InstanceContextMode.PerCall</remarks>
    public abstract class ServiceBase
    {
        /// <summary>The name identifier claim</summary>
        internal const string NameIdentifierClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        /// <summary>Activity result value key for processing times information</summary>
        protected const string ActivityResultProcessingTimesKey = "ProcessingTimes";

        /// <summary>Queuer used to submit activity work items</summary>
        private static IQueuer queuer;

        /// <summary>Claim retriever used to get the name identifier claim</summary>
        private static IClaimRetriever claimRetriever;

        /// <summary>WebContext used to retrieve the query params </summary>
        private static IWebOperationContext webContext;

        /// <summary>Initializes a new instance of the ServiceBase class.</summary>
        protected ServiceBase()
        {
            if (WebOperationContext.Current != null)
            {
                this.VerifyPerCallInstanceContextMode();
            }

            this.NameIdentifierClaimValue = ClaimRetriever.GetClaimValue(NameIdentifierClaim);
            this.Context = new CallContext();
        }

        /// <summary>Gets or sets CallContext for each request</summary>
        public CallContext Context { get; protected set; }

        /// <summary>Gets or sets WebContext.</summary>
        internal static IWebOperationContext WebContext
        {
            get { return webContext ?? new WebOperationContextWrapper(WebOperationContext.Current); }
            set { webContext = value; }
        }

        /// <summary>Gets or sets the queuer</summary>
        internal static IQueuer Queuer
        {
            get { return queuer = queuer ?? RuntimeIocContainer.Instance.Resolve<IQueuer>(); }
            set { queuer = value; }
        }

        /// <summary>Gets or sets the claim retriever</summary>
        internal static IClaimRetriever ClaimRetriever
        {
            get { return claimRetriever = claimRetriever ?? RuntimeIocContainer.Instance.Resolve<IClaimRetriever>(); }
            set { claimRetriever = value; }
        }

        /// <summary>
        /// Gets the time (in milliseconds) to wait between checks for if a
        /// queued work item has been processed.
        /// </summary>
        internal static int QueueResponsePollTime
        {
            get { return Config.GetIntValue("ApiLayer.QueueResponsePollTime"); }
        }

        /// <summary>
        /// Gets the default time (in milliseconds) to wait for a
        /// queued work item to be processed before giving up.
        /// </summary>
        internal static int DefaultMaxQueueResponseWaitTime
        {
            get { return Config.GetIntValue("ApiLayer.MaxQueueResponseWaitTime"); }
        }

        /// <summary>Gets or sets the NameIdentifierClaimValue from HttpContext for this call</summary>
        protected string NameIdentifierClaimValue { get; set; }

        /// <summary>
        /// Gets the time (in milliseconds) to wait for a queued
        /// work item to be processed before giving up.
        /// </summary>
        protected virtual int MaxQueueResponseWaitTime
        {
            get { return DefaultMaxQueueResponseWaitTime; }
        }

        /// <summary>Builds the response from the activity result.</summary>
        /// <remarks>This is the only place from which the response needs to be built.</remarks>
        /// <param name="result">Result returned from the activity</param>
        /// <returns>Stream that contains the json response to be returned</returns>
        protected abstract Stream BuildResponse(ActivityResult result);

        /// <summary>
        /// Builds the json response by inspecting callcontext and sets the response code
        /// This is the only place where the response needs to be built from
        /// </summary>        
        /// <param name="result">Result returned from the activity</param>
        /// <param name="writer">Text writer to which the response is to be written</param>
        protected virtual void WriteResponse(ActivityResult result, TextWriter writer)
        {
            if (WebOperationContext.Current != null)
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = this.Context.ResponseCode;
                WebOperationContext.Current.OutgoingResponse.ContentType = "application/json";
            }

            if (this.Context.Success)
            {
                if (result.Values != null && result.Values.Keys.Count > 0)
                {
                    writer.Write("{");
                    foreach (KeyValuePair<string, string> entry in result.Values)
                    {
                        writer.Write(@"""{0}"":{1}", entry.Key, entry.Value);
                    }

                    writer.Write("}");
                }
            }
            else
            {
                writer.Write(JsonConvert.SerializeObject(this.Context.ErrorDetails));
            }
        }

        /// <summary>Runs an activity request and builds a response</summary>
        /// <param name="request">The request to run</param>
        /// <param name="fetchOnly">True if the operation is just a fetch; otherwise, false.</param>
        /// <returns>Stream containing the JSON response.</returns>
        protected Stream ProcessActivity(ActivityRequest request, bool fetchOnly)
        {
            return this.ProcessActivity(request, fetchOnly, this.MaxQueueResponseWaitTime);
        }

        /// <summary>Runs an activity request and builds a response</summary>
        /// <param name="request">The request to run</param>
        /// <param name="fetchOnly">True if the operation is just a fetch; otherwise, false.</param>
        /// <param name="activityResultTimeout">How long to wait for an activity result.</param>
        /// <returns>Stream containing the JSON response.</returns>
        protected Stream ProcessActivity(ActivityRequest request, bool fetchOnly, long activityResultTimeout)
        {
            if (request == null)
            {
                this.SetContextErrorState(HttpStatusCode.InternalServerError, "Error while creating activity request");
                return this.BuildResponse(null);
            }

            var submitTime = DateTime.UtcNow;
            var result = this.RunActivity(request, fetchOnly, activityResultTimeout);
            if (this.Context.Success)
            {
                if (result == null)
                {
                    this.SetContextErrorState(HttpStatusCode.InternalServerError, "Error while processing activity in ActivityLayer");
                }
                else if (!result.Succeeded)
                {
                    if (result.Error.ErrorId == (int)ActivityErrorId.InvalidEntityId)
                    {
                        this.SetContextErrorState(
                            HttpStatusCode.NotFound,
                            "Invalid entity id");
                    }
                    else if (result.Error.ErrorId == (int)ActivityErrorId.UserAccessDenied)
                    {
                        this.SetContextErrorState(
                            HttpStatusCode.Unauthorized,
                            "Access denied");
                    }
                    else if (result.Error.ErrorId == (int)ActivityErrorId.MissingRequiredInput)
                    {
                        this.SetContextErrorState(
                            HttpStatusCode.BadRequest,
                            "Missing required input: {0}",
                            result.Error.Message);
                    }
                    else
                    {
                        this.SetContextErrorState(
                            HttpStatusCode.InternalServerError,
                            "Error while processing activity in ActivityLayer ({0})",
                            result.Error.ErrorId);
                    }
                }
            }

            var buildResponseStartTime = DateTime.UtcNow;
            var response = this.BuildResponse(result);
            var buildResponseTime = DateTime.UtcNow - buildResponseStartTime;

            this.LogProcessedActivityStats(result, submitTime, buildResponseTime);

            return response;
        }

        /// <summary>
        /// Creates a work item for the activity, submits it to the queue and 
        /// waits for a response.
        /// </summary>
        /// <param name="request">Request for the activity to be run</param>
        /// <param name="fetchOnly">True if the operation is just a fetch; otherwise, false.</param>
        /// <param name="activityResultTimeout">How long to wait for the activity result.</param>
        /// <returns>The result of processing the activity</returns>     
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011", Justification = "Will have to check if this can be resolved")]
        protected ActivityResult RunActivity(ActivityRequest request, bool fetchOnly, long activityResultTimeout)
        {
            WorkItem workItem = new WorkItem
            {
                Id = request.Id,
                Category = (fetchOnly ?
                           ActivityRuntimeCategory.InteractiveFetch :
                           ActivityRuntimeCategory.Interactive)
                           .ToString(),
                ResultType = WorkItemResultType.Polled,
                Source = this.GetType().FullName,
                Content = request.SerializeToXml()
            };

            if (!Queuer.EnqueueWorkItem(ref workItem))
            {
                this.SetContextErrorState(HttpStatusCode.InternalServerError, "Unable to queue message");
                return null;
            }

            var enqueuedTime = DateTime.UtcNow;
            while (workItem.Status != WorkItemStatus.Processed && workItem.Status != WorkItemStatus.Failed)
            {
                if ((DateTime.UtcNow - enqueuedTime).TotalMilliseconds > activityResultTimeout)
                {
                    this.Context.Success = false;

                    // TODO : Hide this and pass some other message to api call; need better message
                    this.SetContextErrorState(HttpStatusCode.Accepted, "Message Accepted and Queued successfully");
                    return null;
                }

                Thread.Sleep(QueueResponsePollTime);
                workItem = Queuer.CheckWorkItem(workItem.Id);
            }

            var result = ActivityResult.DeserializeFromXml(workItem.Result);

            // Add processing times to the result for performance auditing
            var processingTimes =
                "in queue: {0}s; in processing: {1}s; results awaiting retrieval: {2}s"
                .FormatInvariant(
                    workItem.TimeInQueue.TotalSeconds,
                    workItem.TimeInProcessing.TotalSeconds,
                    (DateTime.UtcNow - workItem.ProcessingCompleteTime).TotalSeconds);
            result.Values.Add(ActivityResultProcessingTimesKey, processingTimes);

            return result;
        }

        /// <summary>
        /// Sets the context error state
        /// </summary>
        /// <param name="httpStatus">http response desired to responde to client</param>
        /// <param name="responseMessage">error message passed in http response</param>
        /// <param name="responseMessageArgs">args for the error message</param>
        protected void SetContextErrorState(HttpStatusCode httpStatus, string responseMessage, params object[] responseMessageArgs)
        {
            this.Context.Success = false;
            this.Context.ResponseCode = httpStatus;
            this.Context.ErrorDetails.Message = responseMessage.FormatInvariant(responseMessageArgs);
        }

        /// <summary>Verify that the InstanceContextMode is per-call</summary>
        private void VerifyPerCallInstanceContextMode()
        {
            var serviceBehavior = this.GetType().GetCustomAttributes(true).OfType<ServiceBehaviorAttribute>().Single();
            if (serviceBehavior.InstanceContextMode != InstanceContextMode.PerCall)
            {
                var message = "Services inheriting from ServiceBase must use InstanceContextMode.PerCall. '{0}' uses {1}"
                    .FormatInvariant(this.GetType().FullName, serviceBehavior.InstanceContextMode);
                throw new InvalidOperationException(message);
            }
        }

        /// <summary>Logs statistics about the processed activity</summary>
        /// <param name="result">The activity result</param>
        /// <param name="submitTime">Time when the activity was submitted</param>
        /// <param name="buildResponseTime">Time taken to build the response</param>
        private void LogProcessedActivityStats(ActivityResult result, DateTime submitTime, TimeSpan buildResponseTime)
        {
            var processingTimes =
                (result != null && result.Values != null && result.Values.ContainsKey(ActivityResultProcessingTimesKey)) ?
                result.Values[ActivityResultProcessingTimesKey] : "(unavailable)";

            var processedLogEntryFormat =
@"Processed {0} request - {1} {2}
Processing times: {3}
Build response time: {4}s
Total time: {5}s";
            LogManager.Log(
                LogLevels.Trace,
                processedLogEntryFormat,
                this.GetType().Name,
                HttpContext.Current != null ? HttpContext.Current.Request.HttpMethod : string.Empty,
                HttpContext.Current != null ? HttpContext.Current.Request.RawUrl : "(unavailable)",
                result != null ? result.RequestId : "(unavailable)",
                processingTimes,
                buildResponseTime.TotalSeconds,
                (DateTime.UtcNow - submitTime).TotalSeconds);
        }
    }
}