//-----------------------------------------------------------------------
// <copyright file="EntityActivityUtilities.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ConfigManager;
using DataAccessLayer;
using ResourceAccess;

namespace EntityUtilities
{
    /// <summary>Utilities for entity activities</summary>
    public static class EntityActivityUtilities
    {
        /// <summary>
        /// Builds a CustomConfig from the Config system properties of the provided entities.
        /// </summary>
        /// <remarks>
        /// Configuration settings are composited with those from entities later in the
        /// enumeration overwriting those of earlier entities.
        /// </remarks>
        /// <param name="entities">Entities from which to build custom config</param>
        /// <returns>The custom config</returns>
        public static CustomConfig BuildCustomConfigFromEntities(params IEntity[] entities)
        {
            return BuildCustomConfigFromEntities(true, entities);
        }

        /// <summary>
        /// Builds a CustomConfig from the Config system properties of the provided entities.
        /// </summary>
        /// <remarks>
        /// Configuration settings are composited with those from entities later in the
        /// enumeration overwriting those of earlier entities.
        /// </remarks>
        /// <param name="transparent">
        /// Whether to include settings from earlier entities that
        /// are not present in configs from later entities.
        /// (default is true)
        /// </param>
        /// <param name="entities">
        /// Entities from which to build custom config
        /// </param>
        /// <returns>The custom config</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is captured in log")]
        public static CustomConfig BuildCustomConfigFromEntities(bool transparent, params IEntity[] entities)
        {
            // Build a set of custom configuration from the entities
            var entityConfigs = entities
                .Where(e => e != null)
                .Select(e => e.GetConfigSettings())
                .Where(settings => settings != null)
                .ToArray();

            // Create the set of overrides from the entity configs
            IDictionary<string, string> overrides = null;
            if (transparent)
            {
                // Create a composite of the entitiy configs from each entity
                overrides = new Dictionary<string, string>();
                foreach (var entityConfig in entityConfigs)
                {
                    overrides.Add(entityConfig);
                }
            }
            else
            {
                // Just use the config from the last entity
                overrides =
                    entityConfigs.LastOrDefault() ??
                    new Dictionary<string, string> { };
            }

            // Create and return the custom configuration
            return new CustomConfig(overrides);
        }

        /// <summary>Gets all companies visible to a user</summary>
        /// <param name="accessHandler">Access handler</param>
        /// <param name="repository">Entity repository</param>
        /// <param name="context">Repository request context</param>
        /// <param name="user">The user</param>
        /// <returns>The user's visible companies</returns>
        public static IEnumerable<CompanyEntity> GetCompaniesForUser(
            IResourceAccessHandler accessHandler,
            IEntityRepository repository,
            RequestContext context,
            UserEntity user)
        {
            // Get all the companies and returns the ones htat this user is allowed to see
            var allCompanies = repository.GetAllCompanies(context);
            var visibleCompanies = new ConcurrentBag<CompanyEntity>();
            Parallel.ForEach(
                allCompanies,
                company =>
                {
                    var canonicalResource =
                        new CanonicalResource(
                            new Uri("https://localhost/api/entity/company/{0}".FormatInvariant(company.ExternalEntityId.ToString()), UriKind.Absolute), "GET");
                    if (accessHandler.CheckAccess(canonicalResource, user.ExternalEntityId))
                    {
                        visibleCompanies.Add(company as CompanyEntity);
                    }
                });
            return visibleCompanies;
        }
    }
}
