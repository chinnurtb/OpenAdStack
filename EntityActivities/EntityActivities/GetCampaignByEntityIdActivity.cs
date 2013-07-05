//-----------------------------------------------------------------------
// <copyright file="GetCampaignByEntityIdActivity.cs" company="Rare Crowds Inc">
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
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting campaigns by their entity id
    /// </summary>
    /// <remarks>
    /// Gets the campaign with the specified EntityId
    /// RequiredValues:
    ///   CompanyEntityId - ExternalEntityId of the company containing the campaign
    ///   CampaignEntityId - ExternalEntityId of the campaign to get
    /// ResultValues:
    ///   Campaign - The campaign as json
    /// </remarks>
    [Name(EntityActivityTasks.GetCampaignByEntityId)]
    [RequiredValues(EntityActivityValues.ParentEntityId, EntityActivityValues.EntityId)]
    [ResultValues(EntityActivityValues.Campaign)]
    public class GetCampaignByEntityIdActivity : GetEntityByEntityIdActivityBase
    {
        /// <summary>
        /// Gets the name of the request value containing the context company's ExternalEntityId
        /// </summary>
        protected override string ContextCompanyEntityIdValue
        {
            get { return EntityActivityValues.ParentEntityId; }
        }

        /// <summary>
        /// Gets the expected EntityCategory of the returned entity
        /// </summary>
        protected override string EntityCategory
        {
            get { return CampaignEntity.CategoryName; }
        }

        /// <summary>
        /// Gets the name of the result value in which to return the entity
        /// </summary>
        protected override string ResultValue
        {
            get { return EntityActivityValues.Campaign; }
        }
    }
}
