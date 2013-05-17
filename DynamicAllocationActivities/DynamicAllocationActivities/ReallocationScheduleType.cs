// -----------------------------------------------------------------------
// <copyright file="ReallocationScheduleType.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace DynamicAllocationActivities
{
    /// <summary>
    /// Defines types of reallocations that may be scheduled
    /// </summary>
    public enum ReallocationScheduleType
    {
        /// <summary>Initial allocation</summary>
        /// <remarks>
        /// Schedule (re)allocation to occur at the campaign start
        /// or immediately if campaign start has already passed
        /// </remarks>
        Initial,

        /// <summary>First reallocation</summary>
        /// <remarks>
        /// Schedule reallocation to occur at the end of initialization phase
        /// </remarks>
        FirstReallocation,

        /// <summary>Regular reallocation</summary>
        /// <remarks>
        /// Schedule for the next occurance of the regular reallocation intervals
        /// </remarks>
        RegularReallocation,
    }
}
