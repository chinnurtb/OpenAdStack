//-----------------------------------------------------------------------
// <copyright file="EntityActivityTestHelpers.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using DataAccessLayer;
using EntityUtilities;
using Rhino.Mocks;

namespace EntityActivitiesUnitTests
{
    /// <summary>Helpers for Entity Activity test fixtures</summary>
    public class EntityActivityTestHelpers
    {
        /// <summary>
        /// Build an IEntityFilter stub
        /// </summary>
        /// <param name="includeSystemProperties">True to include system properties.</param>
        /// <param name="includeExtendedProperties">True to include extended properties.</param>
        /// <param name="includeAssociations">True to include associations.</param>
        /// <param name="queryParams">Query param dictionary.</param>
        /// <returns>IEntityFilter stub.</returns>
        public static IEntityFilter BuildEntityFilter(
            bool includeSystemProperties, 
            bool includeExtendedProperties, 
            bool includeAssociations, 
            Dictionary<string, string> queryParams = null)
        {
            var entityFilter = MockRepository.GenerateStub<IEntityFilter>();
            entityFilter.Stub(f => f.IncludeSystemProperties).Return(includeSystemProperties);
            entityFilter.Stub(f => f.IncludeExtendedProperties).Return(includeExtendedProperties);
            entityFilter.Stub(f => f.IncludeAssociations).Return(includeAssociations);
            entityFilter.Stub(f => f.EntityQueries).Return(new EntityActivityQuery(queryParams));
            return entityFilter;
        }
    }
}
