//-----------------------------------------------------------------------
// <copyright file="GetCompanyByEntityIdActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting companies by their entity id
    /// </summary>
    /// <remarks>
    /// Gets the company with the specified EntityId
    /// RequiredValues:
    ///   CompanyEntityId - ExternalEntityId of the company to get
    /// ResultValues:
    ///   Company - The company as json
    /// </remarks>
    [Name(EntityActivityTasks.GetCompanyByEntityId)]
    [RequiredValues(EntityActivityValues.EntityId)]
    [ResultValues(EntityActivityValues.Company)]
    public class GetCompanyByEntityIdActivity : GetEntityByEntityIdActivityBase
    {
        /// <summary>
        /// Gets the expected EntityCategory of the returned entity
        /// </summary>
        protected override string EntityCategory
        {
            get { return CompanyEntity.CompanyEntityCategory; }
        }

        /// <summary>
        /// Gets the name of the result value in which to return the entity
        /// </summary>
        protected override string ResultValue
        {
            get { return EntityActivityValues.Company; }
        }
    }
}
