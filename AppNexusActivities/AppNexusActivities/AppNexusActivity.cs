//-----------------------------------------------------------------------
// <copyright file="AppNexusActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using Activities;
using AppNexusActivities.Measures;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using EntityActivities;

using EntityUtilities;

using Utilities.Storage;

namespace AppNexusActivities
{
    /// <summary>Base class for AppNexus related activities</summary>
    public abstract class AppNexusActivity : EntityActivity
    {
        /// <summary>
        /// Gets a value indicating whether the activity is only for AppNexus App users
        /// </summary>
        internal virtual bool AppNexusAppOnly
        {
            get { return false; }
        }

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
            // Default behavior will be not to get or save extended properties.
            // This is the most consistent with current behavior for these activites
            return CreateRepositoryContext(
                new RepositoryEntityFilter(true, true, false, true),
                request,
                contextCompanyValueName);
        }

        /// <summary>Creates an AppNexus client on behalf of the AppNexus user</summary>
        /// <param name="appNexusUserId">Entities used to build custom configuration</param>
        /// <returns>The AppNexus API client</returns>
        internal static IAppNexusApiClient CreateAppNexusClient(string appNexusUserId)
        {
            var config = new ConfigManager.CustomConfig();
            config.Overrides["AppNexus.IsApp"] = true.ToString();
            config.Overrides["AppNexus.App.UserId"] = appNexusUserId;
            return DeliveryNetworkClientFactory.CreateClient<IAppNexusApiClient>(config);
        }

        /// <summary>Creates an AppNexus client using configurations from the entities</summary>
        /// <param name="context">Repository request context used to get the owner UserEntity (if any)</param>
        /// <param name="entities">Entities used to build custom configuration</param>
        /// <returns>The AppNexus API client</returns>
        internal IAppNexusApiClient CreateAppNexusClient(RequestContext context, params IEntity[] entities)
        {
            // Create a config with custom overrides from entities
            var config = EntityActivityUtilities.BuildCustomConfigFromEntities(entities);

            // Check if an owner is set and if so what user type it is
            var ownerId = entities
                .Select(entity => entity.GetOwnerId())
                .LastOrDefault(id => !string.IsNullOrWhiteSpace(id));
            var owner = ownerId != null ? this.Repository.GetUser(context, ownerId) : null;
            var isAppNexusApp = owner != null && owner.GetUserType() == UserType.AppNexusApp;
            config.Overrides["AppNexus.IsApp"] = isAppNexusApp.ToString();

            // Owner is App User, add AppNexus App UserId config override
            if (isAppNexusApp)
            {
                config.Overrides["AppNexus.App.UserId"] = owner.UserId;
            }

            // Create and return the AppNexus API client
            return DeliveryNetworkClientFactory.CreateClient<IAppNexusApiClient>(config);
        }

