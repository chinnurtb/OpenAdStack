// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ODataPropertyName.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Text;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Abstraction of an OData name for a property.
    /// The field name for an association will be of the 
    /// form sys_PropertyName or just PropertyName
    /// </summary>
    internal class ODataPropertyName : ODataName
    {
        /// <summary>Initializes a new instance of the <see cref="ODataPropertyName"/> class.</summary>
        /// <param name="odataName">The odata name.</param>
        public ODataPropertyName(string odataName)
            : base(odataName)
        {
        }

        /// <summary>Gets the EntityProperty.Name</summary>
        /// <remarks>Returns all non-marker fields</remarks>
        public string PropertyName
        {
            get
            {
                return string.Join(
                    ODataNameDelimiter,
                    this.ODataNameFields.Where(f => !Markers.Contains(f)));
            }
        }

        /// <summary>Gets the property filter for this property name.</summary>
        public PropertyFilter Filter
        {
            get
            {
                var filter = PropertyFilter.Default;

                if (this.IsSystem())
                {
                    filter = PropertyFilter.System;
                }
                else if (this.IsExtended())
                {
                    filter = PropertyFilter.Extended;
                }

                return filter;
            }
        }

        /// <summary>Serialize an EntityProperty to an odata name.</summary>
        /// <param name="entityProperty">The entity property.</param>
        /// <returns>The odata name</returns>
        public static string SerializeProperty(EntityProperty entityProperty)
        {
            var sb = new StringBuilder();

            // For non-default property filter apply the correct marker
            switch (entityProperty.Filter)
            {
                case PropertyFilter.Extended:
                    sb.Append(ExtendedMarker);
                    sb.Append(ODataNameDelimiter);
                    break;
                case PropertyFilter.System:
                    sb.Append(SystemMarker);
                    sb.Append(ODataNameDelimiter);
                    break;
            }

            if (entityProperty.IsBlobRef)
            {
                sb.Append(BlobRefMarker);
                sb.Append(ODataNameDelimiter);
            }

            sb.Append(entityProperty.Name);
            return sb.ToString();
        }
    }
}