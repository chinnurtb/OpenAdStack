// -----------------------------------------------------------------------
// <copyright file="ConcreteEntityRepository.cs" company="Rare Crowds Inc">
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
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using DataAccessLayer;
using Diagnostics;

namespace ConcreteDataStore
{
    /// <summary>
    /// Concrete implementation of the external facing data access layer interface 
    /// used by upstream workflows.
    /// </summary>
    internal class ConcreteEntityRepository : IEntityRepository
    {
        /// <summary>Default storage account</summary>
        internal const string DefaultStorageAccount = "DefaultAzureStorageAccount";

        /// <summary>Default Company Id</summary>
        internal static readonly EntityId DefaultCompanyId = 1;

        /// <summary>Initializes a new instance of the <see cref="ConcreteEntityRepository"/> class.</summary>
        /// <param name="indexStoreFactory">The index store factory.</param>
        /// <param name="entityStoreFactory">The entity store factory.</param>
        /// <param name="storageKeyFactory">The storage key factory.</param>
        /// <param name="blobDictionaryFactory">The blob dictionary factory.</param>
        public ConcreteEntityRepository(IIndexStoreFactory indexStoreFactory, IEntityStoreFactory entityStoreFactory, IStorageKeyFactory storageKeyFactory, IBlobStoreFactory blobDictionaryFactory)
        {
            this.IndexStoreFactory = indexStoreFactory;
            this.EntityStoreFactory = entityStoreFactory;
            this.StorageKeyFactory = storageKeyFactory;
            this.BlobStoreFactory = blobDictionaryFactory;
        }

        /// <summary>Gets or sets IndexStoreFactory.</summary>
        public IIndexStoreFactory IndexStoreFactory { get; set; }

        /// <summary>Gets or sets EntityStoreFactory.</summary>
        public IEntityStoreFactory EntityStoreFactory { get; set; }

        /// <summary>Gets or sets StorageKeyFactory.</summary>
        public IStorageKeyFactory StorageKeyFactory { get; set; }

        /// <summary>Gets or sets BlobStoreFactory.</summary>
        public IBlobStoreFactory BlobStoreFactory { get; set; }

        /// <summary>
        /// Get xml describing the workflow needed to process a request.
        /// </summary>
        /// <param name="requestName">The request name.</param>
        /// <returns>An Xml representation of the request defintion.</returns>
        public RequestDefinition GetRequestDefinition(string requestName)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>Get a subset of entity information for all active entities of a given category.</summary>
        /// <param name="context">The request context.</param>
        /// <returns>A List of IRawEntity objects.</returns>
        public IEnumerable<EntityId> GetFilteredEntityIds(RequestContext context)
        {
            var filters = context.EntityFilter.EntityQueries.QueryStringParams;

            // EntityCategory is required
            if (!filters.ContainsKey(EntityFilterNames.EntityCategoryFilter))
            {
                throw new DataAccessException("EntityCategory must be specified for GetFilteredEntityIds.");
            }

            var entityCategory = filters[EntityFilterNames.EntityCategoryFilter];

            var entityList = this.IndexStoreFactory.GetIndexStore()
                .GetEntityInfoByCategory(entityCategory).ToList();

            // External type is optional
            if (!filters.ContainsKey(EntityFilterNames.ExternalTypeFilter))
            {
                return entityList.Select(e => (EntityId)e.ExternalEntityId).ToList();
            }

            var externalType = filters[EntityFilterNames.ExternalTypeFilter];
            return entityList.Where(e => (string)e.ExternalType == externalType)
                .Select(e => (EntityId)e.ExternalEntityId).ToList();
        }

        /// <summary>Get a single entity by ExternalEntityId</summary>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id.</param>
        /// <returns>The wrapped entity object.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        public IEntity GetEntity(RequestContext context, EntityId entityId)
        {
            return this.GetEntitiesById(context, new[] { entityId }).Single();
        }

