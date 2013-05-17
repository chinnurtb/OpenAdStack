//Copyright 2012-2013 Rare Crowds, Inc.
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

using System;
using System.Collections.Generic;
using ConfigManager;
using DeliveryNetworkUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace ActivityUtilitiesUnitTests
{
    /// <summary>Tests for the DynamicAllocation Activity Utilities</summary>
    [TestClass]
    public class DeliveryNetworkUtilitiesFixture
    {
        /// <summary>Test configuration</summary>
        private IConfig testConfig;

        /// <summary>Test config setting value</summary>
        private string testSettingValue;

        /// <summary>Test delivery network client interface</summary>
        private interface ITestNetworkClient : IDeliveryNetworkClient
        {
            /// <summary>Gets the test setting</summary>
            string Setting { get; }
        }

        /// <summary>Per-test case initialization</summary>
        [TestInitialize]
        public void TestInitialize()
        {
            DeliveryNetworkClientFactory.Initialize(new IDeliveryNetworkClientFactory[0]);
            this.testSettingValue = Guid.NewGuid().ToString();
            this.testConfig = new CustomConfig(
                new Dictionary<string, string>
                {
                    { "Setting", this.testSettingValue }
                });
        }

        /// <summary>
        /// Test using the DeliveryNetworkClientFactory with a mock
        /// </summary>
        [TestMethod]
        public void DeliveryNetworkClientFactoryWithMockFactory()
        {
            // Setup the mock
            var mockFactory = MockRepository.GenerateMock<IDeliveryNetworkClientFactory>();
            mockFactory.Stub(f => f.ClientType)
                .Return(typeof(ITestNetworkClient));
            mockFactory.Stub(f => f.CreateClient(Arg<IConfig>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    call.ReturnValue = new TestNetworkClient
                    {
                        Config = call.Arguments[0] as IConfig
                    };
                });

            // Initialize the static factory with the mock
            DeliveryNetworkClientFactory.Initialize(new[] { mockFactory });

            // Create an instance of the test client and verify
            var client = DeliveryNetworkClientFactory.CreateClient<ITestNetworkClient>(this.testConfig);
            Assert.IsNotNull(client);
            Assert.IsInstanceOfType(client, typeof(ITestNetworkClient));
            Assert.IsInstanceOfType(client, typeof(TestNetworkClient));
            Assert.AreSame(this.testConfig, client.Config);
            Assert.AreEqual(this.testSettingValue, client.Setting);
        }

        /// <summary>
        /// Test using the DeliveryNetworkClientFactory with a generic factory
        /// </summary>
        [TestMethod]
        public void DeliveryNetworkClientFactoryWithGenericFactory()
        {
            // Create a generic factory
            var genericFactory = new GenericDeliveryNetworkClientFactory<ITestNetworkClient, TestNetworkClient>();
            Assert.AreEqual(typeof(ITestNetworkClient), genericFactory.ClientType);

            // Initialize the static factory with a generic factory
            DeliveryNetworkClientFactory.Initialize(new[] { genericFactory });

            // Create an instance of the test client and verify
            var client = DeliveryNetworkClientFactory.CreateClient<ITestNetworkClient>(this.testConfig);
            Assert.IsNotNull(client);
            Assert.IsInstanceOfType(client, typeof(ITestNetworkClient));
            Assert.IsInstanceOfType(client, typeof(TestNetworkClient));
            Assert.AreSame(this.testConfig, client.Config);
            Assert.AreEqual(this.testSettingValue, client.Setting);
        }

        /// <summary>Test delivery network client implementation</summary>
        private sealed class TestNetworkClient : ITestNetworkClient
        {
            /// <summary>Gets or sets the client's configuration</summary>
            public IConfig Config { get; set; }

            /// <summary>Gets the test setting</summary>
            public string Setting
            {
                get { return this.Config.GetValue("Setting"); }
            }

            /// <summary>Frees unmanaged resources</summary>
            public void Dispose()
            {
            }
        }
    }
}