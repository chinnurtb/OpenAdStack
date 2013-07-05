//-----------------------------------------------------------------------
// <copyright file="EntityCustomConfigFixture.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Script.Serialization;
using ConfigManager;
using DataAccessLayer;
using EntityActivities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EntityActivityUnitTests
{
    /// <summary>Test for custom configurations build from entities</summary>
    [TestClass]
    public class EntityCustomConfigFixture
    {
        /// <summary>JSON Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>Test entity configurations</summary>
        private IDictionary<string, IDictionary<string, string>> entityConfigs;

        /// <summary>Test default (global) config</summary>
        private IDictionary<string, string> defaultConfig;

        /// <summary>Initializes test configuration data</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            // Entity configurations
            var configA = new Dictionary<string, string>
            {
                { "Alpha", "a:" + Guid.NewGuid().ToString("N") },
                { "Beta", "B:" + Guid.NewGuid().ToString("N") },
                { "Gamma", "G:" + Guid.NewGuid().ToString("N") },
                { "Delta", "d:" + Guid.NewGuid().ToString("N") }
            };
            var configB = new Dictionary<string, string>
            {
                { "Delta", "d:" + Guid.NewGuid().ToString("N") },
                { "Epsilon", "E:" + Guid.NewGuid().ToString("N") },
                { "Zeta", "z:" + Guid.NewGuid().ToString("N") },
                { "Eta", "e:" + Guid.NewGuid().ToString("N") }
            };
            var configC = new Dictionary<string, string>
            {
                { "Eta", "e:" + Guid.NewGuid().ToString("N") },
                { "Theta", "0:" + Guid.NewGuid().ToString("N") },
                { "Iota", "i:" + Guid.NewGuid().ToString("N") },
                { "Kappa", "k:" + Guid.NewGuid().ToString("N") }
            };
            this.entityConfigs = new Dictionary<string, IDictionary<string, string>>
            {
                { "ConfigA", configA },
                { "ConfigB", configB },
                { "ConfigC", configC }
            };

            // Default configuration
            this.defaultConfig = new Dictionary<string, string>
            {
                { "Alpha", "a:" + Guid.NewGuid().ToString("N") },
                { "Delta", "d:" + Guid.NewGuid().ToString("N") },
                { "Eta", "e:" + Guid.NewGuid().ToString("N") },
                { "Psi", "ps:" + Guid.NewGuid().ToString("N") },
                { "Omega", "o:" + Guid.NewGuid().ToString("N") }
            };
            foreach (var setting in this.defaultConfig)
            {
                ConfigurationManager.AppSettings[setting.Key] = setting.Value;
            }
        }

        /// <summary>
        /// Test creating a custom configuration from a set of entities.
        /// Non-transparent, meaning that only the overrides from the
        /// last entity with a config are used.
        /// </summary>
        [TestMethod]
        public void BuildCustomConfigFromEntitiesOpaque()
        {
            var expectedSettings = new Dictionary<string, string>
            {
                { "Alpha", this.defaultConfig["Alpha"] },
                { "Delta", this.defaultConfig["Delta"] },
                { "Eta", this.entityConfigs["ConfigC"]["Eta"] },
                { "Theta", this.entityConfigs["ConfigC"]["Theta"] },
                { "Iota", this.entityConfigs["ConfigC"]["Iota"] },
                { "Kappa", this.entityConfigs["ConfigC"]["Kappa"] },
                { "Psi", this.defaultConfig["Psi"] },
                { "Omega", this.defaultConfig["Omega"] },
            };
            var unexpectedSettings = new[]
            {
                "Beta", "Gamma", "Epsilon", "Zeta"
            };

            var entities = this.entityConfigs
                .Select(kvp => new PartnerEntity(
                    new Entity
                    {
                        EntityCategory = PartnerEntity.CategoryName,
                        ExternalEntityId = new EntityId(),
                        ExternalName = kvp.Key,
                        Properties = { new EntityProperty("Config", JsonSerializer.Serialize(kvp.Value), PropertyFilter.System) }
                    }))
                .Cast<IEntity>()
                .ToArray();

            var config = EntityActivity.BuildCustomConfigFromEntities(false, entities);

            AssertExpectedSettings(config, expectedSettings);
            AssertUnexpectedSettings(config, unexpectedSettings);
        }

        /// <summary>
        /// Test creating a custom configuration from a set of entities.
        /// Transparent, meaning that a composite of the overrides from
        /// all entities with configs are used.
        /// </summary>
        [TestMethod]
        public void BuildCustomConfigFromEntitiesTransparent()
        {
            var expectedSettings = new Dictionary<string, string>
            {
                { "Alpha", this.entityConfigs["ConfigA"]["Alpha"] },
                { "Beta", this.entityConfigs["ConfigA"]["Beta"] },
                { "Gamma", this.entityConfigs["ConfigA"]["Gamma"] },
                { "Delta", this.entityConfigs["ConfigB"]["Delta"] },
                { "Epsilon", this.entityConfigs["ConfigB"]["Epsilon"] },
                { "Zeta", this.entityConfigs["ConfigB"]["Zeta"] },
                { "Eta", this.entityConfigs["ConfigC"]["Eta"] },
                { "Theta", this.entityConfigs["ConfigC"]["Theta"] },
                { "Iota", this.entityConfigs["ConfigC"]["Iota"] },
                { "Kappa", this.entityConfigs["ConfigC"]["Kappa"] },
                { "Psi", this.defaultConfig["Psi"] },
                { "Omega", this.defaultConfig["Omega"] },
            };
            var entities = this.entityConfigs
                .Select(kvp => new PartnerEntity(
                    new Entity
                    {
                        EntityCategory = PartnerEntity.CategoryName,
                        ExternalEntityId = new EntityId(),
                        ExternalName = kvp.Key,
                        Properties = { new EntityProperty("Config", JsonSerializer.Serialize(kvp.Value), PropertyFilter.System) }
                    }))
                .Cast<IEntity>()
                .ToArray();

            var config = EntityActivity.BuildCustomConfigFromEntities(true, entities);

            AssertExpectedSettings(config, expectedSettings);
        }

        /// <summary>
        /// Test creating a custom configuration non-transparently from
        /// a set of entities when none of them have custom configs.
        /// </summary>
        [TestMethod]
        public void BuildCustomConfigFromEntitiesOpaqueNoOverrides()
        {
            var expectedSettings = new Dictionary<string, string>
            {
                { "Alpha", this.defaultConfig["Alpha"] },
                { "Delta", this.defaultConfig["Delta"] },
                { "Eta", this.defaultConfig["Eta"] },
                { "Psi", this.defaultConfig["Psi"] },
                { "Omega", this.defaultConfig["Omega"] },
            };
            var unexpectedSettings = new[]
            {
                "Beta", "Gamma", "Epsilon", "Zeta", "Theta", "Iota", "Kappa"
            };
            var entities = this.entityConfigs
                .Select(kvp => new PartnerEntity(
                    new Entity
                    {
                        EntityCategory = PartnerEntity.CategoryName,
                        ExternalEntityId = new EntityId(),
                        ExternalName = kvp.Key,
                        Properties = { }
                    }))
                .Cast<IEntity>()
                .ToArray();

            var config = EntityActivity.BuildCustomConfigFromEntities(false, entities);

            AssertExpectedSettings(config, expectedSettings);
            AssertUnexpectedSettings(config, unexpectedSettings);
        }

        /// <summary>
        /// Test creating a custom configuration transparently from
        /// a set of entities when none of them have custom configs.
        /// </summary>
        [TestMethod]
        public void BuildCustomConfigFromEntitiesTransparentNoOverrides()
        {
            var expectedSettings = new Dictionary<string, string>
            {
                { "Alpha", this.defaultConfig["Alpha"] },
                { "Delta", this.defaultConfig["Delta"] },
                { "Eta", this.defaultConfig["Eta"] },
                { "Psi", this.defaultConfig["Psi"] },
                { "Omega", this.defaultConfig["Omega"] },
            };
            var unexpectedSettings = new[]
            {
                "Beta", "Gamma", "Epsilon", "Zeta", "Theta", "Iota", "Kappa"
            };
            var entities = this.entityConfigs
                .Select(kvp => new PartnerEntity(
                    new Entity
                    {
                        EntityCategory = PartnerEntity.CategoryName,
                        ExternalEntityId = new EntityId(),
                        ExternalName = kvp.Key,
                        Properties = { }
                    }))
                .Cast<IEntity>()
                .ToArray();

            var config = EntityActivity.BuildCustomConfigFromEntities(true, entities);

            AssertExpectedSettings(config, expectedSettings);
            AssertUnexpectedSettings(config, unexpectedSettings);
        }

        /// <summary>
        /// Test creating a custom configuration non-transparently from
        /// a set of entities when their configs are empty strings.
        /// </summary>
        [TestMethod]
        public void BuildCustomConfigFromEntitiesOpaqueEmptyOverrides()
        {
            var expectedSettings = new Dictionary<string, string>
            {
                { "Alpha", this.defaultConfig["Alpha"] },
                { "Delta", this.defaultConfig["Delta"] },
                { "Eta", this.defaultConfig["Eta"] },
                { "Psi", this.defaultConfig["Psi"] },
                { "Omega", this.defaultConfig["Omega"] },
            };
            var unexpectedSettings = new[]
            {
                "Beta", "Gamma", "Epsilon", "Zeta", "Theta", "Iota", "Kappa"
            };
            var entities = this.entityConfigs
                .Select(kvp => new PartnerEntity(
                    new Entity
                    {
                        EntityCategory = PartnerEntity.CategoryName,
                        ExternalEntityId = new EntityId(),
                        ExternalName = kvp.Key,
                        Properties = { new EntityProperty("Config", string.Empty, PropertyFilter.System) }
                    }))
                .Cast<IEntity>()
                .ToArray();

            var config = EntityActivity.BuildCustomConfigFromEntities(false, entities);

            AssertExpectedSettings(config, expectedSettings);
            AssertUnexpectedSettings(config, unexpectedSettings);
        }

        /// <summary>
        /// Test creating a custom configuration transparently from
        /// a set of entities when their configs are empty strings.
        /// </summary>
        [TestMethod]
        public void BuildCustomConfigFromEntitiesTransparentEmptyOverrides()
        {
            var expectedSettings = new Dictionary<string, string>
            {
                { "Alpha", this.defaultConfig["Alpha"] },
                { "Delta", this.defaultConfig["Delta"] },
                { "Eta", this.defaultConfig["Eta"] },
                { "Psi", this.defaultConfig["Psi"] },
                { "Omega", this.defaultConfig["Omega"] },
            };
            var unexpectedSettings = new[]
            {
                "Beta", "Gamma", "Epsilon", "Zeta", "Theta", "Iota", "Kappa"
            };
            var entities = this.entityConfigs
                .Select(kvp => new PartnerEntity(
                    new Entity
                    {
                        EntityCategory = PartnerEntity.CategoryName,
                        ExternalEntityId = new EntityId(),
                        ExternalName = kvp.Key,
                        Properties = { new EntityProperty("Config", string.Empty, PropertyFilter.System) }
                    }))
                .Cast<IEntity>()
                .ToArray();

            var config = EntityActivity.BuildCustomConfigFromEntities(true, entities);

            AssertExpectedSettings(config, expectedSettings);
            AssertUnexpectedSettings(config, unexpectedSettings);
        }

        /// <summary>Test that the expected settings are present</summary>
        /// <param name="config">Config to test</param>
        /// <param name="expectedSettings">The expected settings</param>
        private static void AssertExpectedSettings(CustomConfig config, IDictionary<string, string> expectedSettings)
        {
            foreach (var expected in expectedSettings)
            {
                Assert.AreEqual(expected.Value, config.GetValue(expected.Key));
            }
        }

        /// <summary>Test that the unexpected settings are not present</summary>
        /// <param name="config">Config to test</param>
        /// <param name="unexpectedSettings">The expected settings</param>
        private static void AssertUnexpectedSettings(CustomConfig config, IEnumerable<string> unexpectedSettings)
        {
            foreach (var unexpected in unexpectedSettings)
            {
                try
                {
                    config.GetValue(unexpected);
                    Assert.Fail("Expected ArgumentException not thrown for value '{0}'", unexpected);
                }
                catch (ArgumentException ae)
                {
                    Assert.IsTrue(ae.Message.Contains("configValueName"));
                }
            }
        }
    }
}
