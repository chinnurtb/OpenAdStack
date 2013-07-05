// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    /// <summary>Extension methods for IEntityRepository and closely related classes.</summary>
    public static class Extensions
    {
        /// <summary>Do a parallel TryGet of the entity id's given.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">Context information for the request (only EntityFilter is used).</param>
        /// <param name="entityIds">The entity ids to get.</param>
        /// <returns>A collection of entities.</returns>
        public static HashSet<IEntity> TryGetEntities(
            this IEntityRepository repository,
            RequestContext context,
            IEnumerable<EntityId> entityIds)
        {
            return ParallelTryGetEntities(repository, context, entityIds.ToList());
        }

        /// <summary>Get all users in the system.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">Context information for the request (only EntityFilter is used).</param>
        /// <returns>A collection of user entities.</returns>
        public static HashSet<IEntity> GetAllUsers(
            this IEntityRepository repository, RequestContext context)
        {
            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.EntityCategoryFilter, UserEntity.CategoryName);
            var userEntityIdList = repository.GetFilteredEntityIds(filterContext).ToList();
            return ParallelTryGetEntities(repository, context, userEntityIdList);
        }

        /// <summary>Get all companies in the system.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">Context information for the request (only EntityFilter is used).</param>
        /// <returns>A collection of company entities.</returns>
        public static HashSet<IEntity> GetAllCompanies(
            this IEntityRepository repository, RequestContext context)
        {
            var filterContext = new RequestContext { EntityFilter = new RepositoryEntityFilter() };
            filterContext.EntityFilter.EntityQueries.QueryStringParams.Add(
                EntityFilterNames.EntityCategoryFilter, CompanyEntity.CategoryName);
            var companyEntityIdList = repository.GetFilteredEntityIds(filterContext).ToList();
            return ParallelTryGetEntities(repository, context, companyEntityIdList);
        }

        /// <summary>Checked Get of a single entity by ExternalEntityId</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The wrapped entity object or null if not found.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        public static IEntity TryGetEntity(
            this IEntityRepository repository, RequestContext context, EntityId entityId)
        {
            try
            {
                return repository.GetEntity(context, entityId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Checked Get of a single entity by ExternalEntityId</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id.</param>
        /// <typeparam name="T">The IEntity type to return.</typeparam>
        /// <returns>The wrapped entity object or null if not found or of incorrect type.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        public static T TryGetEntity<T>(
            this IEntityRepository repository,
            RequestContext context,
            EntityId entityId)
            where T : EntityWrapperBase, new()
        {
            try
            {
                return repository.GetEntity<T>(context, entityId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Get and entity by Id as the given IEntity type.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id.</param>
        /// <typeparam name="T">The IEntity wrapper type to return.</typeparam>
        /// <returns>The entity if successful.</returns>
        /// <exception cref="DataAccessTypeMismatchException">
        /// Thrown if the type param does not match the retrieved entity category.
        /// </exception>
        /// <exception cref="DataAccessEntityNotFoundException">
        /// Thrown if the entity cannot be found.
        /// </exception>
        public static T GetEntity<T>(
            this IEntityRepository repository, 
            RequestContext context, 
            EntityId entityId)
            where T : EntityWrapperBase, new()
        {
            // GetEntity should throw if it doesn't return a valid entity.
            var entity = repository.GetEntity(context, entityId);
            return entity.BuildWrappedEntity<T>();
        }

        /// <summary>Save a single entity.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to save.</param>
        /// <returns>True if successful.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        public static bool TrySaveEntity(
            this IEntityRepository repository,
            RequestContext context,
            IEntity entity)
        {
            try
            {
                repository.SaveEntity(context, entity);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Save a selected subset of entity properties and/or associations forcing retries
        /// and version advance if stale. This is not safe in the general case an the entity
        /// must be filtered to the subset of properties  and associations than can be safely
        /// be updated in this way. This method fails if an attempt is made to save 
        /// all properties or associations.
        /// </summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to update.</param>
        /// <param name="propertyNameFilters">property name filters or null</param>
        /// <param name="associationNameFilters">association name filters or null</param>
        /// <returns>True if successful.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        public static bool TryForceUpdateEntity(            
            this IEntityRepository repository, 
            RequestContext context, 
            IEntity entity,
            IEnumerable<string> propertyNameFilters,
            IEnumerable<string> associationNameFilters)
        {
            try
            {
                repository.ForceUpdateEntity(context, entity, propertyNameFilters, associationNameFilters);
                return true;
            }
            catch (Exception)
            {
            }

            // Fail if we didn't succeed with retries
            return false;
        }

        /// <summary>
        /// Save a selected subset of entity properties and/or associations forcing retries
        /// and version advance if stale. This is not safe in the general case an the entity
        /// must be filtered to the subset of properties  and associations than can be safely
        /// be updated in this way. This method fails if an attempt is made to save 
        /// all properties or associations.
        /// </summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to update.</param>
        /// <param name="propertyNameFilters">property name filters or null</param>
        /// <param name="associationNameFilters">association name filters or null</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        public static void ForceUpdateEntity(
            this IEntityRepository repository, 
            RequestContext context, 
            IEntity entity,
            IEnumerable<string> propertyNameFilters,
            IEnumerable<string> associationNameFilters)
        {
            var forceContext = BuildContextWithNameFilters(context, propertyNameFilters, associationNameFilters);
            repository.ForceUpdateEntity(forceContext, entity);
        }

        /// <summary>
        /// Save a selected subset of entity properties and/or associations forcing retries
        /// and version advance if stale. This is not safe in the general case an the entity
        /// must be filtered to the subset of properties  and associations than can be safely
        /// be updated in this way. This method fails if an attempt is made to save 
        /// all properties or associations.
        /// </summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to update.</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        public static void ForceUpdateEntity(this IEntityRepository repository, RequestContext context, IEntity entity)
        {
            var retryProvider = context.RetryProvider ?? new DefaultRetryProvider(5, 5000);
            repository.ForceUpdateEntity(context, entity, retryProvider);
        }

        /// <summary>Set an entity status to active or inactive.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="requestContext">Context object for the request.</param>
        /// <param name="entityId">The entity id.</param>
        /// <param name="active">True to set active, false for inactive.</param>
        public static void SetEntityStatus(
            this IEntityRepository repository, 
            RequestContext requestContext, 
            EntityId entityId, 
            bool active)
        {
            repository.SetEntityStatus(requestContext, new HashSet<EntityId> { entityId }, active);
        }

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">Context information for the request.</param>
        /// <param name="sourceEntityId">The external entity id of the source entity to which the target entities are being associated.</param>
        /// <param name="associationName">Association name.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <returns>The source entity whose assocations are being updated, with new associations populated.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        /// <exception cref="DataAccessStaleEntityException">On stale version.</exception>
        public static IEntity AssociateEntities(
            this IEntityRepository repository,
            RequestContext context, 
            EntityId sourceEntityId, 
            string associationName, 
            HashSet<IEntity> targetEntities)
        {
            return repository.AssociateEntities(
                context, sourceEntityId, associationName, string.Empty, targetEntities, AssociationType.Relationship, false);
        }

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">Context information for the request.</param>
        /// <param name="sourceEntityId">The external entity id of the source entity to which the target entities are being associated.</param>
        /// <param name="associationName">Association name.</param>
        /// <param name="associationDetails">Additional metadata associated with the association.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <returns>The source entity whose assocations are being updated, with new associations populated.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        /// <exception cref="DataAccessStaleEntityException">On stale version.</exception>
        public static IEntity AssociateEntities(
            this IEntityRepository repository,
            RequestContext context, 
            EntityId sourceEntityId, 
            string associationName, 
            string associationDetails, 
            HashSet<IEntity> targetEntities, 
            bool replaceIfPresent)
        {
            return repository.AssociateEntities(context, sourceEntityId, associationName, associationDetails, targetEntities, AssociationType.Relationship, replaceIfPresent);
        }

        /// <summary>Add a version filter to an entity filter.</summary>
        /// <param name="entityFilter">The IEntityFilter.</param>
        /// <param name="version">The version to add.</param>
        public static void AddVersionToEntityFilter(this IEntityFilter entityFilter, int version)
        {
            PropertyValue versionProperty = version;
            entityFilter.EntityQueries.QueryStringParams[EntityFilterNames.VersionFilter]
                = versionProperty.SerializationValue;
        }

        /// <summary>Add property name filters to an entity filter.</summary>
        /// <param name="entityFilter">The IEntityFilter.</param>
        /// <param name="propertyNames">The property names to add.</param>
        public static void AddPropertyNameFilter(this IEntityFilter entityFilter, IEnumerable<string> propertyNames)
        {
            entityFilter.EntityQueries.QueryStringParams[EntityFilterNames.PropertyNameFilter]
                = string.Join(",", propertyNames);
        }
        
        /// <summary>Get the property name filters on an entity filter.</summary>
        /// <param name="entityFilter">The IEntityFilter.</param>
        /// <returns>The collection of property names.</returns>
        public static IEnumerable<string> GetPropertyNameFilter(this IEntityFilter entityFilter)
        {
            var filters = entityFilter.EntityQueries.QueryStringParams;
            var propertiesFilters = new List<string>();
            if (filters.ContainsKey(EntityFilterNames.PropertyNameFilter))
            {
                propertiesFilters = filters[EntityFilterNames.PropertyNameFilter]
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim()).ToList();
            }

            return propertiesFilters;
        }

        /// <summary>Add association name filters to an entity filter.</summary>
        /// <param name="entityFilter">The IEntityFilter.</param>
        /// <param name="associationNames">The association names to add.</param>
        public static void AddAssociationNameFilter(this IEntityFilter entityFilter, IEnumerable<string> associationNames)
        {
            entityFilter.EntityQueries.QueryStringParams[EntityFilterNames.AssociationNameFilter]
                = string.Join(",", associationNames);
        }
        
        /// <summary>Get the association name filters on an entity filter.</summary>
        /// <param name="entityFilter">The IEntityFilter.</param>
        /// <returns>The collection of association names.</returns>
        public static IEnumerable<string> GetAssociationNameFilter(this IEntityFilter entityFilter)
        {
            var filters = entityFilter.EntityQueries.QueryStringParams;

            var associationsFilters = new List<string>();
            if (filters.ContainsKey(EntityFilterNames.AssociationNameFilter))
            {
                associationsFilters = filters[EntityFilterNames.AssociationNameFilter]
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim()).ToList();
            }

            return associationsFilters;
        }

        /// <summary>Build a repository request context with name filters.</summary>
        /// <param name="context">The request context to base it on.</param>
        /// <param name="propertyNames">The property names to add.</param>
        /// <param name="associationNames">The association names to add.</param>
        /// <returns>A new RequestContext instance with the entity filter.</returns>
        public static RequestContext BuildContextWithNameFilters(this RequestContext context, IEnumerable<string> propertyNames, IEnumerable<string> associationNames)
        {
            var propertyNameFilters = propertyNames ?? new List<string>();
            var associationNameFilters = associationNames ?? new List<string>();

            // If there are no property name filters don't include any properties. Likewise associations.
            var includeProperties = propertyNameFilters.Any();
            var includeAssociations = associationNameFilters.Any();
            var entityFilter = new RepositoryEntityFilter(includeProperties, includeProperties, includeProperties, includeAssociations);
            entityFilter.AddPropertyNameFilter(propertyNameFilters);
            entityFilter.AddAssociationNameFilter(associationNameFilters);
            var updatedContext = new RequestContext(context);
            updatedContext.EntityFilter = entityFilter;
            updatedContext.ForceOverwrite = false;
            return updatedContext;
        }

        /// <summary>
        /// Save a selected subset of entity properties and/or associations forcing retries
        /// and version advance if stale. This is not safe in the general case an the entity
        /// must be filtered to the subset of properties  and associations than can be safely
        /// be updated in this way. This method fails if an attempt is made to save 
        /// all properties or associations.
        /// </summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to update.</param>
        /// <param name="retryProvider">The retry provider instance.</param>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        internal static void ForceUpdateEntity(
            this IEntityRepository repository, RequestContext context, IEntity entity, IRetryProvider retryProvider)
        {
            if (context.EntityFilter == null)
            {
                throw new DataAccessException("ForceUpdateEntity requires EntityFilter.");
            }

            var associationNameFilters = context.EntityFilter.GetAssociationNameFilter();
            var propertyNameFilters = context.EntityFilter.GetPropertyNameFilter();

            // If property groups are allowed there must be property name filters
            // to narrow the properties to be updated.
            if (context.EntityFilter.Filters.Any() && !propertyNameFilters.Any())
            {
                throw new DataAccessException("ForceUpdateEntity property updates not allowed without name filters.");
            }

            // If associations are allowed there must be association name filters
            // to narrow the associations to be updated.
            if (context.EntityFilter.IncludeAssociations && !associationNameFilters.Any())
            {
                throw new DataAccessException("ForceUpdateEntity association updates not allowed without name filters.");
            }
            
            // We will try five times to get the latest version and merge to overcome
            // a collision
            var remainingRetryCount = retryProvider.MaxRetries;
            while (remainingRetryCount >= 0)
            {
                try
                {
                    // Update the incoming entity to the latest version for best chance of success.
                    // Don't ever do this outside the context of the DAL.
                    var currentVersion = repository.GetEntityVersion(entity.ExternalEntityId);
                    entity.LocalVersion = currentVersion;
                    repository.SaveEntity(context, entity);
                    return;
                }
                catch (DataAccessEntityNotFoundException e)
                {
                    retryProvider.RetryOrThrow(e, ref remainingRetryCount, false, false);
                }
                catch (DataAccessTypeMismatchException e)
                {
                    retryProvider.RetryOrThrow(e, ref remainingRetryCount, false, false);
                }
                catch (DataAccessStaleEntityException e)
                {
                    retryProvider.RetryOrThrow(e, ref remainingRetryCount, true, false);
                }
                catch (Exception e)
                {
                    retryProvider.RetryOrThrow(e, ref remainingRetryCount, true, true);
                }
            }
        }

        /// <summary>Parallel try get on a list of entity id's.</summary>
        /// <param name="repository">The IEntityRepository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="entityIdList">The list of entity id's.</param>
        /// <returns>The entities.</returns>
        private static HashSet<IEntity> ParallelTryGetEntities(
            IEntityRepository repository, 
            RequestContext context, 
            ICollection<EntityId> entityIdList)
        {
            var entities = new HashSet<IEntity>();
            Parallel.For(
                0,
                entityIdList.Count,
                i =>
                {
                    var entity = repository.TryGetEntity(context, entityIdList.ElementAt(i));
                    if (entity != null)
                    {
                        lock (entities)
                        {
                            entities.Add(entity);
                        }
                    }
                });

            return entities;
        }
    }
}