        /// <summary> Get a set of entities by external identifier.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="entityIds">The external ids of the entities to get.</param>
        /// <returns>The wrapped entity objects.</returns>
        /// <exception cref="DataAccessException">On failure.</exception>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        public HashSet<IEntity> GetEntitiesById(RequestContext context, EntityId[] entityIds)
        {
            var requestContext = new RequestContext(context);
            if (requestContext.EntityFilter == null)
            {
                requestContext.EntityFilter = new RepositoryEntityFilter(true, true, true, true);
            }

            var version = GetVersionFromFilter(requestContext.EntityFilter);

            var entities = new HashSet<IEntity>();
            foreach (var externalEntityId in entityIds)
            {
                var indexEntity = this.IndexStoreFactory.GetIndexStore().GetEntity(externalEntityId, DefaultStorageAccount, version);
                if (indexEntity == null)
                {
                    // Default behavior will be to throw. If we need a more generous bulk get that should be a separate interface method
                    // where me make it explicit to the caller that they are responsible for doing the right thing in this scenario.
                    var message = "Index entry not found for external entity id: {0}".FormatInvariant(externalEntityId.ToString());
                    throw new DataAccessEntityNotFoundException(message);
                }

                var entity = this.GetSingleEntity(requestContext, indexEntity, version);

                if (entity == null)
                {
                    // Because we found the key but couldn't load the entity, this should be treated as a more serious failure
                    var message = string.Format(
                        CultureInfo.InvariantCulture, "Entity could not be retrieved for external entity id: {0}", externalEntityId);
                    LogManager.Log(LogLevels.Error, true, message);
                    throw new DataAccessEntityNotFoundException(message);
                }

                entities.Add(entity);
            }

            return entities;
        }

        /// <summary> Get the current version of an entity.</summary>
        /// <param name="entityId">The external id of the entity.</param>
        /// <returns>The version.</returns>
        /// <exception cref="DataAccessEntityNotFoundException">If not found.</exception>
        public int GetEntityVersion(EntityId entityId)
        {
            var indexEntity = this.IndexStoreFactory.GetIndexStore().GetEntity(entityId, DefaultStorageAccount, null);
            if (indexEntity == null)
            {
                var message = "Index entry not found for external entity id: {0}".FormatInvariant(entityId.ToString());
                throw new DataAccessEntityNotFoundException(message);
            }

            return indexEntity.LocalVersion;
        }

        /// <summary>Save a single entity.</summary>
        /// <param name="context">The request context.</param>
        /// <param name="entity">The entity to save.</param>
        /// <exception cref="DataAccessException">On failure.</exception>
        public void SaveEntity(RequestContext context, IEntity entity)
        {
            var entities = new HashSet<IEntity> { entity };
            this.SaveEntities(context, entities);            
        }

        /// <summary>Save a set of entities.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="entities">The entities that will be saved.</param>
        /// <exception cref="DataAccessException">On failure.</exception>
        public void SaveEntities(RequestContext context, HashSet<IEntity> entities)
        {
            var requestContext = new RequestContext(context);

            // TODO: Investigate partial success scenarios
            foreach (var rawEntity in entities.Select(e => e.SafeUnwrapEntity()))
            {
                // See if the key already exists
                var key = this.IndexStoreFactory.GetIndexStore().GetStorageKey(rawEntity.ExternalEntityId, DefaultStorageAccount);
                var isUpdate = key != null;

                // Build the new key or updated key
                var updatedKey = this.GetNewKey(rawEntity, requestContext, key);

                this.SaveSingleEntity(requestContext, rawEntity, updatedKey, isUpdate);
            }
        }

        /// <summary>
        /// Obsolete: Use IEntityRepository extension method ForceUpdateEntity.
        /// Update a single entity with a list of properties.
        /// </summary>
        /// <param name="context">The request context.</param>
        /// <param name="entityId">The entity id to update.</param>
        /// <param name="properties">The properties to add or update on the entity.</param>
        /// <returns>True if successful.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern implementation.")]
        [Obsolete("Use IEntityRepository extension method ForceUpdateEntity")]
        public bool TryUpdateEntity(RequestContext context, EntityId entityId, IEnumerable<EntityProperty> properties)
        {
            IEntity mergeEntity;
            try
            {
                mergeEntity = this.GetMergeEntity(false, entityId, true);
            }
            catch (DataAccessEntityNotFoundException)
            {
                return false;
            }

            var propertiesToUpdate = properties.ToList();
            foreach (var entityProperty in propertiesToUpdate)
            {
                mergeEntity.SetEntityProperty(entityProperty);
            }

            return this.TryForceUpdateEntity(context, mergeEntity, propertiesToUpdate.Select(p => p.Name), null);
        }

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
        public IEntity AssociateEntities(RequestContext context, EntityId sourceEntityId, string associationName, string associationDetails, HashSet<IEntity> targetEntities, AssociationType associationType, bool replaceIfPresent)
        {
            // Only save associations
            var requestContext = new RequestContext(context);
            requestContext.EntityFilter = new RepositoryEntityFilter(false, false, false, true);

            var entity = this.GetEntitiesById(requestContext, new[] { sourceEntityId }).Single();
            entity.AssociateEntities(associationName, associationDetails, targetEntities, associationType, replaceIfPresent);
            var entities = new HashSet<IEntity> { entity };
            this.SaveEntities(requestContext, entities);
            return entity;
        }

