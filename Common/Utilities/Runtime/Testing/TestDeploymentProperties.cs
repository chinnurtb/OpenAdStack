//-----------------------------------------------------------------------
// <copyright file="TestDeploymentProperties.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
