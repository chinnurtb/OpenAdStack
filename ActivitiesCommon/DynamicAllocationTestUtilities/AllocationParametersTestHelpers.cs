//Copyright 2012-2013 Rare Crowds, Inc.
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
