//-----------------------------------------------------------------------
// <copyright file="AppNexusValues.cs" company="Rare Crowds Inc">
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

namespace AppNexusClient
{
    /// <summary>Names of the values in AppNexus objects</summary>
    /// <remarks>
    /// Only includes the subset of possible AppNexus values that are used.
    /// For maintainability, all values used must be referred to using these
    /// constants and not hard-coded, per-use strings.
    /// </remarks>
    public static class AppNexusValues
    {
        /// <summary>Token returned from auth requests</summary>
        public const string AuthToken = "token";

        /// <summary>Element name in error responses</summary>
        public const string Error = "error";

        /// <summary>Error id property in error responses</summary>
        public const string ErrorId = "error_id";

        /// <summary>Error code property in error responses</summary>
        public const string ErrorCode = "error_code";

        /// <summary>Object id</summary>
        /// <remarks>Assigned by AppNexus when objects are created</remarks>
        public const string Id = "id";

        /// <summary>Object name</summary>
        public const string Name = "name";

        /// <summary>Object code</summary>
        /// <remarks>
        /// Provided by the caller and primarily used for associating AppNexus
        /// objects with the caller's own objects.
        /// </remarks>
        public const string Code = "code";

        /// <summary>
        /// Object state.
        /// Valid values: StateActive, StateInactive
        /// </summary>
        /// <remarks>In most objects</remarks>
        public const string State = "state";

        /// <summary>State value: "active"</summary>
        public const string StateActive = "active";

        /// <summary>State value: "active"</summary>
        public const string StateInactive = "inactive";

        /// <summary>Start Date</summary>
        /// <remarks>In line-item and campaign</remarks>
        public const string StartDate = "start_date";

        /// <summary>End Date</summary>
        /// <remarks>In line-item and campaign</remarks>
        public const string EndDate = "end_date";

        /// <summary>Campaigns (in line-item)</summary>
        public const string Campaigns = "campaigns";

        /// <summary>Report id (in request report response)</summary>
        public const string ReportId = "report_id";

        /// <summary>Report information (in retrieve report response)</summary>
        public const string Report = "report";

        /// <summary>
        /// Created on timestamp
        /// (in retrieve report reponse report information)
        /// </summary>
        public const string CreatedOn = "created_on";

        /// <summary>
        /// Report data (CSV)
        /// (in retrieve report reponse report information)
        /// </summary>
        public const string Data = "data";

        /// <summary>
        /// Execution status (in retrieve report response)
        /// Valid values: ExecutionStatusReady, ExecutionStatusPending
        /// </summary>
        public const string ExecutionStatus = "execution_status";

        /// <summary>Execution status value: "ready"</summary>
        public const string ExecutionStatusReady = "ready";

        /// <summary>Execution status value: "pending"</summary>
        /// <remarks>TODO: Verify this is correct</remarks>
        public const string ExecutionStatusPending = "pending";

        /// <summary>AppNexus Member</summary>
        public const string Member = "member";

        /// <summary>AppNexus Segment</summary>
        public const string Segment = "segment";

        /// <summary>AppNexus Segments</summary>
        public const string Segments = "segments";

        /// <summary>AppNexus Advertiser</summary>
        public const string Advertiser = "advertiser";

        /// <summary>AppNexus Advertisers</summary>
        public const string Advertisers = "advertisers";

        /// <summary>AppNexus domain lists</summary>
        public const string DomainLists = "domain-lists";

        /// <summary>AppNexus LineItem</summary>
        public const string LineItem = "line-item";

        /// <summary>AppNexus Profile</summary>
        public const string Profile = "profile";

        /// <summary>AppNexus Profile Id</summary>
        public const string ProfileId = "profile_id";

        /// <summary>AppNexus Campaign</summary>
        public const string Campaign = "campaign";

        /// <summary>AppNexus Creative</summary>
        public const string Creative = "creative";

        /// <summary>AppNexus Creative Collection</summary>
        public const string Creatives = "creatives";

        /// <summary>AppNexus Creative Format Collection</summary>
        public const string CreativeFormats = "creative-formats";

        /// <summary>AppNexus Creative Template Collection</summary>
        public const string CreativeTemplates = "templates";

        /// <summary>AppNexus Format</summary>
        public const string Format = "format";

        /// <summary>Creative audit status</summary>
        public const string AuditStatus = "audit_status";

        /// <summary>Debug information</summary>
        public const string DebugInfo = "dbg_info";

        /// <summary>Lifetime budget</summary>
        public const string LifetimeBudget = "lifetime_budget";
    }
}
