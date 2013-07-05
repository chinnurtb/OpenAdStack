//-----------------------------------------------------------------------
// <copyright file="RoleInstanceState.cs" company="Rare Crowds Inc">
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
