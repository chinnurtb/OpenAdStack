//-----------------------------------------------------------------------
// <copyright file="RoleInstanceState.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Runtime
{
    /// <summary>Role Instance States</summary>
    public enum RoleInstanceState
    {
        /// <summary>Unknown state</summary>
        Unknown,

        /// <summary>Role instance is launching</summary>
        /// <remarks>IRunners are being initialized</remarks>
        Launching,

        /// <summary>Role instance is running</summary>
        /// <remarks>IRunners are running.</remarks>
        Running,

        /// <summary>Role instance has landed</summary>
        /// <remarks>All IRunners have stopped work and exited.</remarks>
        Landed
    }
}
