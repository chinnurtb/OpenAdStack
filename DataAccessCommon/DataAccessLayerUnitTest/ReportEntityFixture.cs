// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReportEntityFixture.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Fixture to test ReportEntity class.</summary>
    [TestClass]
    public class ReportEntityFixture
    {
        /// <summary>Wrapped Entity object with for testing.</summary>
        private Entity wrappedEntity;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.wrappedEntity = TestEntityBuilder.BuildReportEntity(new EntityId()).SafeUnwrapEntity() as Entity;
        }

        /// <summary>Test we can construct and validate from existing entity object.</summary>
        [TestMethod]
        public void ConstructFromIEntity()
        {
            var entity = new ReportEntity(this.wrappedEntity);
            Assert.AreSame(this.wrappedEntity, entity.WrappedEntity);

            var blobEntityBase = this.wrappedEntity.BuildWrappedEntity();
            Assert.AreSame(this.wrappedEntity, blobEntityBase.SafeUnwrapEntity());
        }

        /// <summary>Test we do not double wrap.</summary>
        [TestMethod]
        public void ConstructFromIEntityDoesNotDoubleWrap()
        {
            var entity = new ReportEntity(this.wrappedEntity);
            var entityWrap = new ReportEntity(entity);
            Assert.AreSame(this.wrappedEntity, entityWrap.WrappedEntity);
        }

        /// <summary>Test that entity factory helper method.</summary>
        [TestMethod]
        public void BuildReportEntity()
        {
            var entityId = new EntityId();
            var reportName = "MyReportName";
            var reportType = "SomeReportType";
            var reportData = "SomeReportData";
            var reportEntity = ReportEntity.BuildReportEntity(entityId, reportName, reportType, reportData);
            Assert.AreEqual(entityId, (EntityId)reportEntity.ExternalEntityId);
            Assert.AreEqual(reportName, (string)reportEntity.ExternalName);
            Assert.AreEqual(reportType, reportEntity.ReportType);
            Assert.AreEqual(reportData, reportEntity.ReportData);
            Assert.IsTrue(reportEntity.GetEntityPropertyByName(ReportEntity.ReportDataName).IsExtendedProperty);
        }

        /// <summary>Verify we can correctly get and set ReportType property.</summary>
        [TestMethod]
        public void ReportTypeProperty()
        {
            var entity = TestEntityBuilder.BuildReportEntity(new EntityId());
            var newValue = "NewReportType";
            entity.ReportType = newValue;
            Assert.AreEqual(newValue, entity.ReportType);
            Assert.AreEqual(newValue, entity.GetPropertyByName<string>(ReportEntity.ReportTypeName));
        }

        /// <summary>Verify we can correctly get and set ReportData property.</summary>
        [TestMethod]
        public void ReportDataProperty()
        {
            var entity = TestEntityBuilder.BuildReportEntity(new EntityId());
            var newValue = "NewReportData";
            entity.ReportData = newValue;
            Assert.AreEqual(newValue, entity.ReportData);
            Assert.AreEqual(newValue, entity.GetPropertyByName<string>(ReportEntity.ReportDataName));
            Assert.IsTrue(entity.GetEntityPropertyByName(ReportEntity.ReportDataName).IsExtendedProperty);
        }

        /// <summary>Validate that entity construction fails if category is not Report.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void FailValidationIfReportTypePropertyNotSet()
        {
            this.wrappedEntity.RemovePropertyByName(ReportEntity.ReportTypeName);
            new ReportEntity(this.wrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Report.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void FailValidationIfReportDataPropertyNotSet()
        {
            this.wrappedEntity.RemovePropertyByName(ReportEntity.ReportDataName);
            new ReportEntity(this.wrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Report.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void FailValidationIfReportDataPropertyFilterMismatched()
        {
            this.wrappedEntity.SetPropertyByName(ReportEntity.ReportDataName, "Somedata", PropertyFilter.Default);
            new ReportEntity(this.wrappedEntity);
        }

        /// <summary>Validate that entity construction fails if category is not Report.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessTypeMismatchException))]
        public void FailValidationIfCategoryPropertyNotReport()
        {
            this.wrappedEntity.EntityCategory = "foobar";
            new ReportEntity(this.wrappedEntity);
        }
    }
}