        /// <summary>Update or Insert a new User entity.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="userEntity">The user entity to update or insert.</param>
        /// <exception cref="DataAccessException">If you try to save an entity with a new entity Id but existing UserId.</exception>
        public void SaveUser(RequestContext context, UserEntity userEntity)
        {
            // Check if are trying to create a user with the same user id but different entity id
            if (this.CheckForDuplicateUserId(context, userEntity.UserId, userEntity.ExternalEntityId))
            {
                var message = string.Format(
                    CultureInfo.InvariantCulture, "A user with the same UserId already exists: {0}", userEntity.UserId);
                throw new DataAccessException(message);
            }

            // Adjust the request context so we create on the Default Company for users
            // TODO: Need to investigate this approach further and determine what other 'DefaultCompany' scenarios exist
            var addUserContext = new RequestContext(context) { ExternalCompanyId = DefaultCompanyId };

            var entities = new HashSet<IEntity> { userEntity };
            
            this.SaveEntities(addUserContext, entities);
        }

        /// <summary>Get a user Entity by User Id.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="userId">The User Id.</param>
        /// <exception cref="ArgumentException">If userId not found.</exception>
        /// <returns>The user entity.</returns>
        public UserEntity GetUser(RequestContext context, string userId)
        {
            var entityStore = this.EntityStoreFactory.GetEntityStore();
            var defaultCompanyKey = this.IndexStoreFactory.GetIndexStore().GetStorageKey(
                DefaultCompanyId, DefaultStorageAccount);
            var users = entityStore.GetUserEntitiesByUserId(userId, defaultCompanyKey);

            // TODO: GetUser (by UserId) does not currently support realizing 'heavy' properties.
            // If it ever does the filtering should be done prior to the work
            // avoid the hit on something that may be discarded by the filter.
            FilterProperties(users, context);

            // There should only be one valid current user entity
            var validCurrentUserEntity = this.GetValidCurrentEntities(users).SingleOrDefault();

            if (validCurrentUserEntity == null)
            {
                var message = string.Format(CultureInfo.InvariantCulture, "UserId not found: {0}", userId);
                throw new ArgumentException(message, "userId");
            }

            return new UserEntity(validCurrentUserEntity);
        }

        /// <summary>Add a new company to the system.</summary>
        /// <param name="context">Context information for the request.</param>
        /// <param name="companyEntity">The company entity to add.</param>
        public void AddCompany(RequestContext context, CompanyEntity companyEntity)
        {
            var entityStore = this.EntityStoreFactory.GetEntityStore();
            var rawIncomingEntity = companyEntity.SafeUnwrapEntity();

            // Perform new company setup specific to the storage type
            var partialKey = entityStore.SetupNewCompany(companyEntity.ExternalName);
            if (partialKey == null)
            {
                throw new DataAccessException("Company could not be added: {0}".FormatInvariant(companyEntity.ExternalName));
            }

            // Create company key - these will be a little different because we partially populate the entity key
            // with table name. It would not be available in the index yet at this point.
            rawIncomingEntity.Key = partialKey;
            var newKey = this.StorageKeyFactory.BuildNewStorageKey(
                DefaultStorageAccount, rawIncomingEntity.ExternalEntityId, rawIncomingEntity);
            
            this.SaveSingleEntity(context, rawIncomingEntity, newKey, false);
        }

        /// <summary>Set an entity status to active or inactive.</summary>
        /// <param name="requestContext">Context object for the request.</param>
        /// <param name="entityIds">The entity ids.</param>
        /// <param name="active">True to set active, false for inactive.</param>
        public void SetEntityStatus(RequestContext requestContext, HashSet<EntityId> entityIds, bool active)
        {
            this.IndexStoreFactory.GetIndexStore().SetEntityStatus(entityIds, active);
        }

