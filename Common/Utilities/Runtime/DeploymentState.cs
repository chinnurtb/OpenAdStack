//-----------------------------------------------------------------------
// <copyright file="DeploymentState.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Utilities.Runtime
{
    /// <summary>Deployment States</summary>
    public enum DeploymentState
    {
        /// <summary>Unknown state</summary>
        Unknown,

        /// <summary>Deployment has launched</summary>
        /// <remarks>This is the normal, running state.</remarks>
        Launched,

        /// <summary>Deployment is landing</summary>
        /// <remarks>IRunners need to stop working and exit.</remarks>
        Landing,

        /// <summary>Deployment has landed</summary>
        /// <remarks>
        /// Set by deployment script after confirming
        /// that all role instances have landed.
        /// </remarks>
        Landed
    }
}
