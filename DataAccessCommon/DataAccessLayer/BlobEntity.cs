// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlobEntity.cs" company="Rare Crowds Inc">
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
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Utilities.Serialization;

namespace DataAccessLayer
{
    /// <summary>
    /// An entity wrapper for blob reference entities.
    /// </summary>
    public class BlobEntity : EntityWrapperBase
    {
        /// <summary>Property Name for BlobPropertyType property.</summary>
        public const string BlobPropertyTypeName = "BlobPropertyType";

        /// <summary>Property Name for BlobBytes property.</summary>
        public const string BlobDataPropertyName = "BlobData";

        /// <summary>Category Name for Blob Entities.</summary>
        public const string CategoryName = "DataReference";

        /// <summary>Initializes a new instance of the <see cref="BlobEntity"/> class.</summary>
        /// <param name="entity">The IEntity object from which to construct.</param>
        public BlobEntity(IEntity entity)
        {
            this.Initialize(entity);

            // If entity is not really an IRawEntity but an IEntity, and it's a legacy blob
            // with BlobBytes populated, Initialize will strip the BlobBytes since they
            // are not part of the entity properties. Make sure we carry them over here.
            // TODO: back compat only until entities are migrated
            var legacyBlob = entity as BlobEntity;
            if (legacyBlob != null && legacyBlob.BlobBytes != null)
            {
                this.BlobBytes = legacyBlob.BlobBytes;
            }
        }
        
        /// <summary>Initializes a new instance of the <see cref="BlobEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        public BlobEntity(EntityId externalEntityId)
            : this(externalEntityId, null)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="BlobEntity"/> class.</summary>
        /// <param name="externalEntityId">The external entity id.</param>
        /// <param name="serializedObjToBlob">The string obj to blob.</param>
        public BlobEntity(EntityId externalEntityId, string serializedObjToBlob)
        {
            // Construct the underlying wrapped entity
            var wrappedEntity = new Entity
            {
                ExternalEntityId = externalEntityId,
                EntityCategory = CategoryName,
            };

            this.Initialize(wrappedEntity);

            this.BlobPropertyType = string.Empty;

            // Set the blob string on the entity if provided
            if (serializedObjToBlob != null)
            {
                this.BlobData = serializedObjToBlob;
            }
        }

        /// <summary>Initializes a new instance of the <see cref="BlobEntity"/> class.</summary>
        public BlobEntity()
        {
        }

        /// <summary>Gets or sets BlobData.</summary>
        public EntityProperty BlobData
        {
            get { return this.TryGetEntityPropertyByName(BlobDataPropertyName, null); }
            set { this.SetEntityProperty(new EntityProperty { Name = BlobDataPropertyName, Value = value.Value }); }
        }

        /// <summary>Gets or sets Blob property type.</summary>
        public EntityProperty BlobPropertyType
        {
            get { return this.TryGetEntityPropertyByName(BlobPropertyTypeName, null); }
            set { this.SetEntityProperty(new EntityProperty { Name = BlobPropertyTypeName, Value = value.Value }); }
        }

        /// <summary>Gets or sets blob bytes. Backward compatibility only, for migrating legacy blob entities.</summary>
        internal byte[] BlobBytes { get; set; }

        /// <summary>Build a BlobEntity given a DataContract serializable object.</summary>
        /// <param name="externalEntityId">The external entity Id to assign the entity.</param>
        /// <param name="objToBlob">The object that will be serialized.</param>
        /// <typeparam name="T">Type of the object to be serialized.</typeparam>
        /// <returns>A blob entity that has not yet been persisted.</returns>
        public static BlobEntity BuildBlobEntity<T>(EntityId externalEntityId, T objToBlob) where T : class
        {
            return AddDataToBlob(objToBlob, blobString => new BlobEntity(externalEntityId, blobString));
        }

