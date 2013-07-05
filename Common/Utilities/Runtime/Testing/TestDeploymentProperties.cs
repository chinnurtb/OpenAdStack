//-----------------------------------------------------------------------
// <copyright file="TestDeploymentProperties.cs" company="Rare Crowds Inc">
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

using System;

namespace Utilities.Runtime.Testing
{
    /// <summary>Testing version of deployment properties</summary>
    public class TestDeploymentProperties : DeploymentProperties
    {
        /// <summary>Gets the test deployment id</summary>
        public static string TestDeploymentId { get; private set; }

        /// <summary>Gets the test role instance id</summary>
        public static string TestRoleInstanceId { get; private set; }

        /// <summary>Initializes DeploymentProperties for testing</summary>
        public static void Initialize()
        {
            TestDeploymentId = Guid.NewGuid().ToString("N");
            TestRoleInstanceId = Guid.NewGuid().ToString("N");
            DeploymentProperties.Instance = new TestDeploymentProperties();
        }

        /// <summary>Gets the deployment id</summary>
        /// <returns>The deployment id</returns>
        protected override string GetDeploymentId()
        {
            return TestDeploymentId;
        }

        /// <summary>Gets the role instance id</summary>
        /// <returns>The role instance id</returns>
        protected override string GetRoleInstanceId()
        {
            return TestRoleInstanceId;
        }
    }
}