        /// <summary>Create a blob-backed reference to the property data if it is large.</summary>
        /// <param name="requestContext">Context information for the request.</param>
        /// <param name="entityProperty">The entity property.</param>
        /// <param name="blobStore">The blob store.</param>
        /// <returns>The a new blob reference property, or the original property if not virtualized.</returns>
        internal static EntityProperty VirtualizeProperty(RequestContext requestContext, EntityProperty entityProperty, IBlobStore blobStore)
        {
            // If this is already marked as a blob ref validate it
            if (entityProperty.IsBlobRef)
            {
                if (TryBuildBlobReference(entityProperty, blobStore) != null)
                {
                    // Already virtualized - nothing to do
                    return entityProperty;
                }

                // May be a modified property - don't assume it will be virtualized.
                entityProperty.IsBlobRef = false;
            }

            if (entityProperty.Value.DynamicType != PropertyType.String && entityProperty.Value.DynamicType != PropertyType.Binary)
            {
                return entityProperty;
            }

            byte[] bytes;

            if (entityProperty.Value.DynamicType == PropertyType.String)
            {
                bytes = System.Text.Encoding.Unicode.GetBytes(entityProperty.Value.SerializationValue);
            }
            else
            {
                bytes = entityProperty.Value;
            }

            if (bytes.Length < 5000)
            {
                return entityProperty;
            }

            var company = "ENTITYBACKING";
            if (requestContext != null && requestContext.ExternalCompanyId != null)
            {
                company = requestContext.ExternalCompanyId.ToString();
            }

            var blobEntity = new BlobPropertyEntity(new EntityId(), bytes, entityProperty.Value.SerializationType);

            var blobKeyFactory = blobStore.GetStorageKeyFactory();
            blobEntity.Key = blobKeyFactory.BuildNewStorageKey(DefaultStorageAccount, null, blobEntity);
            blobStore.SaveBlob(blobEntity, company);

            // Make sure flags get copied, set value to the blob reference and set blobref flag
            var virtualizedProperty = new EntityProperty(entityProperty);
            virtualizedProperty.Value = blobKeyFactory.SerializeBlobKey(blobEntity.Key);
            virtualizedProperty.IsBlobRef = true;
            return virtualizedProperty;
        }

        /// <summary>Extract property data from a blob-backed reference.</summary>
        /// <param name="requestContext">Context information for the request.</param>
        /// <param name="entityProperty">A potential blob reference property.</param>
        /// <param name="blobStore">The blob store.</param>
        /// <returns>A realized property or the unmodified property if it was not virtualized.</returns>
        internal static EntityProperty RealizeProperty(RequestContext requestContext, EntityProperty entityProperty, IBlobStore blobStore)
        {
            if (!entityProperty.IsBlobRef)
            {
                return entityProperty;
            }

            if (requestContext.ReturnBlobReferences)
            {
                return entityProperty;
            }

            // Determine if we have a valid blob reference
            var key = TryBuildBlobReference(entityProperty, blobStore);
            if (key == null)
            {
                // Optimistically treat this as if it isn't a blob ref.
                entityProperty.IsBlobRef = false;
                return entityProperty;
            }

            var blobEntity = (BlobPropertyEntity)blobStore.GetBlobByKey(key);
            var blobPropertyType = blobEntity.BlobPropertyType.Value;
            var blobPropertyValue = blobEntity.BlobBytes.Value;

            if (blobPropertyType == new PropertyValue(PropertyType.String, "dummy").SerializationType)
            {
                var blobString = System.Text.Encoding.Unicode.GetString(blobEntity.BlobBytes);
                blobPropertyValue = new PropertyValue(PropertyType.String, blobString);
            }

            // Make sure metadata flags are transferred and set realized property value
            // IsBlobRef should be false on the realized property
            var realizedProperty = new EntityProperty(entityProperty);
            realizedProperty.Value = blobPropertyValue;
            realizedProperty.IsBlobRef = false;

            return realizedProperty;
        }

        /// <summary>Virtualize added/updated properties as needed</summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="entity">The entity to virtualize.</param>
        internal void VirtualizeEntity(RequestContext requestContext, IEntity entity)
        {
            var blobStore = this.BlobStoreFactory.GetBlobStore();
            for (var propertyIndex = 0; propertyIndex < entity.Properties.Count; propertyIndex++)
            {
                var virtualizedProperty = VirtualizeProperty(
                    requestContext, entity.Properties[propertyIndex], blobStore);
                entity.Properties[propertyIndex] = virtualizedProperty;
            }
        }

        /// <summary>Merge two entities based based on optional entity filters to produce the target 'saveable' entity.</summary>
        /// <param name="requestContext">Repository request context for the request.</param>
        /// <param name="newKey">The key to use for the saved entity.</param>
        /// <param name="isUpdate">True if the entity is being updated rather than created.</param>
        /// <param name="targetMergeEntity">The target entity of the merge.</param>
        /// <param name="incomingRawEntity">Entity with updates (or new).</param>
        internal void MergeEntities(
            RequestContext requestContext,
            IStorageKey newKey,
            bool isUpdate,
            IEntity targetMergeEntity,
            IEntity incomingRawEntity)
        {
            // Merge the IEntity level properties to the target entity
            this.MergeInterfaceProperties(targetMergeEntity, incomingRawEntity, isUpdate);

            // Update version, timestamp, etc
            this.UpdateReadOnlyProperties(requestContext, targetMergeEntity, newKey, isUpdate);

            // Merge the non-Interface properites and associations based on the filter
            this.MergeWithEntityFilter(
                targetMergeEntity, incomingRawEntity, requestContext.EntityFilter, requestContext.ForceOverwrite);
        }