        /// <summary>Build a BlobEntity given a DataContract serializable object.</summary>
        /// <param name="objToBlob">The object that will be serialized.</param>
        /// <typeparam name="T">Type of the object to be serialized.</typeparam>
        public void UpdateBlobEntity<T>(T objToBlob) where T : class
        {
            AddDataToBlob(objToBlob, blobString => { this.BlobData = blobString; return this; });
        }

        /// <summary>Deserialize the contained blob bytes.</summary>
        /// <typeparam name="TResult">Target type of deserialize</typeparam>
        /// <returns>The deserialized object (default construction if blob bytes are null)</returns>
        /// <exception cref="DataAccessException">If BlobData is not set.</exception>
        /// <exception cref="AppsJsonException">If BlobData cannot deserialize to requested type.</exception>
        public TResult DeserializeBlob<TResult>() where TResult : class
        {
            // TODO: For backward compatibility. This should be removed when possible after migration
            if (this.BlobBytes != null)
            {
                return this.DeserializeBlobFromXml<TResult>();
            }

            // If we don't have a blob string, fail
            if (this.BlobData.Value == null)
            {
                throw new DataAccessException(
                    "Blob entity has no blob data: {0}.".FormatInvariant((EntityId)ExternalEntityId));
            }

            // First determine if the generic parameter is string. We want to avoid
            // serialize/deserialize for strings.
            var deserializedObj = this.BlobData.Value.SerializationValue as TResult;
            if (deserializedObj != null)
            {
                return deserializedObj;
            }

            // Otherwise deserialize as json.
            return AppsJsonSerializer.DeserializeObject<TResult>(this.BlobData.Value.SerializationValue);
        }

        /// <summary>
        /// Backward compatibility only, for migrating legacy blob entities.Migration only: 
        /// Deserialize the contained blob bytes previously saved as xml serialized data.
        /// </summary>
        /// <typeparam name="TResult">Target type of deserialize</typeparam>
        /// <returns>The deserialized object (default construction if blob bytes are null)</returns>
        /// <exception cref="DataAccessException">If BlobBytes are not set.</exception>
        internal TResult DeserializeBlobFromXml<TResult>()
        {
            if (this.BlobBytes == null)
            {
                throw new DataAccessException(
                    "Blob entity has no blob data.".FormatInvariant((EntityId)ExternalEntityId));
            }

            // Deserialize the bytes.
            using (var stream = new MemoryStream(this.BlobBytes))
            {
                var serializer = new DataContractSerializer(typeof(TResult));
                stream.Seek(0, SeekOrigin.Begin);
                return (TResult)serializer.ReadObject(stream);
            }
        }

        /// <summary>Abstract method to validate type of entity.</summary>
        /// <param name="entity">The entity.</param>
        protected override void ValidateEntityType(IEntity entity)
        {
            ThrowIfCategoryMismatch(entity, CategoryName);
        }

        /// <summary>Add data to a given a DataContract serializable object and builder method.</summary>
        /// <param name="objToBlob">The object that will be serialized.</param>
        /// <param name="buildBlob">Builder method returning a blob.</param>
        /// <typeparam name="T">Type of the object to be serialized.</typeparam>
        /// <returns>A blob entity with the serialized data.</returns>
        private static BlobEntity AddDataToBlob<T>(T objToBlob, Func<string, BlobEntity> buildBlob) where T : class
        {
            // If data is already a string, don't bother to serialize it
            var candidateData = objToBlob as string;
            if (candidateData != null)
            {
                // Construct a entity object
                return buildBlob(candidateData);
            }

            // Otherwise serialize to json string
            try
            {
                if (objToBlob == null)
                {
                    throw new ArgumentException("Cannot serialize null object to blob.");
                }

                var blobString = JsonConvert.SerializeObject(objToBlob);
                return buildBlob(blobString);
            }
            catch (Exception e)
            {
                var msg = "Could not serialize entity as requested type: {0}".FormatInvariant(typeof(T).FullName);
                throw new DataAccessException(msg, e);
            }
        }
    }
}
