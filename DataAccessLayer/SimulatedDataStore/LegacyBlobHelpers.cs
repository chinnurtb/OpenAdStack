// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LegacyBlobHelpers.cs" company="Rare Crowds Inc">
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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using ConcreteDataStore;
using DataAccessLayer;

namespace SimulatedDataStore
{
    /// <summary>
    /// Legacy blob helper methods.
    /// </summary>
    internal static class LegacyBlobHelpers
    {
        /// <summary>
        /// Save a legacy blob.
        /// These can still be read from public interface but we don't support saving this way.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="blobEntityId">The blob entity id.</param>
        /// <param name="blobTargetObject">The object being saved as a blob entity.</param>
        internal static void SaveLegacyBlob(IEntityRepository repository, RequestContext context, EntityId blobEntityId, object blobTargetObject)
        {
            var blobWithLegacyBytes = GetBlobWithLegacyBytes(blobEntityId, blobTargetObject);
            SaveLegacyBlob(repository, context, blobWithLegacyBytes);
        }

        /// <summary>Get a blob with legacy (xml serialized) blob bytes.</summary>
        /// <param name="blobEntityId">The blob entity id.</param>
        /// <param name="blobTargetObject">The object being saved as a blob entity.</param>
        /// <returns>A BlobEntity with blob bytes set to xml serialized data.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1720", Justification = "But it is an object.")]
        internal static BlobEntity GetBlobWithLegacyBytes(EntityId blobEntityId, object blobTargetObject)
        {
            // Serialize a legacy blob entity.
            byte[] blobBytes;
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(blobTargetObject.GetType());
                serializer.WriteObject(stream, blobTargetObject);
                blobBytes = stream.ToArray();
            }

            var blobEntity = new BlobEntity(blobEntityId);
            blobEntity.BlobBytes = blobBytes;
            return blobEntity;
        }

        /// <summary>Determine if the entity is a legacy blob.</summary>
        /// <param name="entityToTest">The entity...to test.</param>
        /// <returns>True if legacy blob.</returns>
        internal static bool IsLegacyBlob(IEntity entityToTest)
        {
            return (string)entityToTest.EntityCategory == BlobEntity.CategoryName
                   && ((BlobEntity)entityToTest).BlobBytes != null;
        }

        /// <summary>Save a legacy blob. No longer supported but they exist in storage.</summary>
        /// <param name="repository">The repository.</param>
        /// <param name="context">The request context.</param>
        /// <param name="blobWithLegacyBytes">A BlobEntity object with legacy BlobBytes property.</param>
        internal static void SaveLegacyBlob(IEntityRepository repository, RequestContext context, BlobEntity blobWithLegacyBytes)
        {
            var companyId = context.ExternalCompanyId;
            var concreteRepository = (ConcreteEntityRepository)repository;

            // Save a legacy blob entity without going through IEntityRepository 
            // since it's not supported directly any longer
            var legacyBlobEntity = new BlobPropertyEntity(blobWithLegacyBytes.ExternalEntityId, blobWithLegacyBytes.BlobBytes);
            var rawBlobEntity = legacyBlobEntity.SafeUnwrapEntity();
            var blobStore = concreteRepository.BlobStoreFactory.GetBlobStore();
            var key = blobStore.GetStorageKeyFactory().BuildNewStorageKey(
                ConcreteEntityRepository.DefaultStorageAccount, companyId, rawBlobEntity);
            rawBlobEntity.Key = key;
            blobStore.SaveBlob(rawBlobEntity, companyId);
            var indexStore = concreteRepository.IndexStoreFactory.GetIndexStore();
            indexStore.SaveEntity(rawBlobEntity, false);
        }

        /// <summary>Only for use to read legacy blobs.</summary>
        /// <param name="blobStoreFactory">An IBlobStoreFactory instance.</param>
        /// <param name="key">The storage key.</param>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <returns>The entity if a blob, otherwise null</returns>
        internal static BlobEntity CheckIfLegacyBlobAndGet(IBlobStoreFactory blobStoreFactory, IStorageKey key, EntityId externalEntityId)
        {
            var blobKey = key as AzureBlobStorageKey;

            // If this is not a blob key return null
            if (blobKey == null)
            {
                return null;
            }

            // Get the blob entity
            var unwrappedEntity = blobStoreFactory.GetBlobStore().GetBlobByKey(key);

            // Blob entities that have AzureBlobStorageKeys are no longer supported at the
            // interface level. The entity returned cannot be saved unless the caller
            // explicitly builds a new BlobEntity which will reserialize the data.
            var oldEntity = new BlobPropertyEntity(unwrappedEntity);
            var newEntity = new BlobEntity(externalEntityId);
            newEntity.BlobBytes = oldEntity.BlobBytes;
            return newEntity;
        }
    }
}