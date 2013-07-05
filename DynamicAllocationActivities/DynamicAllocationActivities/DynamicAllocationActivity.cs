//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationActivity.cs" company="Rare Crowds Inc">
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

using Activities;
using DataAccessLayer;
using EntityActivities;
using EntityUtilities;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace DynamicAllocationActivities
{
    /// <summary>Abstract base class for activities dealing with DA</summary>
    public abstract class DynamicAllocationActivity : EntityActivity
    {
        /// <summary>
        /// Creates an entity repository RequestContext from an ActivityRequest
        /// </summary>
        /// <param name="request">The request containing context information</param>
        /// <returns>The created request context</returns>
        internal static RequestContext CreateContext(ActivityRequest request)
        {
            return CreateContext(
                request,
                EntityActivityValues.CompanyEntityId);
        }

        /// <summary>
        /// Creates an entity repository RequestContext from an ActivityRequest
        /// </summary>
        /// <param name="request">The request containing context information</param>
        /// <param name="contextCompanyValueName">
        /// The name of a request value to use as the context's ExternalCompanyId.
        /// Required if you are updating an entity that is not a user, or
        /// creating an entity that is not a user or company.
        /// </param>
        /// <returns>The created request context</returns>
        internal static RequestContext CreateContext(ActivityRequest request, string contextCompanyValueName)
        {
            // Default behavior will be not to filter anything (null IEntityFilter).
            return CreateRepositoryContext(
                null,
                request,
                contextCompanyValueName);
        }
    }
}
