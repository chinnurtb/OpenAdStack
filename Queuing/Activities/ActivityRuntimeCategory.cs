//-----------------------------------------------------------------------
// <copyright file="ActivityRuntimeCategory.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Activities
{
    /// <summary>Categories of activities by their runtime environment</summary>
    public enum ActivityRuntimeCategory
    {
        /// <summary>
        /// Submitted from an interractive session and
        /// operation is a simple fetch
        /// </summary>
        InteractiveFetch,

        /// <summary>
        /// Submitted from an interractive session and
        /// operation is more than a simple fetch
        /// </summary>
        Interactive,

        /// <summary>
        /// Submitted by automation (non-interractive)
        /// and operation is a simple fetch
        /// </summary>
        BackgroundFetch,

        /// <summary>
        /// Submitted by automation (non-interractive)
        /// and operation is more than a simple fetch
        /// </summary>
        Background
    }
}
