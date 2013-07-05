//-----------------------------------------------------------------------
// <copyright file="DeploymentState.cs" company="Rare Crowds Inc">
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
