//-----------------------------------------------------------------------
// <copyright file="UserType.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace EntityUtilities
{
    /// <summary>Types of users</summary>
    public enum UserType
    {
        /// <summary>Unknown user type</summary>
        Unknown,

        /// <summary>Stand-alone user</summary>
        /// <remarks>Logs into rarecrowds.com directly via ACS</remarks>
        StandAlone,

        /// <summary>AppNexus App user</summary>
        /// <remarks>Logs in via AppNexus console single-sign-on</remarks>
        AppNexusApp,

        /// <summary>Default user type (stand-alone)</summary>
        Default = StandAlone,
    }
}
