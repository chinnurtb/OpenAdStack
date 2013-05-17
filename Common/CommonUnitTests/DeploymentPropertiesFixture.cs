//-----------------------------------------------------------------------
// <copyright file="DeploymentPropertiesFixture.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Utilities.Runtime;
using Utilities.Runtime.Testing;
using Utilities.Storage;
using Utilities.Storage.Testing;

namespace CommonUnitTests
{
    /// <summary>Unit tests for DeploymentProperties</summary>
    [TestClass]
    public class DeploymentPropertiesFixture
    {
        /// <summary>Dictionary for direct access to properties</summary>
        private IPersistentDictionary<IDictionary<string, string>> properties;

        /// <summary>Random key for the tests to use</summary>
        private string key;

        /// <summary>Random value for the tests to use</summary>
        private string expectedValue;

        /// <summary>Test deployment id</summary>
        private string deploymentId;

        /// <summary>Test role instance id</summary>
        private string roleInstanceId;

        /// <summary>Test role instance key</summary>
        private string roleInstanceKey;

        /// <summary>
        /// Per test case initialization
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            SimulatedPersistentDictionaryFactory.Initialize();
            TestDeploymentProperties.Initialize();
            this.properties = PersistentDictionaryFactory.CreateDictionary<IDictionary<string, string>>(DeploymentProperties.DeploymentPropertyStoreName);
            this.key = Guid.NewGuid().ToString("N");
            this.expectedValue = Guid.NewGuid().ToString();

            this.deploymentId = TestDeploymentProperties.TestDeploymentId;
            this.roleInstanceId = TestDeploymentProperties.TestRoleInstanceId;
            this.roleInstanceKey = DeploymentProperties.RoleInstanceKeyFormat
                .FormatInvariant(this.deploymentId, this.roleInstanceId);
        }

        /// <summary>
        /// Test that DeploymentProperties.Instance always returns the same object
        /// </summary>
        public void IsSingleton()
        {
            var instance = DeploymentProperties.Instance;
            Assert.AreSame(instance, DeploymentProperties.Instance);
        }

        /// <summary>
        /// Test getting a deployment property
        /// </summary>
        [TestMethod]
        public void GetDeploymentProperty()
        {
            this.properties[TestDeploymentProperties.TestDeploymentId] = new Dictionary<string, string>
            {
                { this.key, this.expectedValue }
            };

            var value = DeploymentProperties.GetDeploymentProperty(this.key);
            Assert.AreEqual(this.expectedValue, value);
        }

        /// <summary>
        /// Test setting a deployment property
        /// </summary>
        [TestMethod]
        public void SetDeploymentProperty()
        {
            DeploymentProperties.SetDeploymentProperty(this.key, this.expectedValue);
            Assert.IsTrue(this.properties.ContainsKey(this.deploymentId));
            Assert.IsTrue(this.properties[this.deploymentId].ContainsKey(this.key));
            var value = this.properties[this.deploymentId][this.key];
            Assert.AreEqual(this.expectedValue, value);
        }

        /// <summary>
        /// Test re-setting a deployment property
        /// </summary>
        [TestMethod]
        public void ResetDeploymentProperty()
        {
            DeploymentProperties.SetDeploymentProperty(this.key, this.expectedValue);
            Assert.IsTrue(this.properties.ContainsKey(this.deploymentId));
            Assert.IsTrue(this.properties[this.deploymentId].ContainsKey(this.key));
            var value = this.properties[this.deploymentId][this.key];
            Assert.AreEqual(this.expectedValue, value);

            this.expectedValue = Guid.NewGuid().ToString();

            DeploymentProperties.SetDeploymentProperty(this.key, this.expectedValue);
            Assert.IsTrue(this.properties.ContainsKey(this.deploymentId));
            Assert.IsTrue(this.properties[this.deploymentId].ContainsKey(this.key));
            var newValue = this.properties[this.deploymentId][this.key];
            Assert.AreEqual(this.expectedValue, newValue);
        }

        /// <summary>
        /// Test roundtripping a deployment property
        /// </summary>
        [TestMethod]
        public void RoundtripDeploymentProperty()
        {
            DeploymentProperties.SetDeploymentProperty(this.key, this.expectedValue);
            var value = DeploymentProperties.GetDeploymentProperty(this.key);
            Assert.AreEqual(this.expectedValue, value);
        }

        /// <summary>
        /// Test getting a deployment property where the deployment does not exist
        /// </summary>
        [TestMethod]
        public void GetDeploymentPropertyNonexistentDeployment()
        {
            var value = DeploymentProperties.GetDeploymentProperty(this.key);
            Assert.IsNull(value);
        }

        /// <summary>
        /// Test getting a deployment property where the deployment exists, but the property does not
        /// </summary>
        [TestMethod]
        public void GetDeploymentPropertyNonexistentProperty()
        {
            this.properties.Add(this.deploymentId, new Dictionary<string, string>());
            var value = DeploymentProperties.GetDeploymentProperty(this.key);
            Assert.IsNull(value);
        }

        /// <summary>
        /// Test getting a role instance property
        /// </summary>
        [TestMethod]
        public void GetRoleInstanceProperty()
        {
            this.properties[this.roleInstanceKey] = new Dictionary<string, string>
            {
                { this.key, this.expectedValue }
            };

            var value = DeploymentProperties.GetRoleInstanceProperty(this.key);
            Assert.AreEqual(this.expectedValue, value);
        }

