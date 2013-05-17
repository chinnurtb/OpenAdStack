//Copyright 2012-2013 Rare Crowds, Inc.
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
