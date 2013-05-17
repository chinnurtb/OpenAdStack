// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntitySchemaFeatureId.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// Enum of schema feature identifiers
    /// </summary>
    public enum EntitySchemaFeatureId
    {
        /// <summary>Property names are encoded to ensure they are valid in the Azure Table Store.</summary>
        NameEncoding,

        /// <summary>Associations are serialized as a json array of TargetExternalId by a common group key.</summary>
        AssociationGroups
    }
}
