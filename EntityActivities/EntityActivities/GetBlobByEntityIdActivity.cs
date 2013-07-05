//-----------------------------------------------------------------------
// <copyright file="GetBlobByEntityIdActivity.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System.Collections.Generic;
using Activities;
using DataAccessLayer;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting blobs by their entity id
    /// </summary>
    /// <remarks>
    /// Gets the blob with the specified EntityId
    /// RequiredValues:
    ///   CampaignEntityId - ExternalEntityId of the campaign to get
    ///   BlobEntityId - ExternalEntityId of the blob to get
    /// ResultValues:
    ///   Blob - The blob as json
    /// </remarks>
    [Name(EntityActivityTasks.GetBlobByEntityId)]
    [RequiredValues(EntityActivityValues.BlobEntityId)]
    [ResultValues(EntityActivityValues.Blob)]
    public class GetBlobByEntityIdActivity : GetEntityByEntityIdActivityBase
    {
        /// <summary>
        /// Gets the expected EntityCategory of the returned entity
        /// </summary>
        protected override string EntityCategory
        {
            get { return BlobEntity.CategoryName; }
        }

        /// <summary>
        /// Gets the name of the request value containing the ExternalEntityId
        /// </summary>
        protected override string EntityIdValue
        {
            get { return EntityActivityValues.BlobEntityId; }
        }

        /// <summary>
        /// Gets the name of the result value in which to return the entity
        /// </summary>
        protected override string ResultValue
        {
            get { return EntityActivityValues.Blob; }
        }

        /// <summary>
        /// Create a return json string for the Blob entity type
        /// </summary>
        /// <param name="entity">the entity</param>
        /// <param name="queryValues">The parameter is not used.</param>
        /// <returns>a json string</returns>
        protected override string CreateJsonResult(
            IEntity entity,
            Dictionary<string, string> queryValues)
        {
            return this.CreateJsonResult(entity);
        }

        /// <summary>
        /// Create a return json string for the Campaign entity type
        /// </summary>
        /// <param name="entity">the entity</param>
        /// <returns>a json string</returns>
        protected override string CreateJsonResult(IEntity entity)
        {
            BlobEntity blobEntity = new BlobEntity(entity);
            return blobEntity.DeserializeBlob<string>();
        }
    }
}