        /// <summary>Filter the properties according according to the IEntityFilter on the context.</summary>
        /// <param name="entities">The entities to filter.</param>
        /// <param name="requestContext">The RequestContext with the filter.</param>
        private static void FilterProperties(IEnumerable<IEntity> entities, RequestContext requestContext)
        {
            // If we don't have an entity filter, nothing to do.
            if (requestContext.EntityFilter == null || entities == null)
            {
                return;
            }

            // Don't include properties that are not specified in the filter
            foreach (var entity in entities)
            {
                var includedProperties = entity.Properties
                    .Where(p => requestContext.EntityFilter.ContainsFilter(p.Filter)).ToList();
                entity.Properties.Clear();
                entity.Properties.Add(includedProperties);
            }
        }

        /// <summary>Determine if a blob reference is valid and return a valid key.</summary>
        /// <param name="entityProperty">The property to dereference.</param>
        /// <param name="blobStore">The blob store.</param>
        /// <returns>A storage key or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private static IStorageKey TryBuildBlobReference(EntityProperty entityProperty, IBlobStore blobStore)
        {
            try
            {
                var key = blobStore.GetStorageKeyFactory().DeserializeKey(entityProperty.Value);
                return key;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Extract the version to get from the entity filter.</summary>
        /// <param name="entityFilter">The entity filter.</param>
        /// <returns>The version requested or a null value for current version.</returns>
        private static int? GetVersionFromFilter(IEntityFilter entityFilter)
        {
            if (!entityFilter.EntityQueries.QueryStringParams.ContainsKey(EntityFilterNames.VersionFilter))
            {
                return null;
            }

            var versionSer = entityFilter.EntityQueries.QueryStringParams[EntityFilterNames.VersionFilter];
            return new PropertyValue(PropertyType.Int32, versionSer).DynamicValue;
        }

        /// <summary>Get a single entity.</summary>
        /// <param name="requestContext">Context information for the request.</param>
        /// <param name="indexEntity">The index entity.</param>
        /// <param name="version">Version if not current.</param>
        /// <returns>The entity or null.</returns>
        private IEntity GetSingleEntity(RequestContext requestContext, IEntity indexEntity, int? version)
        {
            var entityStore = this.EntityStoreFactory.GetEntityStore();
            var rawEntity = entityStore.GetEntityByKey(requestContext, indexEntity.Key);

            if (rawEntity == null)
            {
                return null;
            }

            if (!requestContext.EntityFilter.IncludeAssociations)
            {
                rawEntity.Associations.Clear();
            }
            else
            {
                // If we are getting the current version, clear the associations from the
                // table store entity and replace them with the cached associations from the index.
                if (version == null)
                {
                    rawEntity.Associations.Clear();
                    foreach (var association in indexEntity.Associations)
                    {
                        rawEntity.Associations.Add(association);
                    }
                }
            }

            // Apply property filter before realizing 'heavy' properties to avoid unecessary work
            FilterProperties(new HashSet<IEntity> { rawEntity }, requestContext);

            var blobStore = this.BlobStoreFactory.GetBlobStore();
            for (var propertyIndex = 0; propertyIndex < rawEntity.Properties.Count; propertyIndex++)
            {
                var realizedProperty = RealizeProperty(requestContext, rawEntity.Properties[propertyIndex], blobStore);
                rawEntity.Properties[propertyIndex] = realizedProperty;
            }

            // This provides category specific validation and allows callers
            // expecting a specific entity category to test against the expected entity wrapper type.
            return rawEntity.BuildWrappedEntity();
        }

        /// <summary>Save a single entity.</summary>
        /// <param name="requestContext">Context information for the request.</param>
        /// <param name="entity">The entity to save.</param>
        /// <param name="key">The key to use for the saved entity.</param>
        /// <param name="isUpdate">True if the entity is being updated rather than created.</param>
        private void SaveSingleEntity(
            RequestContext requestContext, 
            IEntity entity,
            IStorageKey key,
            bool isUpdate)
        {
            var entityStore = this.EntityStoreFactory.GetEntityStore();
            var indexStore = this.IndexStoreFactory.GetIndexStore();
            var incomingRawEntity = entity.SafeUnwrapEntity();

            // We do not directly support saving BlobPropertyEntity at the IEntityRepository level
            if ((string)incomingRawEntity.EntityCategory == BlobPropertyEntity.CategoryName)
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Directly saving BlobPropertyEntity is not permitted through the interface: {0}",
                    entity.ExternalEntityId);
                LogManager.Log(LogLevels.Error, true, msg);
                throw new DataAccessException(msg);
            }

            // Get a merge entity from an existing entity or new instance
            var entityToSave = this.GetMergeEntity(requestContext.ForceOverwrite, incomingRawEntity.ExternalEntityId, isUpdate);
            this.MergeEntities(requestContext, key, isUpdate, entityToSave, incomingRawEntity);

            // Virtualize added/updated properties as needed
            this.VirtualizeEntity(requestContext, entityToSave);

            // Save to entity store
            if (!entityStore.SaveEntity(requestContext, entityToSave, isUpdate))
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Could not save to entity store. ExternalEntityId: {0}",
                    entity.ExternalEntityId);
                LogManager.Log(LogLevels.Error, true, msg);
                throw new DataAccessException(msg);
            }

