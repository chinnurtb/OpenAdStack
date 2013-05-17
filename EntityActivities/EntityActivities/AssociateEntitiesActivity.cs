// -----------------------------------------------------------------------
// <copyright file="AssociateEntitiesActivity.cs" company="Rare Crowds Inc">
// Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using Activities;
using DataAccessLayer;
using EntityUtilities;
using ResourceAccess;

namespace EntityActivities
{
    /// <summary>
    /// Activity for associating entities
    /// </summary>
    /// <remarks>
    /// Associates different entities.
    /// RequiredValues:
    ///   CompanyEntityId - Company external id for the context company
    ///   TargetEntityId - The external id of the entity with which to associate entity
    ///   AssociationName - Association name
    ///   EntityId - The external id of the entity to be associated
    /// ResultValues:
    ///   Entity - The targetId
    /// </remarks>
    [Name(EntityActivityTasks.AssociateEntities)]
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.MessagePayload)]
    [ResultValues(EntityActivityValues.TargetId)]
    public class AssociateEntitiesActivity : EntityActivity
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

            var payloadValues = JsonSerializer.Deserialize<Dictionary<string, string>>(request.Values[EntityActivityValues.MessagePayload]); 

            var sourceEntityId = new EntityId(payloadValues["ParentEntity"]);
            var associationName = payloadValues["AssociationName"];
            var entityId = payloadValues["ChildEntity"];
            
            // set default default assoc type to Relationship, caller may override by providing AssociationType in payload
            var associationType = AssociationType.Relationship;
            if (payloadValues.ContainsKey("AssociationType"))
            {
                associationType = AssociationType.Child;
            }

            if (associationType == AssociationType.Child)
            {
                // make sure this user has write ability on the parent. If not, do not add the association and return auth error
                var userId = request.Values[EntityActivityValues.AuthUserId];
                UserEntity user = null;
                try
                {
                    // Get the user
                    user = this.Repository.GetUser(internalContext, userId);
                }
                catch (ArgumentException)
                {
                    return UserNotFoundError(userId);
                }

                var canonicalResource =
                    new CanonicalResource(
                        new Uri("https://localhost/api/entity/company/{0}".FormatInvariant(sourceEntityId.ToString()), UriKind.Absolute), "POST");
                if (!this.AccessHandler.CheckAccess(canonicalResource, user.ExternalEntityId))
                {
                    return UserNotAuthorized(sourceEntityId.ToString());
                }
            }

            try
            {
                var targetEntity = this.Repository.GetEntity(internalContext, entityId);

                // Associating entities
                this.Repository.AssociateEntities(
                    internalContext,
                    sourceEntityId,
                    associationName,
                    string.Empty,
                    new HashSet<IEntity> { targetEntity },
                    associationType,
                    false);
            }
            catch (DataAccessEntityNotFoundException e)
            {
                return EntityNotFoundError(e);
            }

            return this.SuccessResult();
        }
    }
}
