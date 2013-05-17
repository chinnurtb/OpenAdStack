//-----------------------------------------------------------------------
// <copyright file="GetAdvertisersActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Activities;
using AppNexusClient;
using AppNexusUtilities;
using ConfigManager;
using DataAccessLayer;
using DataServiceUtilities;
using Diagnostics;
using EntityActivities;
using EntityUtilities;
using Newtonsoft.Json;
using ResourceAccess;

namespace AppNexusActivities.AppActivities
{
    /// <summary>
    /// Activity for getting AppNexus App users' advertisers
    /// </summary>
    /// <remarks>
    /// Retrieves users' advertisers from AppNexus
    /// RequiredValues:
    ///   AuthUserId - The user's user id (used to call AppNexus APIs)
    /// </remarks>
    [Name(AppNexusActivityTasks.GetAdvertisers)]
    [RequiredValues(EntityActivityValues.AuthUserId)]
    public class GetAdvertisersActivity : DataServiceActivityBase<KeyValuePair<int, string>>
    {
        /// <summary>Gets the activity's runtime category</summary>
        public override ActivityRuntimeCategory RuntimeCategory
        {
            get { return ActivityRuntimeCategory.InteractiveFetch; }
        }

        /// <summary>
        /// Gets the entity repository from the activity context
        /// </summary>
        private IEntityRepository Repository
        {
            get { return (IEntityRepository)this.Context[typeof(IEntityRepository)]; }
        }

        /// <summary>
        /// Gets the user access repository from the activity context
        /// </summary>
        private IUserAccessRepository UserAccessRepository
        {
            get { return (IUserAccessRepository)this.Context[typeof(IUserAccessRepository)]; }
        }

        /// <summary>
        /// Gets the access handler from the activity context, or returns a new access handler
        /// </summary>
        private IResourceAccessHandler AccessHandler
        {
            get
            {
                if (this.Context.ContainsKey(typeof(IResourceAccessHandler)))
                {
                    return (IResourceAccessHandler)this.Context[typeof(IResourceAccessHandler)];
                }
                else
                {
                    return new ResourceAccessHandler(this.UserAccessRepository, this.Repository);
                }
            }
        }

        /// <summary>Gets the path for the specified result</summary>
        /// <param name="result">Result of which to get the path</param>
        /// <returns>The result path</returns>
        protected override string GetResultPath(KeyValuePair<int, string> result)
        {
            return result.Value;
        }

        /// <summary>Checks whether the specified result has been loaded</summary>
        /// <param name="result">The result</param>
        /// <returns>True if loaded, otherwise, false</returns>
        protected override bool IsResultLoaded(KeyValuePair<int, string> result)
        {
            return true;
        }

        /// <summary>Gets the importable advertiser results</summary>
        /// <param name="requestValues">Request Values</param>
        /// <returns>The results</returns>
        protected override KeyValuePair<int, string>[] GetResults(IDictionary<string, string> requestValues)
        {
            var userId = requestValues[EntityActivityValues.AuthUserId];

            // Get the user's existing companies
            var context = EntityActivity.CreateRepositoryContext(
                RepositoryContextType.InternalEntityGet,
                new ActivityRequest { Values = { { EntityActivityValues.AuthUserId, userId } } });

            // Check that user is an AppNexusApp user
            var user = this.Repository.GetUser(context, userId);
            if (user.GetUserType() != UserType.AppNexusApp)
            {
                throw new ActivityException(
                    ActivityErrorId.UserAccessDenied,
                    "Activity not supported for non-AppNexusApp users");
            }

            // Get and return unimported advertisers
            return this.GetImportableAppNexusAdvertisers(context, user);
        }
        
        /// <summary>Formats the results as XML</summary>
        /// <remarks>Not supported by base class</remarks>
        /// <param name="results">Results to be formatted</param>
        /// <param name="subtreePath">Path of the root of the results</param>
        /// <returns>The XML formatted results</returns>
        protected override string FormatResultsAsXml(
            IDictionary<string, object> results,
            string subtreePath)
        {
            var startTime = DateTime.UtcNow;
            var resultsXml =
                DataServiceActivityUtilities.FormatResultsAsDhtmlxTreeGridXml<KeyValuePair<int, string>>(
                results,
                subtreePath,
                r => r.Key.ToString(CultureInfo.InvariantCulture),
                this.ResultPathSeparator);
            LogManager.Log(
                LogLevels.Trace,
                "Formatted {0} advertisers as XML (duration: {1}s)",
                results.Count,
                (DateTime.UtcNow - startTime).TotalSeconds);
            return resultsXml;
        }

        /// <summary>Gets the importable AppNexus advertisers for the user as a JSON list</summary>
        /// <param name="context">Repository context</param>
        /// <param name="user">The user</param>
        /// <returns>Importable AppNexus advertisers</returns>
        private KeyValuePair<int, string>[] GetImportableAppNexusAdvertisers(RequestContext context, UserEntity user)
        {
            var companies = EntityActivityUtilities.GetCompaniesForUser(this.AccessHandler, this.Repository, context, user);
            var importedAdvertisers = companies
                .Select(c => c.GetAppNexusAdvertiserId())
                .Where(id => id != null)
                .Select(id => id.Value)
                .ToArray();

            using (var client = AppNexusActivity.CreateAppNexusClient(user.UserId))
            {
                // Get all advertisers then filter for only unimported
                var appNexusAdvertisers = client.GetMemberAdvertisers();
                var advertisers = appNexusAdvertisers
                    .Select(advertiser => new KeyValuePair<int, string>(
                        (int)advertiser[AppNexusValues.Id],
                        (string)advertiser[AppNexusValues.Name]))
                    .Distinct(kvp => kvp.Key)
                    .ToDictionary();
                foreach (var advertiser in importedAdvertisers)
                {
                    advertisers.Remove(advertiser);
                }

                return advertisers.ToArray();
            }
        }
    }
}