        /// <summary>
        /// Test setting a role instance property
        /// </summary>
        [TestMethod]
        public void SetRoleInstanceProperty()
        {
            DeploymentProperties.SetRoleInstanceProperty(this.key, this.expectedValue);
            Assert.IsTrue(this.properties.ContainsKey(this.roleInstanceKey));
            Assert.IsTrue(this.properties[this.roleInstanceKey].ContainsKey(this.key));
            var value = this.properties[this.roleInstanceKey][this.key];
            Assert.AreEqual(this.expectedValue, value);
        }

        /// <summary>
        /// Test re-setting a deployent property
        /// </summary>
        [TestMethod]
        public void ResetRoleInstanceProperty()
        {
            DeploymentProperties.SetRoleInstanceProperty(this.key, this.expectedValue);
            Assert.IsTrue(this.properties.ContainsKey(this.roleInstanceKey));
            Assert.IsTrue(this.properties[this.roleInstanceKey].ContainsKey(this.key));
            var value = this.properties[this.roleInstanceKey][this.key];
            Assert.AreEqual(this.expectedValue, value);

            this.expectedValue = Guid.NewGuid().ToString();

            DeploymentProperties.SetRoleInstanceProperty(this.key, this.expectedValue);
            Assert.IsTrue(this.properties.ContainsKey(this.roleInstanceKey));
            Assert.IsTrue(this.properties[this.roleInstanceKey].ContainsKey(this.key));
            var newValue = this.properties[this.roleInstanceKey][this.key];
            Assert.AreEqual(this.expectedValue, newValue);
        }

        /// <summary>
        /// Test getting a role instance property where the role instance does not exist
        /// </summary>
        [TestMethod]
        public void GetRoleInstancePropertyNonexistentRoleInstance()
        {
            var value = DeploymentProperties.GetRoleInstanceProperty(this.key);
            Assert.IsNull(value);
        }

        /// <summary>
        /// Test getting a role instance property where the role instance exists, but the property does not
        /// </summary>
        [TestMethod]
        public void GetRoleInstancePropertyNonexistentProperty()
        {
            this.properties.Add(this.roleInstanceKey, new Dictionary<string, string>());
            var value = DeploymentProperties.GetRoleInstanceProperty(this.key);
            Assert.IsNull(value);
        }

        /// <summary>
        /// Test getting multiple role instance properties
        /// </summary>
        [TestMethod]
        public void GetRoleInstanceProperties()
        {
            // Populate with properties having the same value as their role ids
            var roleIds = new[] { Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") };
            foreach (var roleId in roleIds)
            {
                var roleInstanceKey = DeploymentProperties.RoleInstanceKeyFormat
                        .FormatInvariant(this.deploymentId, roleId);
                this.properties.Add(
                    roleInstanceKey,
                    new Dictionary<string, string>
                    {
                        { this.key, roleId }
                    });
            }

            var values = DeploymentProperties.GetRoleInstanceProperties(this.key);
            Assert.AreEqual(roleIds.Length, values.Length);
            foreach (var pair in roleIds.Zip(values))
            {
                Assert.AreEqual(pair.Item1, pair.Item2);
            }
        }

        /// <summary>
        /// Test getting multiple role instance properties when no deployments/roles exist
        /// </summary>
        [TestMethod]
        public void GetRoleInstancePropertiesNoDeployments()
        {
            var values = DeploymentProperties.GetRoleInstanceProperties(this.key);
            Assert.IsNotNull(values);
            Assert.AreEqual(0, values.Length);
        }

        /// <summary>
        /// Test getting multiple role instance properties only returns values for
        /// the current deployment and the specified key
        /// </summary>
        [TestMethod]
        public void GetRoleInstancePropertiesOnlyThisDeploymentAndKey()
        {
            var roleIds = new[] { Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N") };
            var otherDeploymentId = Guid.NewGuid().ToString();
            var badValue = Guid.NewGuid().ToString();
            var badKey = Guid.NewGuid().ToString();
            foreach (var roleId in roleIds)
            {
                // Populate deployment role instances with properties having the same value as their role ids
                var roleInstanceKey =
                    DeploymentProperties.RoleInstanceKeyFormat
                        .FormatInvariant(this.deploymentId, roleId);
                this.properties.Add(
                    roleInstanceKey,
                    new Dictionary<string, string>
                    {
                        { this.key, roleId },
                        { badKey, badValue }
                    });

                // Populate properties for another deployment's role instances having the "bad" value
                var otherDeploymentRoleInstanceKey =
                    DeploymentProperties.RoleInstanceKeyFormat
                        .FormatInvariant(otherDeploymentId, roleId);
                this.properties.Add(
                    otherDeploymentRoleInstanceKey,
                    new Dictionary<string, string>
                    {
                        { this.key, badValue },
                        { badKey, badValue }
                    });
            }

            var values = DeploymentProperties.GetRoleInstanceProperties(this.key);
            foreach (var value in values)
            {
                Assert.AreNotEqual(badValue, value);
            }

            Assert.AreEqual(roleIds.Length, values.Length);
            foreach (var pair in roleIds.Zip(values))
            {
                Assert.AreEqual(pair.Item1, pair.Item2);
            }
        }
    }
}
