//-----------------------------------------------------------------------
// <copyright file="GetCreativeByEntityIdActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting creative by their entity id
    /// </summary>
    /// <remarks>
    /// Gets the creative with the specified EntityId
    /// RequiredValues:
    ///   CompanyEntityId - ExternalEntityId of the company containing the campaign
    ///   CampaignEntityId - ExternalEntityId of the campaign to get
    /// ResultValues:
    ///   Campaign - The campaign as json
    /// </remarks>
    [Name(EntityActivityTasks.GetCreativeByEntityId)]
    [RequiredValues(EntityActivityValues.ParentEntityId, EntityActivityValues.EntityId)]
    [ResultValues(EntityActivityValues.Creative)]
    public class GetCreativeByEntityIdActivity : GetEntityByEntityIdActivityBase
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
            get { return CreativeEntity.CreativeEntityCategory; }
        }

        /// <summary>
        /// Gets the name of the result value in which to return the entity
        /// </summary>
        protected override string ResultValue
        {
            get { return EntityActivityValues.Creative; }
        }
    }
}
