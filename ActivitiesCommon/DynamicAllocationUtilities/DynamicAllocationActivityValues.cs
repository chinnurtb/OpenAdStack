//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationActivityValues.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

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
    }
}
