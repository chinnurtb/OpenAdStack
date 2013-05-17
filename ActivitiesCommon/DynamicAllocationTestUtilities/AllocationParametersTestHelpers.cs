// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationParametersTestHelpers.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using DataAccessLayer;
using EntityUtilities;

namespace DynamicAllocationTestUtilities
{
    /// <summary>Test helpers for AllocationParameters.</summary>
    public static class AllocationParametersTestHelpers
    {
        /// <summary>Set default test values for the AllocationParameters on an entity.</summary>
        /// <param name="entity">The entity to which the AllocationParameters should be added.</param>
        [SuppressMessage("Microsoft.Performance", "CA1804", Justification = "Intermittent false positive")]
        public static void Initialize(IEntity entity)
        {
            var config = entity.GetConfigSettings();
            var defaultParams = TestUtilities.AllocationParametersDefaults.InitializeDictionary();
            foreach (var defaultParam in defaultParams)
            {
                config[defaultParam.Key] = defaultParam.Value;
            }

            entity.SetConfigSettings(config);
        }
    }
}
