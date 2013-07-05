//-----------------------------------------------------------------------
// <copyright file="AppNexusActivityValues.cs" company="Rare Crowds Inc">
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

namespace AppNexusUtilities
{
    /// <summary>
    /// ActivityRequest/ActivityResult value keys for AppNexus activities
    /// </summary>
    public static class AppNexusActivityValues
    {
        /// <summary>AppNexus advertisers</summary>
        public const string Advertisers = "Advertisers";

        /// <summary>AppNexus creatives</summary>
        public const string Creatives = "Creatives";

        /// <summary>AppNexus line-item id</summary>
        public const string LineItemId = "LineItemId";

        /// <summary>AppNexus creative id</summary>
        public const string CreativeId = "CreativeId";

        /// <summary>AppNexus report id</summary>
        public const string ReportId = "ReportId";

        /// <summary>AppNexus campaign start date</summary>
        public const string CampaignStartDate = "CampaignStartDate";

        /// <summary>
        /// Whether the AppNexus report request is to be scheduled again
        /// </summary>
        public const string Reschedule = "Reschedule";

        /// <summary>AppNexus creative audit status</summary>
        public const string AuditStatus = "AuditStatus";

        /// <summary>AppNexus segment data cost CSV</summary>
        public const string DataCostCsv = "DataCostCsv";
    }
}
