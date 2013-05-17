// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntitySchema.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>Interface defining entity schema behavior.</summary>
    public interface IEntitySchema
    {
        /// <summary>Gets the current schema version for IRawEntity.</summary>
        int CurrentSchemaVersion { get; }

        /// <summary>Check that a schema version supports a particular schema feature.</summary>
        /// <param name="schemaVersion">The schema version of the entity.</param>
        /// <param name="featureId">The feature id being checked against the schema version.</param>
        /// <returns>True if the feature is supported</returns>
        bool CheckSchemaFeature(int schemaVersion, EntitySchemaFeatureId featureId);
    }
}
