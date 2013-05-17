// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppNexusAppService.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml;
using Activities;
using AppNexusUtilities;
using DataServiceUtilities;
using DynamicAllocationUtilities;
using EntityUtilities;
using Newtonsoft.Json;

namespace ApiLayer
{
    /// <summary>Service for AppNexus App specific functionality</summary>
    /// <remarks>The ServiceBase assumes InstanceContextMode.PerCall</remarks>
    [ServiceContract]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class AppNexusAppService : ServiceBase
    {
        /// <summary>Registration service POST handler</summary>
        /// <param name="postBody">JSON string of the post</param>
        /// <returns>Response stream for HTTP response</returns>
        [WebInvoke(UriTemplate = "register", Method = "POST")]
        public Stream RegisterUser(Stream postBody)
        {
            this.Context.BuildRequestContext(postBody);
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.AppUserRegistration,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.NameIdentifierClaimValue },
                    { EntityActivityValues.MessagePayload, this.Context.RequestData }
                }
            };
            return this.ProcessActivity(request, false);
        }

        /// <summary>GET handler for AppNexus app creatives</summary>
        /// <returns>Response stream for HTTP response</returns>
        [WebGet(UriTemplate = "creatives")]
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Should be method rather than property for consistency")]
        public Stream GetCreatives()
        {
            var companyId = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters["Company"];
            var campaignId = WebContext.IncomingRequest.UriTemplateMatch.QueryParameters["Campaign"];
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.GetCreatives,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.NameIdentifierClaimValue },
                    { EntityActivityValues.CompanyEntityId, companyId },
                    { EntityActivityValues.CampaignEntityId, campaignId },
                }
            };
            return this.ProcessActivity(request, false);
        }

        /// <summary>GET handler for AppNexus app segment data costs CSV</summary>
        /// <returns>Response stream for HTTP response</returns>
        [WebGet(UriTemplate = "datacost.csv")]
        [SuppressMessage("Microsoft.Design", "CA1024", Justification = "Should be method rather than property for consistency")]
        public Stream GetDataCostCsv()
        {
            var request = new ActivityRequest
            {
                Task = AppNexusActivityTasks.GetDataCostCsv,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.NameIdentifierClaimValue }
                }
            };
            return this.ProcessActivity(request, false, 30000);
        }

        /// <summary>Builds the data response from the activity result.</summary>
        /// <remarks>This is the only place from which the response needs to be built.</remarks>
        /// <param name="result">Result returned from the activity</param>
        /// <returns>Stream that contains the json response to be returned</returns>
        protected override Stream BuildResponse(ActivityResult result)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                this.WriteResponse(result, writer);
                writer.Flush();
                return new MemoryStream(Encoding.UTF8.GetBytes(writer.ToString()));
            }
        }

        /// <summary>
        /// Writes the data service results to the response
        /// and sets the status code and content type
        /// </summary>        
        /// <param name="result">Result returned from the activity</param>
        /// <param name="writer">Text writer to which the response is to be written</param>
        protected override void WriteResponse(ActivityResult result, TextWriter writer)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = this.Context.ResponseCode;
            WebOperationContext.Current.OutgoingResponse.Headers.Add(HttpResponseHeader.CacheControl, "private,no-cache");
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json";

            if (result != null && result.Values != null && result.Values.Count > 0)
            {
                writer.Write(result.Values.First().Value);
            }
        }
    }
}