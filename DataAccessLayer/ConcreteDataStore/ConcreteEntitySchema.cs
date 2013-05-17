// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConcreteEntitySchema.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>Default implementation of IEntitySchema</summary>
    public class ConcreteEntitySchema : IEntitySchema
    {
        /// <summary>Map of feature id's to the version they are first supported.</summary>
        private static readonly Dictionary<EntitySchemaFeatureId, int> FeatureMap = new Dictionary<EntitySchemaFeatureId, int>
            {
                { EntitySchemaFeatureId.NameEncoding, 1 },
                { EntitySchemaFeatureId.AssociationGroups, 1 }
            };

        /// <summary>Gets the current schema version for IRawEntity.</summary>
        public int CurrentSchemaVersion
        {
            get { return 1; }
        }

        /// <summary>Check that a schema version supports a particular schema feature.</summary>
        /// <param name="schemaVersion">The schema version of the entity.</param>
        /// <param name="featureId">The feature id being checked against the schema version.</param>
        /// <returns>True if the feature is supported</returns>
        public bool CheckSchemaFeature(int schemaVersion, EntitySchemaFeatureId featureId)
        {
            return schemaVersion >= FeatureMap[featureId];
        }
    }
}
