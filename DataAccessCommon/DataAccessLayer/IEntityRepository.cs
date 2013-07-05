// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IEntityRepository.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>
    /// Interface definition for a basic Entity DAL.
    /// </summary>
    public interface IEntityRepository
    {
        /// <summary>
        /// Get xml describing the workflow needed to process a request.
        /// </summary>
        /// <param name="requestName">The request name.</param>
        /// <returns>An Xml representation of the request defintion.</returns>
        RequestDefinition GetRequestDefinition(string requestName);

        /// <summary>Get a single entity by ExternalEntityId</summary>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The wrapped entity object.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        IEntity GetEntity(RequestContext context, EntityId entityId);

        /// <summary>Get a set of entities by external identifier.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="entityIds">The external ids of the entities to get.</param>
        /// <returns>The entity objects.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        HashSet<IEntity> GetEntitiesById(RequestContext context, EntityId[] entityIds);

        /// <summary> Get the current version of an entity.</summary>
        /// <param name="entityId">The external id of the entity.</param>
        /// <returns>The version.</returns>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        int GetEntityVersion(EntityId entityId);

        /// <summary>Save a single entity.</summary>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to save.</param>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessStaleEntityException">On stale version.</exception>
        void SaveEntity(RequestContext context, IEntity entity);

        /// <summary>Save a set of entities.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="entities">The entities that will be saved.</param>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessStaleEntityException">On stale version.</exception>
        void SaveEntities(RequestContext context, HashSet<IEntity> entities);

        /// <summary>
        /// Obsolete: Use IEntityRepository extension method ForceUpdateEntity.
        /// Update a single entity with a list of properties.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id to update.</param>
        /// <param name="properties">The properties to add or update on the entity.</param>
        /// <returns>True if successful.</returns>
        bool TryUpdateEntity(RequestContext context, EntityId entityId, IEnumerable<EntityProperty> properties);

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="sourceEntityId">The external entity id of the source entity to which the target entities are being associated.</param>
        /// <param name="associationName">Association name.</param>
        /// <param name="associationDetails">Additional metadata associated with the association.</param>
        /// <param name="targetEntities">The collection of target entities to be associated with the source entity.</param>
        /// <param name="associationType">The AssociationType.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <returns>The source entity whose assocations are being updated, with new associations populated.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        /// <exception cref="DataAccessStaleEntityException">On stale version.</exception>
        IEntity AssociateEntities(RequestContext context, EntityId sourceEntityId, string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent);

        /// <summary>Update or Insert a new User entity.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="userEntity">The user entity to update or insert.</param>
        /// <exception cref="DataAccessException">If you try to save an entity with a new entity Id but existing UserId.</exception>
        [SuppressMessage("Microsoft.Design", "CA1045", Justification = "Avoid copying.")]
        void SaveUser(RequestContext context, UserEntity userEntity);

        /// <summary>Get a user Entity by User Id.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="userId">The User Id.</param>
        /// <returns>The User entity.</returns>
        UserEntity GetUser(RequestContext context, string userId);

        /// <summary>Add a new company to the system.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="companyEntity">The company entity to add.</param>
        /// <exception cref="DataAccessException">On failure.</exception>
        [SuppressMessage("Microsoft.Design", "CA1045", Justification = "Avoid copying.")]
        void AddCompany(RequestContext context, CompanyEntity companyEntity);

        /// <summary>Get a subset of entity information for all active entities of a given category.</summary>
        /// <param name="context">The request context.</param>
        /// <returns>A List of IRawEntity objects.</returns>
        IEnumerable<EntityId> GetFilteredEntityIds(RequestContext context);

        /// <summary>Set an entity status to active or inactive.</summary>
        /// <param name="requestContext">Context object for the request.</param>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="active">True to set active, false for inactive.</param>
        void SetEntityStatus(RequestContext requestContext, HashSet<EntityId> entityIds, bool active);
    }
}
