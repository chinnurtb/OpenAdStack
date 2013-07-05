//-----------------------------------------------------------------------
// <copyright file="GetCompanyByEntityIdActivity.cs" company="Rare Crowds Inc">
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
            get { return CompanyEntity.CategoryName; }
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
