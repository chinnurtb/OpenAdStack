// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KeyRuleFactory.cs" company="Rare Crowds Inc">
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

namespace ConcreteDataStore
{
    /// <summary>Concrete default key rule factory</summary>
    internal class KeyRuleFactory : IKeyRuleFactory
    {
        /// <summary>Get a rule to generate a specific element of a key.</summary>
        /// <param name="entity">The entity.</param>
        /// <param name="storageType">The storage type.</param>
        /// <param name="keyFieldType">The type of key element the rule is for.</param>
        /// <returns>An IKeyRule</returns>
        public IKeyRule GetKeyRule(IEntity entity, string storageType, string keyFieldType)
        {
            // TODO: Full IKeyRule lookup mechanism.
            return new DefaultKeyRule();
        }
    }
}
