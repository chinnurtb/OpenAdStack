//-----------------------------------------------------------------------
// <copyright file="ODataSerializer.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Class to serialize collections of values.</summary>
    internal static class ODataSerializer
    {
        /// <summary>Atom namespace</summary>
        internal static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";

        /// <summary>Atom namespace</summary>
        internal static readonly XNamespace ODataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";

        /// <summary>OData metdata namespace</summary>
        internal static readonly XNamespace ODataMetadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

        /// <summary>Atom 'content' element name</summary>
        internal static readonly XName ODataContentName = AtomNamespace + "content";

        /// <summary>OData metadata 'properties' element name</summary>
        internal static readonly XName ODataPropertiesName = ODataMetadataNamespace + "properties";

        /// <summary>OData metadata 'null' property marker name</summary>
        internal static readonly XName NullName = ODataMetadataNamespace + "null";

        /// <summary>OData metadata 'type' name</summary>
        internal static readonly XName OdataTypeName = ODataMetadataNamespace + "type";

        /// <summary>OData default data type (a string) name</summary>
        internal const string OdataDefaultTypeName = "Edm.String";

        /// <summary>Map of OData types to PropertyType</summary>
        private static readonly Dictionary<string, PropertyType> ODataTypeMap = new Dictionary<string, PropertyType>
                {
                    { OdataDefaultTypeName, PropertyType.String },
                    { "Edm.Int32", PropertyType.Int32 },
                    { "Edm.Int64", PropertyType.Int64 },
                    { "Edm.Double", PropertyType.Double },
                    { "Edm.Boolean", PropertyType.Bool },
                    { "Edm.DateTime", PropertyType.Date },
                    { "Edm.Binary", PropertyType.Binary },
                    { "Edm.Guid", PropertyType.Guid }
                };

        /// <summary>Determine if the serialization element is a simple property rather than a collection.</summary>
        /// <param name="odataName">The OData element name.</param>
        /// <returns>True if the name refers to a simple property.</returns>
        public static bool IsEntityProperty(string odataName)
        {
            var name = new ODataName(odataName);
            return !name.IsAssociation() && !name.IsCollection();
        }

        /// <summary>Determine if the serialization element is a simple association rather than a collection.</summary>
        /// <param name="odataElement">The OData element.</param>
        /// <returns>True if the name refers to a simple association.</returns>
        public static bool IsSimpleAssociation(ODataElement odataElement)
        {
            if (!odataElement.HasValue)
            {
                return false;
            }
            
            var odataName = new ODataAssociationName(odataElement.ODataName);
            return odataName.IsAssociation() && !odataName.IsCollection();
        }

        /// <summary>Determine if the serialization element is an association.</summary>
        /// <param name="odataName">The OData element name.</param>
        /// <returns>True if the name refers to an association.</returns>
        public static bool IsAssociation(string odataName)
        {
            return new ODataAssociationName(odataName).IsAssociation();
        }

        /// <summary>Get the collection of OData elements from the OData xml.</summary>
        /// <param name="odataXml">The odata xml.</param>
        /// <returns>Collection of ODataElements</returns>
        public static IEnumerable<ODataElement> GetODataElements(XElement odataXml)
        {
            // Note that in development storage we will get back deleted properties with "null" attribute.
            // This shouldn't happen in cloud storage.
            return odataXml.Element(ODataContentName).Element(ODataPropertiesName).Elements()
                .Select(odataProp => ParseODataProperty(odataProp)).ToList();
        }

        /// <summary>Parse a single OData property element to an ODataElement object.</summary>
        /// <param name="odataProperty">The odata property element.</param>
        /// <returns>An ODataElement object.</returns>
        public static ODataElement ParseODataProperty(XElement odataProperty)
        {
            return new ODataElement
            {
                // If the null attribute is not present, HasValue is true, if the null attribute is present, HasValue has the opposite value
                HasValue = odataProperty.Attribute(NullName) == null || !bool.Parse(odataProperty.Attribute(NullName).Value),
                ODataType = odataProperty.Attribute(OdataTypeName) == null ? OdataDefaultTypeName : odataProperty.Attribute(OdataTypeName).Value,

                // Xml unencode the element name before doing anything further with it.
                ODataName = AzureNameEncoder.UnencodeXmlName(odataProperty.Name.LocalName),
                ODataValue = odataProperty.Value
            };
        }

        /// <summary>Get the OData content elements.</summary>
        /// <param name="data">The OData xml.</param>
        /// <returns>The OData content xml.</returns>
        public static XElement GetODataContent(XElement data)
        {
            return data.Descendants(ODataPropertiesName).First();
        }

        /// <summary>Get the PropertyType value associated with the Odata Type.</summary>
        /// <param name="odataProperty">The odata property.</param>
        /// <returns>The PropertyType.</returns>
        public static PropertyType GetPropertyType(ODataElement odataProperty)
        {
            return ODataTypeMap[odataProperty.ODataType];
        }

        /// <summary>Get the OData Type associated with a given PropertyType.</summary>
        /// <param name="propertyType">The PropertyType.</param>
        /// <returns>ODate type string.</returns>
        public static string GetODataType(PropertyType propertyType)
        {
            return ODataTypeMap.Single(m => m.Value == propertyType).Key;
        }

        /// <summary>Serialize a single EntityProperty to the Atom/OData content element.</summary>
        /// <param name="odataProperties">The OData properties xml.</param>
        /// <param name="entityProperty">The entity property.</param>
        public static void SerializeEntityProperty(XElement odataProperties, EntityProperty entityProperty)
        {
            if (entityProperty == null)
            {
                // Nothing to serialize
                return;
            }

            var value = entityProperty.Value.SerializationValue;
            var type = GetODataType(entityProperty.Value.DynamicType);
            var name = ODataPropertyName.SerializeProperty(entityProperty);

            // Xml encode the property name before using it as an XElement
            var odataProperty = new XElement(ODataNamespace + AzureNameEncoder.EncodeXmlName(name), value);
            odataProperty.Add(new XAttribute(OdataTypeName, type));
            odataProperties.Add(odataProperty);
        }

        /// <summary>Treat the OData element as a collection and deserialize it.</summary>
        /// <param name="entity">The entity to add the collection to.</param>
        /// <param name="odataElement">The OData odataElement.</param>
        public static void DeserializeODataToCollection(IRawEntity entity, ODataElement odataElement)
        {
            var odataName = new ODataName(odataElement.ODataName);

            if (!odataName.IsAssociation() || !odataName.IsCollection())
            {
                throw new DataAccessException("Could not deserialize malformed association in retrieved entity.");
            }

            var associations = DeserializeAssociationCollection(odataElement);
            foreach (var association in associations)
            {
                entity.Associations.Add(association);
            }
        }

        /// <summary>Deserialize a single Association from an OData element.</summary>
        /// <param name="odataElement">The odata element.</param>
        /// <returns>Association object.</returns>
        public static Association DeserializeAssociation(ODataElement odataElement)
        {
            var association = new Association();

            if (!odataElement.HasValue)
            {
                return association;
            }

            var nameFields = new ODataAssociationName(odataElement.ODataName);
            if (!nameFields.IsAssociation() || nameFields.IsCollection())
            {
                throw new DataAccessException("Could not deserialize malformed association in retrieved entity.");
            }

            var valueFields = new ODataAssociationValue(odataElement.ODataValue);

            // The field name will be of the form v_ptr_Relationship_Company_Agency_ParentCompany
            // The field value will be of the form 00000000000000000000000000000001||Agency||ParentCompany
            // The name will be mangled to conform to our rules so we shouldn't have to
            // validate correctness here. We get Category and AssociationType from the
            // name, TargetEntityId, TargetExternalType, and ExternalName from the value.
            // The other name components are not used here.
            association.TargetEntityCategory = nameFields.TargetEntityCategory;
            association.AssociationType = nameFields.AssociationType;
            association.TargetEntityId = valueFields.TargetEntityId;
            association.TargetExternalType = valueFields.TargetExternalType;
            association.ExternalName = valueFields.ExternalName;

            return association;
        }

        /// <summary>Serialize groups of Associations having the same compound key to the odata content xml.</summary>
        /// <param name="odataContent">The odata content.</param>
        /// <param name="associationGroupKey">
        /// An Association instance with the association group key fields populated.
        /// </param>
        /// <param name="targetEntityIds">
        /// One or more TargetEntityId's corresponding to the association group key.
        /// </param>
        internal static void SerializeAssociationGroup(XElement odataContent, Association associationGroupKey, EntityId[] targetEntityIds)
        {
            if (targetEntityIds.Length == 0)
            {
                // Nothing to do
                return;
            }

            var odataName = ODataAssociationName.SerializeAssociationGroup(associationGroupKey);
            var odataValue = ODataAssociationValue.SerializeAssociationGroup(targetEntityIds);

            // Xml encode the association group key name before using it as an XElement
            var odataElement = new XElement(ODataNamespace + AzureNameEncoder.EncodeXmlName(odataName), odataValue);
            odataElement.Add(new XAttribute(OdataTypeName, OdataDefaultTypeName));
            odataContent.Add(odataElement);
        }
        
        /// <summary>Deserialize a group of Associations having the same group key.</summary>
        /// <param name="odataElement">The OData odataElement.</param>
        /// <returns>List of associations.</returns>
        internal static IEnumerable<Association> DeserializeAssociationGroup(ODataElement odataElement)
        {
            if (!odataElement.HasValue)
            {
                return new List<Association>();
            }

            var nameFields = new ODataAssociationName(odataElement.ODataName);
            if (!nameFields.IsAssociation() || !nameFields.IsCollection())
            {
                // All new associations are association groups. Should not get here if
                // this is not an association and not serialized as a collection.
                throw new DataAccessException("Could not deserialize malformed association in retrieved entity.");
            }

            var groupKey = new ODataAssociationName(odataElement.ODataName).AssociationGroupKey;
            var targetIds = new ODataAssociationValue(odataElement.ODataValue).TargetEntityIds;

            if (groupKey == null || targetIds == null)
            {
                // Could not deserialize the association elements
                throw new DataAccessException("Could not deserialize malformed association in retrieved entity.");
            }

            return targetIds.Select(id => new Association(groupKey) { TargetEntityId = id });
        }

        /// <summary>Deserialize to a collection of Associations.</summary>
        /// <param name="odataElement">The OData odataElement.</param>
        /// <returns>List of associations.</returns>
        private static IEnumerable<Association> DeserializeAssociationCollection(ODataElement odataElement)
        {
            var associations = new List<Association>();

            if (!odataElement.HasValue)
            {
                return associations;
            }

            var nameFields = new ODataAssociationName(odataElement.ODataName);
            if (!nameFields.IsAssociation() || !nameFields.IsCollection())
            {
                throw new DataAccessException("Could not deserialize malformed association in retrieved entity.");
            }

            IList<string> valueList = ODataAssociationValue.SplitCollectionValues(odataElement.ODataValue);

            foreach (var value in valueList)
            {
                var association = new Association();
                var valueFields = new ODataAssociationValue(value);

                // The field name will be of the form v_ptr_Relationship_Company_Agency_ParentCompany
                // The field value will be of the form 00000000000000000000000000000001||Agency||ParentCompany
                // The name will be mangled to conform to our rules so we shouldn't have to
                // validate correctness here. We get Category and AssociationType from the
                // name, TargetEntityId, TargetExternalType, and ExternalName from the value.
                // The other name components are not used here.
                association.TargetEntityCategory = nameFields.TargetEntityCategory;
                association.AssociationType = nameFields.AssociationType;
                association.TargetEntityId = valueFields.TargetEntityId;
                association.TargetExternalType = valueFields.TargetExternalType;
                association.ExternalName = valueFields.ExternalName;

                associations.Add(association);
            }

            return associations;
        }
    }
}
