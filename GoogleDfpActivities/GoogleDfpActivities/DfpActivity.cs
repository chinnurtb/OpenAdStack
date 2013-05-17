//-----------------------------------------------------------------------
// <copyright file="DfpActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Activities;
using DataAccessLayer;
using Diagnostics;
using EntityActivities;
using EntityUtilities;
using GoogleDfpClient;

namespace GoogleDfpActivities
{
    /// <summary>Base class for Google DFP related activities</summary>
    public abstract class DfpActivity : EntityActivity
    {
        /// <summary>
        /// Creates an entity repository RequestContext from an ActivityRequest
        /// </summary>
        /// <param name="request">The request containing context information</param>
        /// <returns>The created request context</returns>
        internal static RequestContext CreateContext(ActivityRequest request)
        {
            // Default behavior will be not to get or save extended properties.
            // This is the most consistent with current behavior for these activites
            return CreateRepositoryContext(
                new RepositoryEntityFilter(true, true, false, true),
                request,
                EntityActivityValues.CompanyEntityId);
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            try
            {
                return this.ProcessDfpRequest(request);
            }
            catch (GoogleDfpClientException dfpe)
            {
                StringBuilder message = new StringBuilder();
                Exception e = dfpe;
                while (e != null)
                {
                    message.AppendLine(e.ToString());
                    e = e.InnerException;
                    if (e != null)
                    {
                        message.AppendLine("----------------------------------------");
                    }
                }

                LogManager.Log(LogLevels.Error, message.ToString());
                return this.DfpClientError(dfpe);
            }
        }

        /// <summary>Processes the AppNexus request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected abstract ActivityResult ProcessDfpRequest(ActivityRequest request);

        /// <summary>Creates an error ActivityResult for the exception</summary>
        /// <param name="exception">The exception</param>
        /// <returns>The error result</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "This is specifically for GoogleDfpClientExceptions")]
        protected ActivityResult DfpClientError(GoogleDfpClientException exception)
        {
            return this.ErrorResult(
                ActivityErrorId.GenericError,
                "Google DFP Client Error: {0}",
                exception);
        }
    }
}
