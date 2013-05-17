//-----------------------------------------------------------------------
// <copyright file="EntityActivityTestHelpers.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
