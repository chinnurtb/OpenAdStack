// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDataStoreBaseFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for XmlDataStoreBase tested through derived classes</summary>
    [TestClass]
    public class XmlDataStoreBaseFixture
    {
        /// <summary>minimum index store</summary>
        private XmlIndexDataStore minIndexStore;

        /// <summary>path to a backing file that does not exist</summary>
        private string nonExistentBackingFile;

        /// <summary>Per-test initialization</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            // Create the index store and set up it's factory stub to return it
            var indexXml = ResourceHelper.LoadXmlResource(@"MinimalIndexStore.xml");
            this.minIndexStore = new XmlIndexDataStore();
            this.minIndexStore.LoadFromXml(indexXml);

            // Make sure the non-existent backing file is non-existent
            this.nonExistentBackingFile = @".\imnothere.xml";
            File.Delete(this.nonExistentBackingFile);
        }

        /// <summary>By default, do not create backing file if specified one is not found.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DefaultDoNotCreateNewBackingFile()
        {
            var ds = new XmlIndexDataStore();
            ds.LoadFromFile(this.nonExistentBackingFile);
        }
               
        /// <summary>Create backing file if specified one is not found.</summary>
        [TestMethod]
        public void CreateNewBackingFile()
        {
            var ds = new XmlIndexDataStore();
            ds.LoadFromFile(this.nonExistentBackingFile, true);
            Assert.IsTrue(File.Exists(this.nonExistentBackingFile));
        }

        /// <summary>Create default Index backing file if specified.</summary>
        [TestMethod]
        public void CreateDefaultIndexBackingFile()
        {
            var ds = new XmlIndexDataStore();
            ds.LoadFromFile("DefaultIndex");
            ds.Commit();
            Assert.IsTrue(ds.DataSet.Tables.Count > 0);
        }

        /// <summary>Resolve full path on basis of bare filename.</summary>
        [TestMethod]
        public void CreateNewBackingFileResolveFullPathFromFilename()
        {
            var ds = new XmlIndexDataStore();
            var filename = Path.GetFileName(this.nonExistentBackingFile);
            ds.LoadFromFile(filename, true);
            Assert.IsTrue(File.Exists(this.nonExistentBackingFile));
        }

        /// <summary>Throw if filename is invalid.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidBackingFilename()
        {
            var ds = new XmlIndexDataStore();
            ds.LoadFromFile(@"thiswon't*work", true);
        }

        /// <summary>Throw if file directory is invalid.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InvalidBackingFileDirectory()
        {
            var ds = new XmlIndexDataStore();
            ds.LoadFromFile(@".\bad*dog\foo.xml", true);
        }

        /// <summary>Commit datastore to file.</summary>
        [TestMethod]
        public void Commit()
        {
            var store = new XmlIndexDataStore();
            store.LoadFromFile(this.nonExistentBackingFile, true);

            var newCompany = CreateNewInitializedCompany(store);
            var indexDataSet = (XmlIndexDataSet)store.DataSet;
            indexDataSet.Company.AddCompanyRow(newCompany);
            store.Commit();

            store = new XmlIndexDataStore();
            store.LoadFromFile(this.nonExistentBackingFile);
            indexDataSet = (XmlIndexDataSet)store.DataSet;
            Assert.IsNotNull(indexDataSet.Company.SingleOrDefault(c => c.xId == newCompany.xId));
        }

        /// <summary>Commit succeeds if datastore initialized from xml string rather than file. No-op for DataSet.</summary>
        [TestMethod]
        public void CommitSucceedsWithNoBackingFile()
        {
            var dataXml = ResourceHelper.LoadXmlResource(@"MinimalIndexStore.xml");
            var store = new XmlIndexDataStore();
            store.LoadFromXml(dataXml);
            
            var newCompany = CreateNewInitializedCompany(store);
            var indexDataSet = (XmlIndexDataSet)store.DataSet;
            indexDataSet.Company.AddCompanyRow(newCompany);
            store.Commit();

            Assert.IsNotNull(indexDataSet.Company.SingleOrDefault(c => c.xId == newCompany.xId));
        }

        /// <summary>Create a new company row with the provided store and initialize to default test values.</summary>
        /// <param name="store">The index store the row will be added to.</param>
        /// <returns>The new company row.</returns>
        private static XmlIndexDataSet.CompanyRow CreateNewInitializedCompany(XmlIndexDataStore store)
        {
            var indexDataSet = (XmlIndexDataSet)store.DataSet;
            var company = indexDataSet.Company.NewCompanyRow();
            company.xId = (EntityId)100;
            return company;
        }
    }
}
