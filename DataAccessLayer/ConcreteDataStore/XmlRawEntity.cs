// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlRawEntity.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Represents an entity that is not deserialized but has been normalized
    /// to a form that is not specific to a data store technology.
    /// </summary>
    internal class XmlRawEntity : Entity
    {
        // TODO: Need methods to manage construction and field serialization in this class
      
        /// <summary>Gets or sets raw serialized properties from xml entity store.</summary>
        public string Fields { get; set; }
    }
}
