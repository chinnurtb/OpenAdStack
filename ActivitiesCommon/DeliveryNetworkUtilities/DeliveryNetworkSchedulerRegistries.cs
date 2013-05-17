//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkSchedulerRegistries.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// TimeSlottedRegistry names for DynamicAllocation scheduled activity dispatchers
    /// </summary>
    public static class DeliveryNetworkSchedulerRegistries
    {
        /// <summary>Campaigns to export TimeSlottedRegistry name</summary>
        public const string CampaignsToExport = "delivery-campaigns-toexport";

        /// <summary>Campaigns to cleanup TimeSlottedRegistry name</summary>
        public const string CampaignsToCleanup = "delivery-campaigns-tocleanup";

        /// <summary>Creatives to export TimeSlottedRegistry name</summary>
        public const string CreativesToExport = "delivery-creatives-toexport";

        /// <summary>Creatives to update TimeSlottedRegistry name</summary>
        public const string CreativesToUpdate = "delivery-creatives-toupdate";

        /// <summary>Reports to request TimeSlottedRegistry name</summary>
        public const string ReportsToRequest = "delivery-reports-torequest";

        /// <summary>Reports to retrieve TimeSlottedRegistry name</summary>
        public const string ReportsToRetrieve = "delivery-reports-toretrieve";
    }
}
