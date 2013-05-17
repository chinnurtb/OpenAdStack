// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDataStoreFactoryFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
