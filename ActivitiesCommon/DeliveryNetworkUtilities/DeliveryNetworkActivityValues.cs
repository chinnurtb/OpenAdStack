//-----------------------------------------------------------------------
// <copyright file="DeliveryNetworkActivityValues.cs" company="Rare Crowds Inc">
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
