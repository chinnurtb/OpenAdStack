//-----------------------------------------------------------------------
// <copyright file="PropertyChangeTypes.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace EntityActivities.Chaining
{
    /// <summary>
    /// Ways in which entity properties can be changed which may result in
    /// chained activity request(s) being submitted.
    /// </summary>
    [Flags]
    public enum PropertyChangeTypes
    {
        /// <summary>No change</summary>
        None = 0x0,
        
        /// <summary>
        /// The original property was null and the updated property is not
        /// </summary>
        Added = 0x1,

        /// <summary>
        /// The original property and the updated property values do not match
        /// </summary>
        Changed = 0x2,

        /// <summary>
        /// The original property was not null and the updated property is
        /// </summary>
        Removed = 0x4,

        /// <summary>
        /// The property was added or the values do not match
        /// </summary>
        AddedOrChanged = Added | Changed,

        /// <summary>
        /// The property was removed or the values do not match
        /// </summary>
        RemovedOrChanged = Removed | Changed,

        /// <summary>
        /// The property was modified in any way
        /// </summary>
        Any = Added | Changed | Removed
    }
}
