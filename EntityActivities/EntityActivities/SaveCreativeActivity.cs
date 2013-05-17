//-----------------------------------------------------------------------
// <copyright file="SaveCreativeActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Activities;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;
using ResourceAccess;
using ScheduledActivities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for Creating a new creative / saving an existing creative
    /// </summary>
    [Name(EntityActivityTasks.SaveCreative)]
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.ParentEntityId, EntityActivityValues.MessagePayload)]
    [ResultValues(EntityActivityValues.Creative)]
    public class SaveCreativeActivity : EntityActivity
    {
        /// <summary>Handles the results of submitted (chained) activity requests</summary>
        /// <param name="result">The result</param>
        public override void OnActivityResult(ActivityResult result)
        {
            if (!result.Succeeded && result.Task == AppNexusActivityTasks.ExportCreative)
            {
                // TODO: Resubmit?
                LogManager.Log(
                    LogLevels.Error,
                    "Failed to export creative to AppNexus: {0}",
                    result.Error.Message);
            }
        }

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception returned in error ActivityResult")]
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            var externalContext = CreateRepositoryContext(
                RepositoryContextType.ExternalEntitySave, request, EntityActivityValues.ParentEntityId);
            var internalContext = CreateRepositoryContext(
                RepositoryContextType.InternalEntityGet, request);

            var advertiser = this.Repository.GetEntity<CompanyEntity>(internalContext, externalContext.ExternalCompanyId);
            var user = this.Repository.GetUser(internalContext, externalContext.UserId);

            var creative = EntityJsonSerializer.DeserializeCreativeEntity(
                request.Values[EntityActivityValues.EntityId],
                request.Values[EntityActivityValues.MessagePayload]);
            
            // Check if the creative already exists
            var original = this.Repository.TryGetEntity(internalContext, creative.ExternalEntityId);

            // verify the user can save this creative
            var canonicalResource =
                original == null ?
                new CanonicalResource(
                    new Uri(
                        "https://localhost/api/entity/company/{0}/creative"
                        .FormatInvariant(request.Values[EntityActivityValues.ParentEntityId]),
                        UriKind.Absolute),
                    "POST") :
                new CanonicalResource(
                    new Uri(
                        "https://localhost/api/entity/company/{0}/creative/{1}"
                        .FormatInvariant(request.Values[EntityActivityValues.ParentEntityId], request.Values[EntityActivityValues.EntityId]),
                        UriKind.Absolute),
                    "PUT");
            if (!this.AccessHandler.CheckAccess(canonicalResource, user.ExternalEntityId))
            {
                return UserNotAuthorized(request.Values[EntityActivityValues.EntityId]);
            }

            // Set the owner to current user for new creatives or creatives missing OwnerId
            if (original == null || string.IsNullOrWhiteSpace(creative.TryGetPropertyByName<string>("OwnerId", null)))
            {
                creative.SetOwnerId(user.UserId);
            }

            // Save the creative
            this.Repository.SaveEntity(externalContext, creative);

            // Associate the new creative with the Company
            this.Repository.AssociateEntities(
                internalContext,
                request.Values[EntityActivityValues.ParentEntityId],
                "creative",
                string.Empty,
                new HashSet<IEntity> { creative },
                AssociationType.Child,
                false);

            // Register the Creative to be exported to the delivery network
            try
            {
                var deliveryNetwork = advertiser.GetDeliveryNetwork();
                Scheduler.AddToSchedule<string, DeliveryNetworkDesignation>(
                    DeliveryNetworkSchedulerRegistries.CreativesToExport,
                    DateTime.UtcNow,
                    creative.ExternalEntityId.ToString(),
                    advertiser.ExternalEntityId.ToString(),
                    deliveryNetwork);
            }
            catch (Exception e)
            {
                return this.ErrorResult(
                    ActivityErrorId.GenericError,
                    "Unable to schedule creative '{0}' ({1}) for export:\n'{2}'",
                    creative.ExternalName,
                    creative.ExternalEntityId,
                    e);
            }

            return this.SuccessResult(new Dictionary<string, string>
            {
                { EntityActivityValues.Creative, creative.SerializeToJson() }
            });
        }
    }
}
