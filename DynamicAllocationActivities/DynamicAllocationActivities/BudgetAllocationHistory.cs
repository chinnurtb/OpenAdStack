// -----------------------------------------------------------------------
// <copyright file="BudgetAllocationHistory.cs" company="Rare Crowds Inc">
//  Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using DynamicAllocationUtilities;
using Utilities.Serialization;

namespace DynamicAllocationActivities
{
    /// <summary>Class to manage Budget Allocation History on the campaign entity.</summary>
    public class BudgetAllocationHistory : IBudgetAllocationHistory
    {
        /// <summary>Initializes a new instance of the <see cref="BudgetAllocationHistory"/> class.</summary>
        /// <param name="repository">The IEntityRepository instance.</param>
        /// <param name="companyEntity">The companyEntity for the campaign.</param>
        /// <param name="campaignEntity">The campaignEntity.</param>
        public BudgetAllocationHistory(IEntityRepository repository, CompanyEntity companyEntity, CampaignEntity campaignEntity)
        {
            this.Repository = repository;
            this.CompanyEntity = companyEntity;
            this.CampaignEntity = campaignEntity;
        }

        /// <summary>Gets the company entity associated with the DA Campaign.</summary>
        public CompanyEntity CompanyEntity { get; internal set; }

        /// <summary>Gets the campaign entity associated with the DA Campaign.</summary>
        public CampaignEntity CampaignEntity { get; internal set; }

        /// <summary>Gets or sets the IEntityRepository instance associated with the DA Campaign.</summary>
        internal IEntityRepository Repository { get; set; }

        /// <summary>Retrieve the allocation history index.</summary>
        /// <returns>The list of history elements.</returns>
        public IEnumerable<HistoryElement> RetrieveAllocationHistoryIndex()
        {
            var context = new RequestContext
            {
                ExternalCompanyId = this.CompanyEntity.ExternalEntityId,
                EntityFilter = new RepositoryEntityFilter(true, true, true, true)
            };

            // Get the BudgetAllocationHistoryIndex blob association
            var budgetAllocationHistoryAssociation =
                this.CampaignEntity.TryGetAssociationByName(DynamicAllocationEntityProperties.AllocationHistoryIndex);
            if (budgetAllocationHistoryAssociation == null)
            {
                throw new ActivityException(ActivityErrorId.GenericError, "No DA Allocation History Index Association.");
            }

            // Get the BudgetAllocationHistoryIndex blob
            BlobEntity blobEntity;
            try
            {
                blobEntity = this.Repository.GetEntity<BlobEntity>(
                    context, budgetAllocationHistoryAssociation.TargetEntityId);
            }
            catch (DataAccessException e)
            {
                throw new ActivityException(ActivityErrorId.DataAccess, "Error retrieving DA Allocation History Index Blob.", e);
            }

            // Deserialize the index
            try
            {
                return blobEntity.DeserializeBlob<List<HistoryElement>>();
            }
            catch (AppsJsonException e)
            {
                throw new ActivityException(ActivityErrorId.InvalidJson, "Error deserializing DA Allocation History Index Blob.", e);
            }
        }
    }
}
