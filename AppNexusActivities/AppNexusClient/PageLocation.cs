//-----------------------------------------------------------------------
// <copyright file="PageLocation.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace AppNexusClient
{
    /// <summary>Page location targeting</summary>
    public enum PageLocation : int
    {
        /// <summary>Unknown position</summary>
        Unknown = 0,

        /// <summary>Allow any position</summary>
        Any = 1,

        /// <summary>Above the fold</summary>
        Above = 2,

        /// <summary>Below the fold</summary>
        Below = 3
    }
}
