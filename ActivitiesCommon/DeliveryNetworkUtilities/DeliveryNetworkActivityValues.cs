//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkActivityValues.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DeliveryNetworkUtilities
{
    /// <summary>
    /// ActivityRequest/ActivityResult value keys for delivery network activities/schedulers
    /// </summary>
    public static class DeliveryNetworkActivityValues
    {
        /// <summary>Delivery network</summary>
        /// <see cref="DynamicAllocation.DeliveryNetworkDesignation"/>
        public const string DeliveryNetwork = "DeliveryNetwork";

        /// <summary>Report ID</summary>
        public const string ReportId = "ReportId";

        /// <summary>Whether to reschedule a report request</summary>
        public const string RescheduleReportRequest = "Reschedule";

        /// <summary>Ids of the allocations successfully exported</summary>
        public const string ExportedAllocationIds = "ExportedAllocationIds";
    }
}
