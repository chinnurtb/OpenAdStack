//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
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
