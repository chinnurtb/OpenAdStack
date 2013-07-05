//-----------------------------------------------------------------------
// <copyright file="ODataSerializerFixture.cs" company="Rare Crowds Inc.">
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

using System.IO;
using System.Linq;
using System.Xml.Linq;
using ConcreteDataStore;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcreteDataStoreUnitTests
{
    /// <summary>Test fixture for ODataSerializer</summary>
    [TestClass]
    public class ODataSerializerFixture
    {
        /// <summary>An ODataElement representing an association for testing.</summary>
        private ODataElement odataAssociation;

        /// <summary>An association for testing.</summary>
        private Association association;

        /// <summary>An association group key for testing.</summary>
        private Association associationGroupKey;

        /// <summary>An ODataElement representing an association group for testing.</summary>
        private ODataElement odataAssociationGroup;

        /// <summary>Target entity id array for testing</summary>
        private EntityId[] targetEntityIds;

        /// <summary>Per test initialization.</summary>
        [TestInitialize]
        public void InitializeTest()
        {
            this.odataAssociation = new ODataElement
            {
                HasValue = true,
                ODataName = "v_ptr_Relationship_Company_Agency_ParentCompany",
                ODataType = "Edm.String",
                ODataValue = "00000000000000000000000000000001||Agency||ParentCompany"
            };

            this.association = new Association
            {
                TargetEntityId = new EntityId(1),
                TargetEntityCategory = CompanyEntity.CategoryName,
                TargetExternalType = "Agency",
                ExternalName = "ParentCompany",
                AssociationType = AssociationType.Relationship
            };

            this.associationGroupKey = new Association
            {
                TargetEntityCategory = CompanyEntity.CategoryName,
                TargetExternalType = "Advertiser",
                ExternalName = "Advertisers",
                AssociationType = AssociationType.Child
            };

            this.odataAssociationGroup = new ODataElement
            {
                HasValue = true,
                ODataName = "c_ptr_Child_Company_Advertiser_Advertisers",
                ODataType = "Edm.String",
                ODataValue = @"[""00000000000000000000000000000001"",""00000000000000000000000000000002""]"
            };

            this.targetEntityIds = new[] { new EntityId(1), new EntityId(2) };
        }

        /// <summary>Detect name of simple property. Complex properties start with c_ or v_</summary>
        [TestMethod]
        public void IsSimpleProperty()
        {
            Assert.IsTrue(ODataSerializer.IsEntityProperty("SomeName"));
            Assert.IsTrue(ODataSerializer.IsEntityProperty("_SomeName"));
            Assert.IsTrue(ODataSerializer.IsEntityProperty(" x_SomeName "));
            Assert.IsFalse(ODataSerializer.IsEntityProperty("c_SomeName"));
            Assert.IsFalse(ODataSerializer.IsEntityProperty("c_Some_Name"));
        }

        /// <summary>Detect name of system property. System properties contain sys_</summary>
        [TestMethod]
        public void IsSystemProperty()
        {
            Assert.IsTrue(new ODataPropertyName("sys_SomeName").Filter == PropertyFilter.System);
            Assert.IsTrue(new ODataPropertyName("_sys_SomeName").Filter == PropertyFilter.System);
            Assert.IsTrue(new ODataPropertyName("c_sys_SomeName").Filter == PropertyFilter.System);
            Assert.IsTrue(new ODataPropertyName("c_sys_Some_Name").Filter == PropertyFilter.System);
            Assert.IsTrue(new ODataPropertyName("v_sys_SomeName").Filter == PropertyFilter.System);
            Assert.IsFalse(new ODataPropertyName("sysSomeNonSysNamesys").Filter == PropertyFilter.System);
            Assert.IsFalse(new ODataPropertyName("x_SomeName").Filter == PropertyFilter.System);
            Assert.IsFalse(new ODataPropertyName("c_SomeName").Filter == PropertyFilter.System);
            Assert.IsFalse(new ODataPropertyName("c_Some_Name").Filter == PropertyFilter.System);
            Assert.IsFalse(new ODataPropertyName("v_SomeName").Filter == PropertyFilter.System);
        }

        /// <summary>Detect name of extended property. Extended properties contain ext_</summary>
        [TestMethod]
        public void IsExtendedProperty()
        {
            Assert.IsTrue(new ODataPropertyName("ext_SomeName").Filter == PropertyFilter.Extended);
            Assert.IsTrue(new ODataPropertyName("_ext_SomeName").Filter == PropertyFilter.Extended);
            Assert.IsTrue(new ODataPropertyName("c_ext_SomeName").Filter == PropertyFilter.Extended);
            Assert.IsTrue(new ODataPropertyName("c_ext_Some_Name").Filter == PropertyFilter.Extended);
            Assert.IsTrue(new ODataPropertyName("v_ext_SomeName").Filter == PropertyFilter.Extended);
            Assert.IsFalse(new ODataPropertyName("extSomeNonExtNameext").Filter == PropertyFilter.Extended);
            Assert.IsFalse(new ODataPropertyName("x_SomeName").Filter == PropertyFilter.Extended);
            Assert.IsFalse(new ODataPropertyName("c_SomeName").Filter == PropertyFilter.Extended);
            Assert.IsFalse(new ODataPropertyName("c_Some_Name").Filter == PropertyFilter.Extended);
            Assert.IsFalse(new ODataPropertyName("v_SomeName").Filter == PropertyFilter.Extended);
        }

        /// <summary>Test we can serialize an entity property to odata with xml encoding.</summary>
        [TestMethod]
        public void SerializeEntityPropertyWithXmlEncoding()
        {
            // Get an OData shell
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);
            var name = "\u0b83\u0b83\u16a0somename";
            var expectedEncodedName = "{0}0B83\u0b83{0}16A0somename".FormatInvariant(AzureNameEncoder.XmlEscapeCharacter);
            var value = "somevalue";
            ODataSerializer.SerializeEntityProperty(odataContent, new EntityProperty(name, value));

            var odataElements = odataXml.Element(ODataSerializer.ODataContentName).Element(ODataSerializer.ODataPropertiesName).Elements();
            Assert.IsTrue(odataElements.Single(e => e.Name.LocalName == expectedEncodedName).Value == value);
        }

        /// <summary>Test we can deserialize odata to a single assocation.</summary>
        [TestMethod]
        public void DeserializeAssociationSuccess()
        {
            var newAssocation = ODataSerializer.DeserializeAssociation(this.odataAssociation);
            Assert.AreEqual(this.association, newAssocation);
        }

        /// <summary>Determine we get empty Assoication if odata doesn't have a value.</summary>
        [TestMethod]
        public void DeserializeAssociationODataDoesNotHaveValue()
        {
            this.odataAssociation.HasValue = false;
            var newAssocation = ODataSerializer.DeserializeAssociation(this.odataAssociation);
            Assert.AreEqual(new Association(), newAssocation);
        }

        /// <summary>We get valid Assoication if odata doesn't have a value.</summary>
        [TestMethod]
        public void DeserializeAssociationODataValueHasIdOnly()
        {
            this.odataAssociation.ODataValue = "00000000000000000000000000000001||||";
            var newAssocation = ODataSerializer.DeserializeAssociation(this.odataAssociation);
            Assert.AreEqual(this.association.TargetEntityId, newAssocation.TargetEntityId);
            Assert.AreEqual(string.Empty, newAssocation.TargetExternalType);
            Assert.AreEqual(string.Empty, newAssocation.ExternalName);
        }

        /// <summary>Deserialize an Association group key from an odata name</summary>
        [TestMethod]
        public void DeserializeAssociationGroupKey()
        {
            var odataName = new ODataAssociationName(this.odataAssociationGroup.ODataName);
            var actualAssociationGroupKey = odataName.AssociationGroupKey;
            Assert.AreEqual(this.associationGroupKey.AssociationType, actualAssociationGroupKey.AssociationType);
            Assert.AreEqual(this.associationGroupKey.TargetEntityCategory, actualAssociationGroupKey.TargetEntityCategory);
            Assert.AreEqual(this.associationGroupKey.TargetExternalType, actualAssociationGroupKey.TargetExternalType);
            Assert.AreEqual(this.associationGroupKey.ExternalName, actualAssociationGroupKey.ExternalName);
        }

        /// <summary>Deserialize an Association group key from an odata name with encoding</summary>
        [TestMethod]
        public void DeserializeAssociationGroupKeyPartiallySpecified()
        {
            this.odataAssociationGroup.ODataName = "c_ptr_Child_Company__Advertisers";
            var odataName = new ODataAssociationName(this.odataAssociationGroup.ODataName);
            
            Assert.AreEqual(string.Empty, odataName.AssociationGroupKey.TargetExternalType);
            Assert.AreEqual("Advertisers", odataName.AssociationGroupKey.ExternalName);
        }

        /// <summary>Deserialize an Association group key returns null on failure.</summary>
        [TestMethod]
        public void DeserializeAssociationGroupKeyReturnsNullOnFail()
        {
            this.odataAssociationGroup.ODataName = "c_ptr_Notvalid_Company__Advertisers";
            var odataName = new ODataAssociationName(this.odataAssociationGroup.ODataName);

            Assert.IsNull(odataName.AssociationGroupKey);
        }

        /// <summary>Serialize an Association group key to an odata name</summary>
        [TestMethod]
        public void SerializeAssociationGroupKey()
        {
            var odataName = ODataAssociationName.SerializeAssociationGroup(this.associationGroupKey);
            Assert.AreEqual(this.odataAssociationGroup.ODataName, odataName);
        }

        /// <summary>Serialize a partially specified Association group key to an odata name</summary>
        [TestMethod]
        public void SerializeAssociationGroupKeyPartiallySpecified()
        {
            this.associationGroupKey.TargetExternalType = null;
            var odataName = ODataAssociationName.SerializeAssociationGroup(this.associationGroupKey);
            Assert.AreEqual("c_ptr_Child_Company__Advertisers", odataName);
        }

        /// <summary>Serialize Association group ids to an odata value</summary>
        [TestMethod]
        public void SerializeAssociationGroupIds()
        {
            var serializedValue =
                ODataAssociationValue.SerializeAssociationGroup(new[] { new EntityId(1), new EntityId(2) });
            Assert.AreEqual(this.odataAssociationGroup.ODataValue, serializedValue);
        }

        /// <summary>Deserialize Association group ids from an odata value</summary>
        [TestMethod]
        public void DeserializeAssociationGroupIds()
        {
            var odataValue = new ODataAssociationValue(this.odataAssociationGroup.ODataValue);
            var ids = odataValue.TargetEntityIds;
            Assert.AreEqual(2, ids.Length);
            Assert.IsTrue(ids.Any(id => id == this.targetEntityIds[0]));
            Assert.IsTrue(ids.Any(id => id == this.targetEntityIds[1]));
        }

        /// <summary>Trying to deserialize the id json on legacy association odata fails.</summary>
        [TestMethod]
        public void DeserializeAssociationJsonFromLegacyAssociationFails()
        {
            var odataValue = new ODataAssociationValue(this.odataAssociation.ODataValue);
            Assert.IsNull(odataValue.TargetEntityIds);
        }

        /// <summary>Trying to deserialize legacy field from id json fails.</summary>
        [TestMethod]
        public void DeserializeAssociationLegacyIdFromJsonFails()
        {
            var odataValue = new ODataAssociationValue(this.odataAssociationGroup.ODataValue);
            Assert.IsNull(odataValue.TargetEntityId);
            Assert.IsNull(odataValue.ExternalName);
            Assert.IsNull(odataValue.TargetExternalType);
        }

        /// <summary>Roundtrip serialize at the ODataSerializer level.</summary>
        [TestMethod]
        public void RoundtripSerializeAssociationGroup()
        {
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);
            ODataSerializer.SerializeAssociationGroup(odataContent, this.associationGroupKey, this.targetEntityIds);
            var odataElements = ODataSerializer.GetODataElements(odataXml);
            var associationElement = odataElements.Single(e => e.ODataName == this.odataAssociationGroup.ODataName);
            var associationGroup = ODataSerializer.DeserializeAssociationGroup(associationElement).ToList();
            Assert.AreEqual(2, associationGroup.Count());
            Assert.IsTrue(associationGroup.Any(a => a.TargetEntityId == this.targetEntityIds[0]));
            Assert.IsTrue(associationGroup.Any(a => a.TargetEntityId == this.targetEntityIds[1]));
        }

        /// <summary>Roundtrip serialize at the ODataSerializer level.</summary>
        [TestMethod]
        public void SerializeAssociationGroupWithXmlEncoding()
        {
            var odataXml = XElement.Load(new StringReader(ResourceHelper.LoadXmlResource(@"AzurePropertySaveOData.xml")));
            var odataContent = ODataSerializer.GetODataContent(odataXml);

            this.associationGroupKey.ExternalName = "\u0b83\u16a0somename";
            var expectedEncodedName = "c_ptr_Child_Company_Advertiser_\u0b83{0}16A0somename".FormatInvariant(AzureNameEncoder.XmlEscapeCharacter);

            ODataSerializer.SerializeAssociationGroup(odataContent, this.associationGroupKey, this.targetEntityIds);

            var odataElements = odataXml.Element(ODataSerializer.ODataContentName).Element(ODataSerializer.ODataPropertiesName).Elements();
            Assert.IsNotNull(odataElements.Single(e => e.Name.LocalName == expectedEncodedName));
        }

        /// <summary>No association marker throws DAL exception</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeMalformedAssociationNoAssociationMarkers()
        {
            var odataElement = this.BuildODataElement(
                "c_Child_Company_Advertiser_Advertisers",
                @"[""00000000000000000000000000000001"",""00000000000000000000000000000002""]",
                "Edm.String",
                false, 
                false);
            ODataSerializer.DeserializeAssociationGroup(odataElement);
        }

        /// <summary>No collection marker throws DAL exception</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeMalformedAssociationNoCollectionMarker()
        {
            var odataElement = this.BuildODataElement(
                "v_ptr_Child_Company_Advertiser_Advertisers",
                @"[""00000000000000000000000000000001"",""00000000000000000000000000000002""]",
                "Edm.String",
                false,
                false);
            ODataSerializer.DeserializeAssociationGroup(odataElement);
        }

        /// <summary>Bad target entity id throws DAL exception</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessException))]
        public void DeserializeMalformedAssociationBadId()
        {
            var odataElement = this.BuildODataElement(
                "c_ptr_NotValid_Company_Advertiser_Advertisers",
                @"InvalidId",
                "Edm.String",
                false,
                false);
            ODataSerializer.DeserializeAssociationGroup(odataElement);
        }

        /// <summary>Association element doesn't have a value.</summary>
        [TestMethod]
        public void DeserializeAssociationNoValue()
        {
            var odataElement = this.BuildODataElement(
                "c_ptr_Child_Company_Advertiser_Advertisers",
                @"[""00000000000000000000000000000001"",""00000000000000000000000000000002""]",
                "Edm.String",
                true,
                true);
            var associationGroup = ODataSerializer.DeserializeAssociationGroup(odataElement);
            Assert.AreEqual(0, associationGroup.Count());
        }
        
        /// <summary>Parse OData property with default type and no null attribute.</summary>
        [TestMethod]
        public void ParseODataPropertyDefaultTypeNoNull()
        {
            var odataElement = this.BuildODataElement("name", "value", null, false, false);
            this.AssertParseODataProperty(odataElement, "name", "value", "Edm.String", true);
        }
        
        /// <summary>Parse OData property with type attribute and no null attribute.</summary>
        [TestMethod]
        public void ParseODataPropertyWithType()
        {
            var odataElement = this.BuildODataElement("name", "value", "Edm.Int32", false, false);
            this.AssertParseODataProperty(odataElement, "name", "value", "Edm.Int32", true);
        }

        /// <summary>Parse OData property with default type and a null attribute false.</summary>
        [TestMethod]
        public void ParseODataPropertyDefaultTypeNullFalse()
        {
            var odataElement = this.BuildODataElement("name", "value", null, true, false);
            this.AssertParseODataProperty(odataElement, "name", "value", "Edm.String", true);
        }

        /// <summary>Parse OData property with default type and a null attribute true.</summary>
        [TestMethod]
        public void ParseODataPropertyDefaultTypeNullTrue()
        {
            var odataElement = this.BuildODataElement("name", "value", null, true, true);
            this.AssertParseODataProperty(odataElement, "name", "value", "Edm.String", false);
        }

        /// <summary>Parse OData property with an xml encoded name.</summary>
        [TestMethod]
        public void ParseODataPropertyXmlEncodedName()
        {
            var name = "{0}0B83\u0b83{0}16A0".FormatInvariant(AzureNameEncoder.XmlEscapeCharacter);
            var expectedParsedName = "\u0b83\u0b83\u16a0";
            var odataElement = this.BuildODataElement(name, "value", null, true, true);
            this.AssertParseODataProperty(odataElement, expectedParsedName, "value", "Edm.String", false);
        }

        /// <summary>Build an OData property with specified values.</summary>
        /// <param name="name">The element name.</param>
        /// <param name="value">The element value.</param>
        /// <param name="typeValue">The value of the type attribute if present (empty or null for not present)</param>
        /// <param name="nullPresent">True if the null attribute is present.</param>
        /// <param name="nullValue">The value of the null attibute if present.</param>
        /// <returns>OData property element</returns>
        private ODataElement BuildODataElement(string name, string value, string typeValue, bool nullPresent, bool nullValue)
        {
            var odataProperty = new XElement(ODataSerializer.ODataNamespace + name, value);

            // If a null attribute should be present use the value provided, otherwise default
            if (nullPresent)
            {
                odataProperty.Add(new XAttribute(ODataSerializer.NullName, nullValue));
            }

            // If a type attribute should be specified use value provided
            if (!string.IsNullOrEmpty(typeValue))
            {
                odataProperty.Add(new XAttribute(ODataSerializer.OdataTypeName, typeValue));
            }

            return ODataSerializer.ParseODataProperty(odataProperty);
        }

        /// <summary>Assert we can parse OData property with specified values.</summary>
        /// <param name="odataElement">The odata property element</param>
        /// <param name="expectedName">The expected name.</param>
        /// <param name="expectedValue">The expected value.</param>
        /// <param name="expectedTypeValue">The expected type.</param>
        /// <param name="expectedHasValue">Expected HasValue.</param>
        private void AssertParseODataProperty(ODataElement odataElement, string expectedName, string expectedValue, string expectedTypeValue, bool expectedHasValue)
        {
            Assert.AreEqual(expectedHasValue, odataElement.HasValue);
            Assert.AreEqual(expectedName, odataElement.ODataName);
            Assert.AreEqual(expectedTypeValue, odataElement.ODataType);
            Assert.AreEqual(expectedValue, odataElement.ODataValue);
        }
    }
}
