// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDataStoreFactoryFixture.cs" company="Rare Crowds Inc">
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
// <summary>
//   Defines the XmlDataStoreFactoryFixture type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Configuration;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for Xml data store factories.</summary>
    [TestClass]
    public class XmlDataStoreFactoryFixture
    {
        /// <summary>XmlIndexDataStoreFactory for testing.</summary>
        private IIndexStoreFactory indexStoreFactory;

        /// <summary>Index store connection string for testing.</summary>
        private string indexConnectionString;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.indexConnectionString = ConfigurationManager.AppSettings["Index.ConnectionString"];
            this.indexStoreFactory = new XmlIndexStoreFactory(this.indexConnectionString);
        }

        /// <summary>Default get (uses index store file from app.config.</summary>
        [TestMethod]
        [DeploymentItem(@"resources\indexstorebackingfile.xml")]
        public void GetIndexStore()
        {
            var store = this.indexStoreFactory.GetIndexStore();
            Assert.IsNotNull(store);
            Assert.IsInstanceOfType(store, typeof(XmlIndexDataStore));
        }

        /// <summary>Verify singleton behavior.</summary>
        [TestMethod]
        [DeploymentItem(@"resources\indexstorebackingfile.xml")]
        public void IndexStoreIsSingleton()
        {
            var store = this.indexStoreFactory.GetIndexStore();

            var newFactory = new XmlIndexStoreFactory(this.indexConnectionString);
            var newStore = newFactory.GetIndexStore();

            Assert.AreSame(store, newStore);
        }
    }
}
