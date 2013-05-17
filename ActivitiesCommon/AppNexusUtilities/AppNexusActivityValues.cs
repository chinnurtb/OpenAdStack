//-----------------------------------------------------------------------
// <copyright file="AppNexusActivityValues.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
