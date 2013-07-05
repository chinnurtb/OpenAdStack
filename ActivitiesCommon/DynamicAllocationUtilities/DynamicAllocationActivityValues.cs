//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationActivityValues.cs" company="Rare Crowds Inc">
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

namespace DynamicAllocationUtilities
{
    /// <summary>
    /// ActivityRequest/ActivityResult value keys for Dynamic Allocation activities
    /// </summary>
    public static class DynamicAllocationActivityValues
    {
        /// <summary>AppNexus campaign start date</summary>
        public const string AppNexusCampaignStartDate = "CampaignStartDate";

        /// <summary>Start date for the period being allocated</summary>
        public const string AllocationStartDate = "AllocationStartDate";

        /// <summary>EntityId of the allocations to export</summary>
        public const string ExportAllocationsEntityId = "ExportAllocationsEntityId";

        /// <summary>Whether the allocation is for the initialization phase</summary>
        public const string IsInitialAllocation = "IsInitialAllocation";

        /// <summary>Measure cost types (aka 'provider') to include</summary>
        public const string IncludeCostTypes = "IncludeCostTypes";

        /// <summary>Measure cost types (aka 'provider') to exclude</summary>
        public const string ExcludeCostTypes = "ExcludeCostTypes";
    }
}