            // Save to index store
            try
            {
                indexStore.SaveEntity(entityToSave, isUpdate);
            }
            catch (DataAccessException)
            {
                // Rollback the entity store
                entityStore.RemoveEntity(entityToSave.Key);
                throw;
            }

            // TODO: Try to eliminate the need for this by modifying callers who rely on the
            // state of the incoming entity being updated.
            incomingRawEntity.Key = entityToSave.Key;
            incomingRawEntity.LocalVersion = entityToSave.LocalVersion;
            incomingRawEntity.LastModifiedDate = entityToSave.LastModifiedDate;
            incomingRawEntity.LastModifiedUser = entityToSave.LastModifiedUser;
        }

        /// <summary>Get the latest version of the entity as a base target for the merge.</summary>
        /// <param name="forceOverwrite">True to force an overwrite (meaning we ignore the current one).</param>
        /// <param name="entityId">Id of Entity to get.</param>
        /// <param name="isUpdate">True if this is an update.</param>
        /// <returns>An unfiltered entity with all blob refs unrealized.</returns>
        private IEntity GetMergeEntity(bool forceOverwrite, EntityId entityId, bool isUpdate)
        {
            // If this is not an update the merged entity will start with a new entity
            if (!isUpdate)
            {
                return new Entity();
            }

            // For merge, we need all properties as blob references
            var existingContext = new RequestContext { ReturnBlobReferences = true, EntityFilter = new RepositoryEntityFilter(true, true, true, true) };
            var existingEntity = this.GetEntity(existingContext, entityId);

            if (!forceOverwrite)
            {
                return existingEntity;
            }

            // If this is a forceOverwrite most properties will be discarded but we still need to preserve
            // certain interface properties - transfer them so it can be handled downstream.
            var mergeEntity = new Entity();
            foreach (var interfaceProperty in existingEntity.InterfaceProperties)
            {
                mergeEntity.InterfaceProperties.Add(interfaceProperty);
            }

            return mergeEntity;
        }

        /// <summary>Merge an updated entity with the existing entity if one exists.</summary>
        /// <param name="targetMergeEntity">Entity being composed to save.</param>
        /// <param name="incomingRawEntity">Entity with updates (or new).</param>
        /// <param name="isUpdate">True if this is an update.</param>
        [SuppressMessage("Microsoft.Performance", "CA1822", Justification = "Not guaranteed to remain static by intent.")]
        private void MergeInterfaceProperties(IEntity targetMergeEntity, IEntity incomingRawEntity, bool isUpdate)
        {
            // Preserve non-updateable properties
            var createDate = targetMergeEntity.CreateDate;

            // Copy over interface properties
            targetMergeEntity.InterfaceProperties.Clear();
            foreach (var property in incomingRawEntity.InterfaceProperties)
            {
                targetMergeEntity.InterfaceProperties.Add(property);
            }

            // Restore non-updateable properties
            if (isUpdate)
            {
                targetMergeEntity.CreateDate = createDate;
            }
        }

        /// <summary>Update internally maintained entity properties (key, timestamp, user, version)</summary>
        /// <param name="requestContext">Context object for the repository request.</param>
        /// <param name="targetMergeEntity">Entity being composed to save.</param>
        /// <param name="updatedKey">Updated (or new) key.</param>
        /// <param name="isUpdate">True if this is an update.</param>
        [SuppressMessage("Microsoft.Performance", "CA1822", Justification = "Not guaranteed to remain static by intent.")]
        private void UpdateReadOnlyProperties(
            RequestContext requestContext,
            IEntity targetMergeEntity,
            IStorageKey updatedKey,
            bool isUpdate)
        {
            // Set the new key
            targetMergeEntity.Key = updatedKey;

            // Set LastModifiedDate and User independent of whether it's an update.
            var dateNow = DateTime.UtcNow;
            targetMergeEntity.LastModifiedDate = dateNow;
            targetMergeEntity.LastModifiedUser = requestContext.UserId;

            if (isUpdate)
            {
                // Increment LocalVersion
                targetMergeEntity.LocalVersion++;
            }
            else
            {
                // Set LocalVersion and CreateDate
                targetMergeEntity.LocalVersion = 0;
                targetMergeEntity.CreateDate = dateNow;
            }
        }

        /// <summary>Merge the non-interface properties and associations of two entities based on the supplied entity filter.</summary>
        /// <param name="targetMergeEntity">The target of the merge.</param>
        /// <param name="incomingRawEntity">Entity with updates (or new).</param>
        /// <param name="entityFilter">The entity filter to apply to the merge.</param>
        /// <param name="forceOverwrite">True to force an overwrite (replace) of all properties and associations.</param>
        private void MergeWithEntityFilter(
            IEntity targetMergeEntity, IEntity incomingRawEntity, IEntityFilter entityFilter, bool forceOverwrite)
        {
            // Current default behavior is to allow everything to be saved if no filter is specified
            if (entityFilter == null || forceOverwrite)
            {
                entityFilter = new RepositoryEntityFilter(true, true, true, true);
            }

            // Merge associations if specified in filter
            this.MergeAssociations(targetMergeEntity, incomingRawEntity, entityFilter);

            // Merge the property bag
            this.MergePropertyBag(targetMergeEntity, incomingRawEntity, entityFilter);
        }

        /// <summary>Merge associations from an input entity if specified by filter.</summary>
        /// <param name="targetMergeEntity">Entity being composed to save.</param>
        /// <param name="incomingRawEntity">Entity with updates (or new).</param>
        /// <param name="entityFilter">The entity filter to apply.</param>
        [SuppressMessage("Microsoft.Performance", "CA1822", Justification = "Not guaranteed to remain static by intent.")]
        private void MergeAssociations(
            IEntity targetMergeEntity,
            IEntity incomingRawEntity,
            IEntityFilter entityFilter)
        {
            // Nothing to do. Ignore anything else in the filter if associations are excluded.
            if (!entityFilter.IncludeAssociations)
            {
                return;
            }

            var associationsToRemove = targetMergeEntity.Associations.ToList();
            var associationsToAdd = incomingRawEntity.Associations.ToList();

            // If we have an association name filter reduce the sets to match the filter.
            var associationNameFilters = entityFilter.GetAssociationNameFilter().ToList();
            if (associationNameFilters.Any())
            {
                associationsToRemove = associationsToRemove.Where(a => associationNameFilters.Contains(a.ExternalName)).ToList();
                associationsToAdd = associationsToAdd.Where(a => associationNameFilters.Contains(a.ExternalName)).ToList();
            }

            // First remove assocations to be dropped/replaced
            foreach (var association in associationsToRemove)
            {
                targetMergeEntity.Associations.Remove(association);
            }

            // Then add back new/updated associations
            foreach (var association in associationsToAdd)
            {
                targetMergeEntity.Associations.Add(association);
            }
        }

        /// <summary>Merge properties matching propertyFilter between and incoming entity and an existing entity.</summary>
        /// <param name="targetMergeEntity">Entity being composed to save.</param>
        /// <param name="incomingRawEntity">Entity with updates (or new).</param>
        /// <param name="entityFilter">The entity filter to apply.</param>
        private void MergePropertyBag(
            IEntity targetMergeEntity,
            IEntity incomingRawEntity, 
            IEntityFilter entityFilter)
        {
            // Nothing to do. Ignore anything else in the filter if all are excluded
            var propertyFilters = entityFilter.Filters;
            if (!propertyFilters.Any())
            {
                return;
            }

            var propertiesToRemove = targetMergeEntity.Properties.Where(p => propertyFilters.Contains(p.Filter)).ToList();
            var propertiesToAdd = incomingRawEntity.Properties.Where(p => propertyFilters.Contains(p.Filter)).ToList();

            // If we have a property name filter reduce the sets to match the filter.
            var propertyNameFilters = entityFilter.GetPropertyNameFilter().ToList();
            if (propertyNameFilters.Any())
            {
                propertiesToRemove = propertiesToRemove.Where(p => propertyNameFilters.Contains(p.Name)).ToList();
                propertiesToAdd = propertiesToAdd.Where(p => propertyNameFilters.Contains(p.Name)).ToList();
            }

            // Apply merge logic per property to determine final set of properties to include.
            propertiesToAdd = propertiesToAdd
                .Select(incomingProperty => this.MergeProperty(targetMergeEntity, incomingProperty)).ToList();

            // First remove the properties to be dropped/replaced
            foreach (var entityProperty in propertiesToRemove)
            {
                targetMergeEntity.Properties.Remove(entityProperty);
            }

            // Then add back new/updated properties
            foreach (var entityProperty in propertiesToAdd)
            {
                targetMergeEntity.Properties.Add(entityProperty);
            }
        }

        /// <summary>Merge properties matching propertyFilter between an incoming entity and an existing entity.</summary>
        /// <param name="mergeEntity">Entity being composed to save.</param>
        /// <param name="propertyInput">Property with updates (or new).</param>
        /// <returns>The property to save.</returns>
        private EntityProperty MergeProperty(IEntity mergeEntity, EntityProperty propertyInput)
        {
            var existingProperty = mergeEntity.Properties.SingleOrDefault(p => p.Name == propertyInput.Name);

            // If there isn't a cooresponding existing property or
            // this isn't a blob ref use the incoming property value
            if (existingProperty == null || !existingProperty.IsBlobRef)
            {
                return propertyInput;
            }

            // Realize the existing property and compare to the new value. If they are the same
            // then use the existing property blobRef to avoid creating a new blob
            var blobStore = this.BlobStoreFactory.GetBlobStore();
            var realizedExistingProperty = RealizeProperty(new RequestContext(), existingProperty, blobStore);
            if (realizedExistingProperty.Value.DynamicValue != propertyInput.Value.DynamicValue)
            {
                return propertyInput;
            }

            // Value didn't change so add the existing property blob ref so we don't create a new one.
            return existingProperty;
        }

        /// <summary>Get a new or updated key.</summary>
        /// <param name="unwrappedEntity">The raw entity the key is for.</param>
        /// <param name="requestContext">The repository request context.</param>
        /// <param name="key">And existing key if present.</param>
        /// <returns>The new key for the update.</returns>
        private IStorageKey GetNewKey(IEntity unwrappedEntity, RequestContext requestContext, IStorageKey key)
        {
            // key will be null if this is an insert rather than an update
            if (key == null)
            {
                // TODO: Get account from company context
                // Get new storage key
                key = this.StorageKeyFactory.BuildNewStorageKey(
                    DefaultStorageAccount, requestContext.ExternalCompanyId, unwrappedEntity);
            }
            else
            {
                // Get an rebuilt storage key
                key = this.StorageKeyFactory.BuildUpdatedStorageKey(key, unwrappedEntity);
            }

            return key;
        }

        /// <summary>Check if the userId has been used with a different entity id.</summary>
        /// <param name="context">The request context.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="requestedEntityId">The entity id of the user being saved.</param>
        /// <returns>True if the userId has been used.</returns>
        private bool CheckForDuplicateUserId(RequestContext context, string userId, EntityProperty requestedEntityId)
        {
            try
            {
                var userEntity = this.GetUser(context, userId);

                // If we found the userId the entity id better match (this is an update) or it means
                // we are trying to create a new entity with the same userId.
                return userEntity.ExternalEntityId != requestedEntityId;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>Get subset of valid current versions of the entities retrieved from entity store through non-key queries.</summary>
        /// <param name="entities">The list of entities.</param>
        /// <returns>The subset of valid current versions of the entities.</returns>
        private IEnumerable<IEntity> GetValidCurrentEntities(HashSet<IEntity> entities)
        {
            // Filter orphaned entities whose entity Id is in the index but whose table key does not correspond to
            // a valid key in the index (failed to clean up entity when it couldn't be added to index)
            var entityIds = entities.Select(u => u.ExternalEntityId).Distinct();
            var currentKeys =
                entityIds.Select(
                    id => this.IndexStoreFactory.GetIndexStore().GetStorageKey(id, DefaultStorageAccount));
            var validEntities = new List<IEntity>();

            foreach (var key in currentKeys)
            {
                // It shouldn't happen but we might have an orphaned entity with an entity id 
                // that is not in the index at all.
                if (key == null)
                {
                    continue;
                }

                // There should be no more than one entity with this key since the keys were culled from the distinct entity ids.
                // However, if the index was updated on one of the entities since getting it there could be no match. In
                // this case we will not return the out of date entity.
                // TODO: Figure out if we can do something more robust - possibly do a best effort and see if the key is
                // TODO: at least in the history of the entity.
                var validEntity = entities.SingleOrDefault(u => u.Key.IsEqual(key));

                // Otherwise this may be an index record that gets grandfathered in from before versioning
                // TODO: Fix storage key encapsulation violation
                if (validEntity == null && key.LocalVersion == 0)
                {
                    // If there is exactly one entity matching the row id and local version is 0 in the index
                    // we will call it a match.
                    validEntity = entities.SingleOrDefault(u => ((AzureStorageKey)u.Key).RowId == ((AzureStorageKey)key).RowId);
                }

                if (validEntity != null)
                {
                    validEntities.Add(validEntity);
                }
            }

            return validEntities;
        }
    }
}
