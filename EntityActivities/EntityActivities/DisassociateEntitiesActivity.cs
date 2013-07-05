// -----------------------------------------------------------------------
// <copyright file="DisassociateEntitiesActivity.cs" company="Rare Crowds Inc">
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
using System.Linq;
using System.Web.Script.Serialization;
using Activities;
using DataAccessLayer;
using EntityUtilities;
using ResourceAccess;

namespace EntityActivities
{
    /// <summary>
    /// Activity for disassociating entities
    /// </summary>
    /// <remarks>
    /// Disassociates different entities.
    /// RequiredValues:
    ///   ParentEntityId - EntityId of the entity from which the association is to be removed
    ///   EntityId - EntityId of the association's target entity to disassociate
    ///   MessagePayload - JSON containing the AssociationName and optional AssociationType
    /// </remarks>
    [Name(EntityActivityTasks.DisassociateEntities)]
    [RequiredValues(EntityActivityValues.ParentEntityId, EntityActivityValues.EntityId, EntityActivityValues.MessagePayload)]
    public class DisassociateEntitiesActivity : EntityActivity
    {
        /// <summary>JSON Serializer</summary>
        private static readonly JavaScriptSerializer JsonSerializer = new JavaScriptSerializer();

        /// <summary>Process the request</summary>
        /// <param name="request">The request containing input values</param>
        /// <returns>The result containing output values</returns>
        protected override ActivityResult ProcessRequest(ActivityRequest request)
        {
            // This is functionally an internal operation since it is wholly arbitrated
            var internalContext = CreateRepositoryContext(RepositoryContextType.InternalEntitySave, request);

            // Get association payload values
            var payloadValues = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Values[EntityActivityValues.MessagePayload]);
            var associationName = payloadValues["AssociationName"];
            var parentEntityId = new EntityId(payloadValues["ParentEntity"]);
            var targetEntityId = new EntityId(payloadValues["ChildEntity"]);

            // Verify parent and request entity ids match
            var requestEntityId = new EntityId(request.Values[EntityActivityValues.EntityId]);
            if (requestEntityId != parentEntityId)
            {
                return ErrorResult(
                    ActivityErrorId.InvalidEntityId,
                    "Request EntityId and association payload ParentEntity must match. (request: '{0}' payload: '{1}')",
                    requestEntityId,
                    parentEntityId);
            }

            // Get the association type (if specified)
            AssociationType? associationType = null;
            if (payloadValues.ContainsKey("AssociationType"))
            {
                AssociationType type;
                if (Enum.TryParse<AssociationType>(payloadValues["AssociationType"], out type))
                {
                    associationType = type;
                }
            }

            try
            {
                // Get the entity from which the association is to be removed
                var entity = this.Repository.GetEntity(internalContext, parentEntityId);

                // Find the matching association(s)
                var associationIndexes = entity.Associations
                    .Where(a =>
                        a.TargetEntityId == targetEntityId &&
                        a.ExternalName == associationName &&
                        (associationType == null || a.AssociationType == associationType))
                    .Select(a =>
                        entity.Associations.IndexOf(a))
                    .ToArray();
                if (associationIndexes.Length == 0)
                {
                    return EntityNotFoundError(targetEntityId);
                }

                // Remove the association(s)
                foreach (var index in associationIndexes)
                {
                    entity.Associations.RemoveAt(index);
                }

                // Save the modified entity and return success
                this.Repository.SaveEntity(internalContext, entity);
                return this.SuccessResult();
            }
            catch (DataAccessEntityNotFoundException e)
            {
                return EntityNotFoundError(e);
            }
        }
    }
}