        /// <summary>
        /// Gets the AppNexus advertiser id for the CompanyEntity.
        /// If the CompanyEntity does not one yet, creates one.
        /// </summary>
        /// <param name="client">AppNexus API client</param>
        /// <param name="context">Entity repository request context</param>
        /// <param name="companyEntity">Company to get the advertiser id of</param>
        /// <returns>AppNexus advertiser id</returns>
        internal int CreateAppNexusAdvertiser(
            IAppNexusApiClient client,
            RequestContext context,
            ref CompanyEntity companyEntity)
        {
            var advertiserId = companyEntity.GetAppNexusAdvertiserId();
            if (advertiserId != null)
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "AppNexus Advertiser ID already set on company '{0}' ({1}): {2}. Not creating a new advertiser.",
                    companyEntity.ExternalName,
                    companyEntity.ExternalEntityId,
                    advertiserId);
                return (int)advertiserId;
            }

            LogManager.Log(
                LogLevels.Trace,
                "No AppNexus Advertiser ID set on company '{0}' ({1}). Checking AppNexus using company entity id as advertiser code.",
                companyEntity.ExternalName,
                companyEntity.ExternalEntityId);

            var advertiser = client.GetAdvertiserByCode(companyEntity.ExternalEntityId.ToString());
            if (advertiser != null)
            {
                advertiserId = (int)advertiser[AppNexusValues.Id];
                LogManager.Log(
                    LogLevels.Trace,
                    "Found AppNexus Advertiser {0} for company '{1}' ({2}).",
                    advertiserId,
                    companyEntity.ExternalName,
                    companyEntity.ExternalEntityId);
            }
            else
            {
                try
                {
                    // Create an advertiser for this company
                    advertiserId = client.CreateAdvertiser(
                        companyEntity.ExternalName,
                        companyEntity.ExternalEntityId.ToString());
                    LogManager.Log(
                        LogLevels.Trace,
                        "Created AppNexus advertiser '{0}' for company '{1}' ({2})",
                        advertiserId,
                        companyEntity.ExternalName,
                        companyEntity.ExternalEntityId);
                }
                catch (AppNexusClientException ace)
                {
                    LogManager.Log(
                        LogLevels.Trace,
                        "Error creating AppNexus advertiser for company '{0}' ({1}): {2}",
                        companyEntity.ExternalName,
                        companyEntity.ExternalEntityId,
                        ace.ErrorMessage);

                    // Check for race condition where advertiser has been created since the above check
                    var advertiserExistsError = "advertiser code {0} is already in use"
                        .FormatInvariant(companyEntity.ExternalEntityId.ToString());
                    if (!ace.ErrorMessage.ToLowerInvariant().Contains(advertiserExistsError))
                    {
                        // Some other error occured
                        throw;
                    }

                    Thread.Sleep(100);
                    advertiser = client.GetAdvertiserByCode(companyEntity.ExternalEntityId.ToString());
                    if (advertiser == null)
                    {
                        // Couldn't find the advertiser
                        throw;
                    }

                    // Return the advertiserId
                    advertiserId = (int)advertiser[AppNexusValues.Id];
                    LogManager.Log(
                        LogLevels.Trace,
                        "Found new AppNexus Advertiser {0} for company '{1}' ({2}).",
                        advertiserId,
                        companyEntity.ExternalName,
                        companyEntity.ExternalEntityId);
                    return (int)advertiserId;
                }
            }

            // Set the company's AppNexus advertiserId and save it
            companyEntity.SetAppNexusAdvertiserId((int)advertiserId);
            this.Repository.SaveEntity(context, companyEntity);
            return (int)advertiserId;
        }

        /// <summary>
        /// Gets the AppNexus ids of the audited creatives for the CampaignEntity.
        /// </summary>
        /// <remarks>Also includes creatives set for no audit</remarks>
        /// <param name="client">AppNexus API client</param>
        /// <param name="context">Entity repository request context</param>
        /// <param name="campaignEntity">Campaign to get the creative ids of</param>
        /// <returns>AppNexus creative ids</returns>
        internal int[] GetAuditedAppNexusCreativeIds(
            IAppNexusApiClient client,
            RequestContext context,
            CampaignEntity campaignEntity)
        {
            var allowedStatuses = new[] { "AUDITED", "NO_AUDIT" };

            // Get all the creatives that are Third Party Ads associated with the campaign
            var creativeEntityIds = campaignEntity.Associations
                .Where(association =>
                    association.TargetEntityCategory == CreativeEntity.CreativeEntityCategory &&
                    (association.TargetExternalType == CreativeType.ThirdPartyAd.ToString() ||
                    association.TargetExternalType == CreativeType.ImageAd.ToString() ||
                    association.TargetExternalType == CreativeType.FlashAd.ToString() ||
                    association.TargetExternalType == CreativeType.AppNexus.ToString()))
                .Select(association => association.TargetEntityId)
                .ToArray();

            // Get the creative entities
            var creativeEntities = this.Repository
                .GetEntitiesById(context, creativeEntityIds)
                .Cast<CreativeEntity>()
                .ToArray();

            // Filter for those that have been exported
            var exportedCreativeEntities = creativeEntities
                .Where(creative => creative.GetAppNexusCreativeId() != null);

            // Update their statuses
            var auditStatuses = exportedCreativeEntities
                .ToDictionary(
                    creative => (int)creative.GetAppNexusCreativeId(),
                    creative => UpdateCreativeAuditStatus.UpdateAuditStatus(
                        this.Repository, context, client, ref creative)
                        .ToUpperInvariant());

            // Log a warning if any creatives have not passed audit
            if (!auditStatuses.All(kvp => allowedStatuses.Contains(kvp.Value)))
            {
                var statuses = exportedCreativeEntities
                    .Select(creative =>
                        "'{0}' ({1}): {2} (AppNexus id: {3})"
                        .FormatInvariant(
                            creative.ExternalName,
                            creative.ExternalEntityId,
                            creative.GetAppNexusAuditStatus(),
                            creative.GetAppNexusCreativeId()));
                LogManager.Log(
                    LogLevels.Warning,
                    "One or more creatives for campaign '{0}' ({1}) have not passed audit:\n{2}",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId,
                    string.Join("\n", statuses));
            }

            // Audit never happens in sandbox, just return them all
            if (Config.GetBoolValue("AppNexus.Sandbox"))
            {
                LogManager.Log(
                    LogLevels.Trace,
                    "Playing in AppNexus Sandbox, returning all creatives for '{0}' ({1}), regardless of audit status.",
                    campaignEntity.ExternalName,
                    campaignEntity.ExternalEntityId);
                return auditStatuses
                    .Select(kvp => kvp.Key)
                    .ToArray();
            }

            // return only the audited creatives' ids
            return auditStatuses
                .Where(kvp => allowedStatuses.Contains(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToArray();
        }

        /// <summary>Processes the request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            try
            {
                if (this.AppNexusAppOnly)
                {
                    var context = CreateContext(request);
                    var userId = request.Values[EntityActivityValues.AuthUserId];
                    var user = this.Repository.GetUser(context, userId);
                    if (user.GetUserType() != UserType.AppNexusApp)
                    {
                        return ErrorResult(
                            ActivityErrorId.UserAccessDenied,
                            "Non-AppNexus App User");
                    }
                }

                return this.ProcessAppNexusRequest(request);
            }
            catch (AppNexusClientException ance)
            {
                StringBuilder message = new StringBuilder();
                Exception e = ance;
                while (e != null)
                {
                    message.AppendLine(e.ToString());
                    e = e.InnerException;
                    if (e != null)
                    {
                        message.AppendLine("----------------------------------------");
                    }
                }

                LogManager.Log(LogLevels.Error, message.ToString());
                return this.AppNexusClientError(ance);
            }
            catch (ActivityException ae)
            {
                return ErrorResult(ae);
            }
        }

        /// <summary>Processes the AppNexus request and returns the result</summary>
        /// <param name="request">The request containing the activity input</param>
        /// <returns>The result containing the output of the activity</returns>
        protected abstract ActivityResult ProcessAppNexusRequest(ActivityRequest request);

        /// <summary>Creates an error ActivityResult for the AppNexusClientException</summary>
        /// <param name="exception">The AppNexusClientException</param>
        /// <returns>The error result</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "This is specifically for AppNexusClientExceptions")]
        protected ActivityResult AppNexusClientError(AppNexusClientException exception)
        {
            return this.ErrorResult(
                ActivityErrorId.GenericError,
                "AppNexusClientError: {0}\n----------------------------------------\n{1}\n----------------------------------------\n{2}",
                exception.ErrorId,
                exception.ErrorMessage,
                exception);
        }
    }
}
