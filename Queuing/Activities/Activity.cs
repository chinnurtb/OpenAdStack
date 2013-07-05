//-----------------------------------------------------------------------
// <copyright file="Activity.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using ConfigManager;
using Diagnostics;

namespace Activities
{
    /// <summary>Delegate for submitting activity requests from within activities</summary>
    /// <param name="request">The activity request to submit</param>
    /// <param name="sourceName">Used to look up this activity when the result is ready</param>
    /// <returns>True if the request was submitted successfully; otherwise, false.</returns>
    public delegate bool SubmitActivityRequestHandler(ActivityRequest request, string sourceName);

    /// <summary>
    /// Base class for activities
    /// </summary>
    public abstract class Activity
    {
        /// <summary>Names of values required in the activity request</summary>
        /// <remarks>This comes from the RequiredValuesAttribute on the Activity</remarks>
        private string[] requiredRequestValues;

        /// <summary>Names of values allowed in the activity result</summary>
        /// <remarks>This comes from the ResultValuesAttribute on the Activity</remarks>
        private string[] allowedResultValues;

        /// <summary>Delgate used to submit activity requests from within activities</summary>
        private SubmitActivityRequestHandler submitActivityRequest;

        /// <summary>Backing field for ActivityHandlerFactory</summary>
        private IActivityHandlerFactory defaultActivityHandlerFactory = new DefaultActivityHandlerFactory();

        /// <summary>Initializes a new instance of the Activity class.</summary>
        protected Activity()
        {
            this.Name = this.GetType()
                .GetCustomAttributes(typeof(NameAttribute), false)
                .Cast<NameAttribute>()
                .Single()
                .Value;
        }

