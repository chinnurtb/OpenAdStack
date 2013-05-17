// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SimulatedEntityRepository.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConcreteDataStore;
using DataAccessLayer;

namespace SimulatedDataStore
{
    /// <summary>
    /// A memory-based repository for use by the simulator.
    /// </summary>
    public class SimulatedEntityRepository : IEntityRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedEntityRepository"/> class
        /// with a memory-only backing store.
        /// </summary>
        public SimulatedEntityRepository() : this(null, null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedEntityRepository"/> class
        /// with a memory cache, a local persistent store, and a read-only store.
        /// </summary>
        /// <param name="localIndexConnectionString">
        /// Index connection string to a local persistent repository instance. 
        /// Writes are not performed as a result of simulated IEntityRespository saves.
        /// Writes may be performed to synchronize with read-only store.
        /// </param>
        /// <param name="localEntityConnectionString">
        /// Entity connection string to a local persistent repository instance. 
        /// Writes are not performed as a result of simulated IEntityRespository saves.
        /// Writes may be performed to synchronize with read-only store.
        /// </param>
        /// <param name="readOnlyIndexConnectionString">
        /// Index connection string to a persistent repository instance.
        /// Writes are not performed for any reason.
        /// </param>
        /// <param name="readOnlyEntityConnectionString">
        /// Entity connection string to a persistent repository instance.
        /// Writes are not performed for any reason.
        /// </param>
        public SimulatedEntityRepository(
            string localIndexConnectionString, 
            string localEntityConnectionString,
            string readOnlyIndexConnectionString,
            string readOnlyEntityConnectionString)
            : this(localIndexConnectionString, localEntityConnectionString, readOnlyIndexConnectionString, readOnlyEntityConnectionString, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedEntityRepository"/> class
        /// with a memory cache, a local persistent store, and a read-only store.
        /// </summary>
        /// <param name="localIndexConnectionString">
        /// Index connection string to a local persistent repository instance. 
        /// Writes are not performed as a result of simulated IEntityRespository saves.
        /// Writes may be performed to synchronize with read-only store.
        /// </param>
        /// <param name="localEntityConnectionString">
        /// Entity connection string to a local persistent repository instance. 
        /// Writes are not performed as a result of simulated IEntityRespository saves.
        /// Writes may be performed to synchronize with read-only store.
        /// </param>
        /// <param name="readOnlyIndexConnectionString">
        /// Index connection string to a persistent repository instance.
        /// Writes are not performed for any reason.
        /// </param>
        /// <param name="readOnlyEntityConnectionString">
        /// Entity connection string to a persistent repository instance.
        /// Writes are not performed for any reason.
        /// </param>
        /// <param name="forceRefreshFromReadOnlyStore">
        /// True to force a refresh of the local store from readonly store.
        /// </param>
        public SimulatedEntityRepository(
            string localIndexConnectionString,
            string localEntityConnectionString,
            string readOnlyIndexConnectionString,
            string readOnlyEntityConnectionString,
            bool forceRefreshFromReadOnlyStore)
        {
            // Initialize memory cache
            this.Entities = new Dictionary<EntityId, IEntity>();
            this.EntitiesUpdated = new Dictionary<EntityId, DateTime>();

            // Initialize persistent repositories
            this.LocalRepository = BuildRepository(localIndexConnectionString, localEntityConnectionString);
            this.ReadOnlyRepository = BuildRepository(readOnlyIndexConnectionString, readOnlyEntityConnectionString);

            this.ForceRefreshFromReadOnlyStore = forceRefreshFromReadOnlyStore;
        }

        /// <summary>
        /// Gets the read-only respository.
        /// </summary>
        public IEntityRepository LocalRepository { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to force a refresh of the local store from readonly store.
        /// </summary>
        private bool ForceRefreshFromReadOnlyStore { get; set; }

        /// <summary>
        /// Gets or sets ReadOnlyRepository.
        /// </summary>
        private IEntityRepository ReadOnlyRepository { get; set; }

        /// <summary>
        /// Gets or sets Entities.
        /// </summary>
        private Dictionary<EntityId, IEntity> Entities { get; set; }

        /// <summary>
        /// Gets or sets when an entity was updated in the cache.
        /// </summary>
        private Dictionary<EntityId, DateTime> EntitiesUpdated { get; set; }

        /// <summary>Remove old entities from the memory cache</summary>
        /// <param name="cutoff">The time before which entities should be removed.</param>
        public void CleanOldMemory(DateTime cutoff)
        {
            var blobs = this.Entities
                .Where(kvp => (string)kvp.Value.EntityCategory == BlobEntity.BlobEntityCategory);

            var terminated = blobs
                .Where(kvp => this.EntitiesUpdated[kvp.Key] < cutoff)
                .Select(kvp => kvp.Key);

            this.Entities.Remove(kvp => terminated.Contains(kvp.Key));
        }

        /// <summary>
        /// Get xml describing the workflow needed to process a request.
        /// </summary>
        /// <param name="requestName">The request name.</param>
        /// <returns>
        /// An Xml representation of the request defintion.
        /// </returns>
        public RequestDefinition GetRequestDefinition(string requestName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a single entity by ExternalEntityId
        /// </summary>
        /// <param name="context">The request context.</param><param name="entityId">The entity id.</param>
        /// <returns>
        /// The wrapped entity object.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">If entity Id is not valid.</exception><exception cref="T:System.Exception">This will be replaced by a custom exception.</exception>
        public IEntity GetEntity(RequestContext context, EntityId entityId)
        {
            return this.SynchronizeMemoryEntity(context, entityId);
        }

        /// <summary>
        /// Get a set of entities by external identifier.
        /// </summary>
        /// <param name="context">Context information for the request.</param><param name="entityIds">The external ids of the entities to get.</param>
        /// <returns>
        /// The entity objects.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">If entity Id is not valid.</exception><exception cref="T:System.Exception">This will be replaced by a custom exception.</exception>
        public HashSet<IEntity> GetEntitiesById(RequestContext context, EntityId[] entityIds)
        {
            var entities = new HashSet<IEntity>();
            foreach (var externalEntityId in entityIds)
            {
                var entity = this.SynchronizeMemoryEntity(context, externalEntityId);
                entities.Add(entity);
            }

            return entities;
        }

        /// <summary>
        /// Save a single entity.
        /// </summary>
        /// <param name="context">The request context.</param><param name="entity">The entity to save.</param><exception cref="T:System.Exception">This will be replaced by a custom exception.</exception>
        public void SaveEntity(RequestContext context, IEntity entity)
        {
            this.AddToMemoryCache(entity);
        }

        /// <summary>
        /// Save a set of entities.
        /// </summary>
        /// <param name="context">Context information for the request.</param><param name="entities">The entities that will be saved.</param><exception cref="T:System.Exception">This will be replaced by a custom exception.</exception>
        public void SaveEntities(RequestContext context, HashSet<IEntity> entities)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update a single entity with a list of properties.
        /// </summary>
        /// <param name="context">The request context.</param><param name="entityId">The entity id to update.</param><param name="properties">The properties to add or update on the entity.</param>
        /// <returns>
        /// True if successful.
        /// </returns>
        public bool TryUpdateEntity(RequestContext context, EntityId entityId, IEnumerable<EntityProperty> properties)
        {
            var entity = this.TryGetEntity(context, entityId);
            if (entity == null)
            {
                return false;
            }

            foreach (var entityProperty in properties)
            {
                if (!entity.TrySetEntityProperty(entityProperty))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Add a set of existing target entities as associations of a single source entity.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="sourceEntityId">The external entity id of the source entity to which the target entities are being associated.</param><param name="associationName">Association name.</param><param name="associationDetails">Additional metadata associated with the association.</param><param name="targetEntities">The collection of target entities to be associated with the source entity.</param><param name="associationType">The AssociationType.</param>
        /// <param name="replaceIfPresent">
        /// If true an existing association of the same name will be replaced. 
        /// If false, and the association already exists, a collection of associations would result.
        /// </param>
        /// <returns>The source entity whose assocations are being updated, with new associations populated.</returns>
        /// <exception cref="T:System.Exception">This will be replaced by a custom exception.</exception>
        public IEntity AssociateEntities(RequestContext context, EntityId sourceEntityId, string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update or Insert a new User entity.
        /// </summary>
        /// <param name="context">Context information for the request.</param><param name="userEntity">The user entity to update or insert.</param>
        public void SaveUser(RequestContext context, UserEntity userEntity)
        {
            this.SaveEntity(context, userEntity);
        }

        /// <summary>
        /// Get a user Entity by User Id.
        /// </summary>
        /// <param name="context">Context information for the request.</param><param name="userId">The User Id.</param>
        /// <returns>
        /// The User entity.
        /// </returns>
        public UserEntity GetUser(RequestContext context, string userId)
        {
            var entityId = this.Entities.Values
                .OfType<UserEntity>()
                .Where(user => user.UserId.ToString() == userId)
                .Select(user => user.ExternalEntityId)
                .FirstOrDefault();
            if (entityId == null)
            {
                throw new DataAccessEntityNotFoundException();
            }

            return this.GetEntity<UserEntity>(context, entityId);
        }

        /// <summary>
        /// Add a new company to the system.
        /// </summary>
        /// <param name="context">Context information for the request.</param><param name="companyEntity">The company entity to add.</param>
        public void AddCompany(RequestContext context, CompanyEntity companyEntity)
        {
            this.SaveEntity(context, companyEntity);
        }

        /// <summary>
        /// Get a subset of entity information for all active entities of a given category.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <returns>
        /// A List of IRawEntity objects.
        /// </returns>
        public IEnumerable<EntityId> GetFilteredEntityIds(RequestContext context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set an entity status to active or inactive.
        /// </summary>
        /// <param name="requestContext">Context object for the request.</param><param name="entityIds">The entity ids.</param><param name="active">True to set active, false for inactive.</param>
        public void SetEntityStatus(RequestContext requestContext, HashSet<EntityId> entityIds, bool active)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Build a new ConcreteEntityRepository from connection strings.
        /// </summary>
        /// <param name="indexConnectionString">
        /// Index connection string to a persistent repository instance from which to pull cache misses. 
        /// Writes should not be performed on this repository.
        /// </param>
        /// <param name="indexEntityConnectionString">
        /// Entity connection string to a persistent repository instance from which to pull cache misses. 
        /// Writes should not be performed on this repository.
        /// </param>
        /// <returns>Repository object.</returns>
        private static IEntityRepository BuildRepository(string indexConnectionString, string indexEntityConnectionString)
        {
            if (indexConnectionString == null || indexEntityConnectionString == null)
            {
                return null;
            }

            var indexStoreFactory = new SqlIndexStoreFactory(indexConnectionString);
            var entityStoreFactory = new AzureEntityStoreFactory(indexEntityConnectionString);
            var storageKeyFactory = new AzureStorageKeyFactory(indexStoreFactory, new KeyRuleFactory());
            var blobStoreFactory = new AzureBlobStoreFactory(indexEntityConnectionString);
            var concreteRepository = new ConcreteEntityRepository(
                indexStoreFactory, entityStoreFactory, storageKeyFactory, blobStoreFactory);

            return concreteRepository;
        }

        /// <summary>Synchronize the local store with the read-only store.</summary>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity Id to syncrhonize.</param>
        /// <returns>The entity.</returns>
        private IEntity SynchronizeLocalEntity(RequestContext context, EntityId entityId)
        {
            // If we don't have persistent stores we don't have the entity
            if (this.LocalRepository == null || this.ReadOnlyRepository == null)
            {
                return null;
            }

            // Try to get it from local store
            var localEntity = this.LocalRepository.TryGetEntity(context, entityId);
            if (localEntity != null)
            {
                Console.WriteLine("Read Local Success: {0}, {1}".FormatInvariant(localEntity.ExternalName, localEntity.ExternalEntityId));
            }

            // If not found try to get it from the read-only store
            IEntity readOnlyEntity = null;
            if (localEntity == null)
            {
                readOnlyEntity = this.ReadOnlyRepository.TryGetEntity(context, entityId);
            }

            // Early exit if we don't have either one.
            if (localEntity == null && readOnlyEntity == null)
            {
                Console.WriteLine("Not found local or read-only repository: {0}".FormatInvariant(entityId));
                return null;
            }

            var category = localEntity != null
                ? (string)localEntity.EntityCategory
                : (string)readOnlyEntity.EntityCategory;

            if (category == CompanyEntity.CompanyEntityCategory)
            {
                // We don't refresh companies from readonly if they already exist locally
                if (localEntity != null)
                {
                    return localEntity;
                }

                // Add company needs to be handled differently
                var companyEntity = (CompanyEntity)readOnlyEntity;
                this.LocalRepository.AddCompany(new RequestContext(), companyEntity);
                return readOnlyEntity;
            }

            if (category == BlobEntity.BlobEntityCategory)
            {
                // We don't refresh blob entities from readonly if they already exist locally
                if (localEntity != null)
                {
                    return localEntity;
                }

                this.LocalRepository.SaveEntity(context, readOnlyEntity);
                return readOnlyEntity;
            }

            // If we refreshing from read-only and localEntity exists, need to do a version fix-up
            // or the update will fail.
            if (localEntity != null && this.ForceRefreshFromReadOnlyStore)
            {
                readOnlyEntity = this.ReadOnlyRepository.TryGetEntity(context, entityId);
                readOnlyEntity.LocalVersion = localEntity.LocalVersion;
            }

            this.LocalRepository.SaveEntity(context, readOnlyEntity);
            localEntity = this.LocalRepository.GetEntity(context, entityId);
            Console.WriteLine("Sync Local Succeeded: {0}, {1}".FormatInvariant(readOnlyEntity.ExternalName, readOnlyEntity.ExternalEntityId));

            // We return the readonly entity because at this point the associations on
            // the local version might have been stripped because the targets aren't there yet.
            readOnlyEntity.LocalVersion = localEntity.LocalVersion;
            return readOnlyEntity;
        }

        /// <summary>Synchronize the memory cache with the local persistent store if required.</summary>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity Id to syncrhonize.</param>
        /// <returns>The entity.</returns>
        private IEntity SynchronizeMemoryEntity(RequestContext context, EntityId entityId)
        {
            // See if the entity is in the memory cache
            if (this.Entities.ContainsKey(entityId))
            {
                return this.Entities[entityId];
            }

            // Try to get it from the local store
            var entity = this.SynchronizeLocalEntity(context, entityId);

            // Update memory cache
            if (entity != null)
            {
                this.AddToMemoryCache(entity);
            }

            return entity;
        }

        /// <summary>
        /// Add an entity to the memory cache
        /// </summary>
        /// <param name="entity">The entity.</param>
        private void AddToMemoryCache(IEntity entity)
        {
            this.Entities[entity.ExternalEntityId] = entity;
            this.EntitiesUpdated[entity.ExternalEntityId] = DateTime.UtcNow;
        }
    }
}
