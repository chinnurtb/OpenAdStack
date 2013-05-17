// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    /// <summary>Extension method for IEntityRepository.</summary>
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
                EntityFilterNames.EntityCategoryFilter, UserEntity.UserEntityCategory);
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
                EntityFilterNames.EntityCategoryFilter, CompanyEntity.CompanyEntityCategory);
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
            where T : class, IEntity
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
        /// <typeparam name="T">The IEntity type to return.</typeparam>
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
            where T : class, IEntity
        {
            // GetEntity should throw if it doesn't return a valid entity.
            var entity = repository.GetEntity(context, entityId) as T;
            if (entity != null)
            {
                return entity;
            }

            // If the entity is not of the type we expected, throw
            throw new DataAccessTypeMismatchException("Retrieved entity: {0} does not match requested type: {1}".FormatInvariant(entityId, typeof(T).ToString()));
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