        /// <summary>Gets the name of the activity</summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the runtime category of the activity.
        /// Default is ActivityRuntimeCategory.Interractive.
        /// Override in activities as appropriate.
        /// </summary>
        public virtual ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.Interactive; }
        }

        /// <summary>
        /// Gets or sets the activity handler factory
        /// </summary>
        protected virtual IActivityHandlerFactory ActivityHandlerFactory
        {
            get { return this.defaultActivityHandlerFactory; }
            set { this.defaultActivityHandlerFactory = value; }
        }

        /// <summary>Gets the context for the activity to use</summary>
        protected IDictionary<Type, object> Context { get; private set; }

        /// <summary>Gets the number of time to retry submitting requests from within activities</summary>
        private static int SubmitRequestRetries
        {
            get { return Config.GetIntValue("Activities.SubmitRequestRetries"); }
        }

        /// <summary>Gets the the time (in milliseconds) to wait before retrying to submit requests</summary>
        private static int SubmitRequestRetryWait
        {
            get { return Config.GetIntValue("Activities.SubmitRequestRetryWait"); }
        }

        /// <summary>Factory method for activities</summary>
        /// <param name="activityHandlerFactory">An activity handler factory.</param>
        /// <param name="activityType">Type of activity to create</param>
        /// <param name="context">Context for the activity to use</param>
        /// <param name="submitActivityRequest">Delegate used to submit new requests</param>
        /// <returns>The activity</returns>
        public static Activity CreateActivity(IActivityHandlerFactory activityHandlerFactory, Type activityType, IDictionary<Type, object> context, SubmitActivityRequestHandler submitActivityRequest)
        {
            var activity = CreateActivity(activityType, context, submitActivityRequest);

            // Allow a factory to be injected at create time.
            activity.ActivityHandlerFactory = activityHandlerFactory;
            return activity;
        }

        /// <summary>Factory method for activities</summary>
        /// <param name="activityType">Type of activity to create</param>
        /// <param name="context">Context for the activity to use</param>
        /// <param name="submitActivityRequest">Delegate used to submit new requests</param>
        /// <returns>The activity</returns>
        public static Activity CreateActivity(Type activityType, IDictionary<Type, object> context, SubmitActivityRequestHandler submitActivityRequest)
        {
            // Check that type is actually a valid activity
            if (!typeof(Activity).IsAssignableFrom(activityType) || activityType.IsAbstract)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "The type '{0}' does not inherit from Activity or is abstract", activityType.FullName);
                throw new ArgumentException(message, "activityType");
            }

            // Check for required NameAttribute
            var attributes = activityType.GetCustomAttributes(true);
            if (attributes.OfType<NameAttribute>().Count() == 0)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "The type '{0}' does not have a Name attribute", activityType.FullName);
                throw new ArgumentException(message, "activityType");
            }

            // Check for required SubmitActivityRequestHandler
            if (submitActivityRequest == null)
            {
                throw new ArgumentNullException("submitActivityRequest");
            }

            // Create the activity and initialize its peroperties
            var activity = (Activity)Activator.CreateInstance(activityType);
            activity.Context = context;
            activity.submitActivityRequest = submitActivityRequest;
            activity.requiredRequestValues = attributes
                .OfType<RequiredValuesAttribute>()
                .SelectMany(a => a.ValueNames)
                .ToArray();
            activity.allowedResultValues = attributes
                .OfType<ResultValuesAttribute>()
                .SelectMany(a => a.ValueNames)
                .ToArray();

            return activity;
        }

        /// <summary>Runs the activity using the provided request</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Returning ErrorResult is equivalent to rethrowing")]
        public ActivityResult Run(ActivityRequest request)
        {
            try
            {
                this.CheckForRequiredValues(request);
            }
            catch (ArgumentException ae)
            {
                return this.ErrorResult(ActivityErrorId.MissingRequiredInput, ae);
            }

            ActivityResult result = null;
            try
            {
                result = this.ProcessRequest(request);
                result.RequestId = request.Id;
                result.Task = request.Task;
            }
            catch (Exception e)
            {
                return this.ErrorResult(ActivityErrorId.GenericError, e);
            }

            try
            {
                this.CheckForAllowedResultValues(result);
            }
            catch (ArgumentException ae)
            {
                return this.ErrorResult(ActivityErrorId.UnknownResultValue, ae);
            }

            return result;
        }

        /// <summary>Override to handle results of submitted requests.</summary>
        /// <param name="result">The result of the previously submitted work item</param>
        public virtual void OnActivityResult(ActivityResult result)
        {
            LogManager.Log(
                LogLevels.Error,
                "Activities that submit requests must override Activity.OnActivityResult(ActivityResult). The activity '{0}' ({1}) does not.",
                this.Name,
                this.GetType().FullName);
        }

        /// <summary>Creates a successful result without result values</summary>
        /// <returns>The success result</returns>
        protected ActivityResult SuccessResult()
        {
            return SuccessResult(null);
        }

        /// <summary>Creates a successful result with the provided result values</summary>
        /// <param name="values">The result values</param>
        /// <returns>The success result</returns>
        protected ActivityResult SuccessResult(Dictionary<string, string> values)
        {
            return new ActivityResult
            {
                Task = this.Name,
                Succeeded = true,
                Values = values
            };
        }

        /// <summary>Creates an unsuccessful result with the provided error id and exception</summary>
        /// <param name="exception">The exception</param>
        /// <returns>The error result</returns>
        protected ActivityResult ErrorResult(ActivityException exception)
        {
            if (exception == null)
            {
                return this.ErrorResult(ActivityErrorId.None, (Exception)null);
            }

            return this.ErrorResult(exception.ActivityErrorId, exception.ToString());
        }

        /// <summary>Creates an unsuccessful result with the provided error id and exception</summary>
        /// <param name="errorId">The error id</param>
        /// <param name="exception">The exception</param>
        /// <returns>The error result</returns>
        protected ActivityResult ErrorResult(ActivityErrorId errorId, Exception exception)
        {
            return this.ErrorResult(errorId, exception != null ? exception.ToString() : null);
        }

        /// <summary>Creates an unsuccessful result with the provided error id and message</summary>
        /// <param name="errorId">The error id</param>
        /// <param name="message">The error message</param>
        /// <param name="args">Args for the message</param>
        /// <returns>The error result</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Creation of error results must not thrown")]
        protected ActivityResult ErrorResult(ActivityErrorId errorId, string message, params object[] args)
        {
            var errorMessage = string.Empty;
            try
            {
                errorMessage = message.FormatInvariant(args);
            }
            catch
            {
                errorMessage = message ?? string.Empty;
            }

            return new ActivityResult
            {
                Task = this.Name,
                Succeeded = false,
                Error =
                {
                    ErrorId = (int)errorId,
                    Message = errorMessage
                }
            };
        }

        /// <summary>Submits a new activity request from within an activity</summary>
        /// <param name="request">The request to submit</param>
        /// <param name="retry">Whether to retry failed submissions</param>
        /// <returns>True if the request was submitted successfully; otherwise, false</returns>
        protected bool SubmitRequest(ActivityRequest request, bool retry)
        {
            var retries = SubmitRequestRetries;

            do
            {
                if (this.submitActivityRequest(request, this.Name))
                {
                    return true;
                }

                Thread.Sleep(SubmitRequestRetryWait);
            }
            while (retry && retries-- > 0);

            return false;
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected abstract ActivityResult ProcessRequest(ActivityRequest request);

        /// <summary>Checks that the request contains all of the required values</summary>
        /// <param name="request">The request</param>
        /// <exception cref="ArgumentException">Thrown if the request is missing any required values</exception>
        private void CheckForRequiredValues(ActivityRequest request)
        {
            // Check for values that are in the required request values that are not in the request
            var requestValues = request.Values.Keys;
            var missingValues = this.requiredRequestValues.Except(requestValues).Intersect(this.requiredRequestValues);
            if (missingValues.Count() > 0)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Request did not contain one or more required values: {0}",
                    string.Join(", ", missingValues));
                throw new ArgumentException(message, "request");
            }
        }

        /// <summary>Checks that the result only contains allowed values</summary>
        /// <param name="result">The results</param>
        /// <exception cref="ArgumentException">
        /// Thrown if any of the result values are not in the list of allowed values
        /// </exception>
        private void CheckForAllowedResultValues(ActivityResult result)
        {
            if (this.allowedResultValues.Length == 0 || result.Values == null || result.Values.Count == 0)
            {
                return;
            }

            // Check for values in the result that are not in the allowed result values
            var resultValues = result.Values.Keys;
            var unknownValues = resultValues.Except(this.allowedResultValues).Intersect(resultValues);
            if (unknownValues.Count() > 0)
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture,
                    "Result contained one or more unknown values: {0}",
                    string.Join(", ", unknownValues));
                throw new ArgumentException(message, "result");
            }
        }
    }
}
