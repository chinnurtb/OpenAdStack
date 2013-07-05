//-----------------------------------------------------------------------
// <copyright file="SaveCampaignActivity.cs" company="Rare Crowds Inc">
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
using System.Diagnostics.CodeAnalysis;
using Activities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocationUtilities;
using EntityUtilities;
using Newtonsoft.Json;
using ResourceAccess;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace EntityActivities
{
    /// <summary>
    /// Activity for saving changes to an existing campaigns
    /// </summary>
    /// <remarks>
    /// Updates an existing campaign. Depending on what is updated may also trigger
    /// additional activities.
    /// RequiredValues
    ///   CompanyEntityId - ExternalEntityId of the company to add campaigns to
    ///   CampaignEntityId - ExternalEntityId for the campaign
    ///   Campaign - The campaign as json
    /// </remarks>
    [Name(EntityActivityTasks.SaveCampaign)]
    [RequiredValues(
        EntityActivityValues.ParentEntityId,
        EntityActivityValues.EntityId,
        EntityActivityValues.MessagePayload)]
    public class SaveCampaignActivity : EntityActivity
    {
        /// <summary>Handler for chained activity results</summary>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityResult result)
        {
            if (!result.Succeeded)
            {
                LogManager.Log(
                    LogLevels.Error,
                    "Error processing activity:",
                    result.Error.Message);
            }
        }

        /// <summary>Determine if the valuations have been modified in some way.</summary>
        /// <param name="original">The original campaign entity.</param>
        /// <param name="updated">The updated campaign entity.</param>
        /// <returns>True if valuations were modified.</returns>
        internal static bool CheckValuationsModified(CampaignEntity original, CampaignEntity updated)
        {
            var originalNodeValuationSetJson = original.TryGetPropertyValueByName(daName.NodeValuationSet);
            var updatedNodeValuationSetJson = updated.TryGetPropertyValueByName(daName.NodeValuationSet);
            if (TryCheckValuationJsonModified(originalNodeValuationSetJson, updatedNodeValuationSetJson))
            {
                return true;
            }

            var originalMeasureListJson = original.TryGetPropertyValueByName(daName.MeasureList);
            var updatedMeasureListJson = updated.TryGetPropertyValueByName(daName.MeasureList);
            if (TryCheckValuationJsonModified(originalMeasureListJson, updatedMeasureListJson))
            {
                return true;
            }

            return false;
        }

        /// <summary>Determine if approved valuations should be updated.</summary>
        /// <param name="original">The original campaign entity.</param>
        /// <param name="updated">The updated campaign entity.</param>
        /// <param name="valuationsModified">True if valuation inputs have been modified.</param>
        /// <returns>True if approved valuations need to be updated.</returns>
        internal static bool CheckIfApprovedValuationsNeedUpdate(
            CampaignEntity original, CampaignEntity updated, bool valuationsModified)
        {
            var originalGoLiveApproval = original.TryGetPropertyByName<string>(daName.Status, null) == daName.StatusApproved;
            var updatedGoLiveApproval = updated.TryGetPropertyByName<string>(daName.Status, null) == daName.StatusApproved;
            var billingApproval = original.TryGetPropertyByName(BillingActivityNames.IsBillingApproved, false);
            var updateApproval = updatedGoLiveApproval && billingApproval;

            // If we are not in the approved state there is nothing to do.
            if (!updateApproval)
            {
                return false;
            }

            // Either approval status needs to have changed, or valuations
            // need to have changed, otherwise we do nothing.
            return !originalGoLiveApproval || valuationsModified;
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing success or an error</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(
                RepositoryContextType.ExternalEntitySave, request, EntityActivityValues.ParentEntityId);
            var internalContext = CreateRepositoryContext(
                RepositoryContextType.InternalEntityGet, request);
            var campaignEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
            var campaign = EntityJsonSerializer.DeserializeCampaignEntity(
                campaignEntityId,
                request.Values[EntityActivityValues.MessagePayload]);

            // Check that the campaign already exists
            var original = this.Repository.TryGetEntity(internalContext, campaignEntityId) as CampaignEntity;
            if (original == null)
            {
              return EntityNotFoundError(campaignEntityId);
            }

            // verify the user can write to this campaign
            var userId = request.Values[EntityActivityValues.AuthUserId];
            UserEntity user = null;
            try
            {
                // Get the user
                user = this.Repository.GetUser(internalContext, userId);
            }
            catch (ArgumentException)
            {
                return UserNotFoundError(userId);
            }

            var canonicalResourceUri = "https://localhost/api/entity/company/{0}/campaign/{1}"
                .FormatInvariant(request.Values[EntityActivityValues.ParentEntityId], request.Values[EntityActivityValues.EntityId]);
            var canonicalResource =
                new CanonicalResource(
                    new Uri(canonicalResourceUri, UriKind.Absolute), "POST");
            if (!this.AccessHandler.CheckAccess(canonicalResource, user.ExternalEntityId))
            {
                return UserNotAuthorized(request.Values[EntityActivityValues.EntityId]);
            }

            // Copy unset properties from original
            CopyPropertiesFromOriginal(original, ref campaign);

            // Set the owner to current user if missing OwnerId
            if (string.IsNullOrWhiteSpace(campaign.TryGetPropertyByName<string>("OwnerId", null)))
            {
                campaign.SetOwnerId(user.UserId);
            }

            // Check if valuation inputs have changed
            var valuationsModified = CheckValuationsModified(original, campaign);

            // Save the campaign
            this.Repository.SaveEntity(externalContext, campaign);

            // Submit valuation approval activity if required
            this.SubmitIfApprovalStatusChanged(original, campaign, valuationsModified, externalContext);

            return this.SuccessResult();
        }

        /// <summary>Check whether a valuation inputs json was modifed at the collection level.</summary>
        /// <param name="originalJson">The original json.</param>
        /// <param name="updatedJson">The updated json.</param>
        /// <returns>True if modified.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private static bool TryCheckValuationJsonModified(
            PropertyValue originalJson, PropertyValue updatedJson)
        {
            if (originalJson != updatedJson)
            {
                return true;
            }

            if (originalJson != null && updatedJson != null)
            {
                try
                {
                    // If neither is null but one or both of them cannot be correctly deserialized
                    // treat it as if the valuation inputs have not changed.
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(originalJson);
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(updatedJson);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if approval status has changed and submit to the 
        /// valuation approval activity if required.
        /// </summary>
        /// <param name="original">The original campaign entity.</param>
        /// <param name="updated">The updated campaign entity.</param>
        /// <param name="valuationsModified">True if valuation inputs have been modified.</param>
        /// <param name="context">The repository request context.</param>
        private void SubmitIfApprovalStatusChanged(
            CampaignEntity original, CampaignEntity updated, bool valuationsModified, RequestContext context)
        {
            if (!CheckIfApprovedValuationsNeedUpdate(original, updated, valuationsModified))
            {
                return;
            }

            // Verify campaign has everything needed for approving valuation inputs
            var company = this.Repository.GetEntity<CompanyEntity>(context, context.ExternalCompanyId);
            var owner = this.Repository.GetUser(context, updated.GetOwnerId());
            DynamicAllocationActivityUtilities.VerifyHasRequiredValuationInputs(company, updated, owner);

            var approveRequest = new ActivityRequest
            {
                Task = "DAApproveValuationInputs",
                Values =
                        {
                            { EntityActivityValues.AuthUserId, context.UserId },
                            { EntityActivityValues.CompanyEntityId, context.ExternalCompanyId },
                            { EntityActivityValues.CampaignEntityId, updated.ExternalEntityId.Value.SerializationValue }
                        }
            };

            // Let the approve activity determine the appropriate action.
            this.SubmitRequest(approveRequest, true);
        }
    }
}
