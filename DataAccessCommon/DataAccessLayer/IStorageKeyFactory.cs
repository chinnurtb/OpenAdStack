// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IStorageKeyFactory.cs" company="Rare Crowds Inc">
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

namespace DataAccessLayer
{
    /// <summary>Interface definition for storage key factories</summary>
    public interface IStorageKeyFactory
    {
        /// <summary>Build a new storage key for an entity that will be inserted to the entity store.</summary>
        /// <param name="storageAccountName">The storage account name.</param>
        /// <param name="companyExternalId">External Id of company associated with this entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        IStorageKey BuildNewStorageKey(string storageAccountName, EntityId companyExternalId, IEntity rawEntity);

        /// <summary>Get a storage key for to use for updating and entity.</summary>
        /// <param name="existingKey">The storage key of an existing entity.</param>
        /// <param name="rawEntity">Entity data in raw form.</param>
        /// <returns>An IStorageKey instance.</returns>
        IStorageKey BuildUpdatedStorageKey(IStorageKey existingKey, IEntity rawEntity);

        /// <summary>Build a storage key from the serialized representation.</summary>
        /// <param name="serializedKey">The serialized key.</param>
        /// <returns>The storage key.</returns>
        IStorageKey DeserializeKey(string serializedKey);

        /// <summary>Build a serialized representation of the key.</summary>
        /// <param name="key">The storage key.</param>
        /// <returns>A string serialized form of the blob.</returns>
        string SerializeBlobKey(IStorageKey key);
    }
}
