// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropsCommand.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Utilities.DeploymentProps
{
    /// <summary>Available commands</summary>
    public enum PropsCommand
    {
        /// <summary>Unknown command</summary>
        Unknown,

        /// <summary>Get a deployment property</summary>
        /// <remarks>Requires -o</remarks>
        Get,

        /// <summary>Set a deployment property</summary>
        /// <remarks>Requires -i</remarks>
        Set,

        /// <summary>List properties</summary>
        List,

        /// <summary>Display a list of instances</summary>
        Instances,

        /// <summary>Remove a deployment property</summary>
        Remove,

        /// <summary>Display an index of all deployments</summary>
        Index,
    }
}