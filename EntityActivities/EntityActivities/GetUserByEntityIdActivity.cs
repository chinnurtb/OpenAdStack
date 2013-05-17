//-----------------------------------------------------------------------
// <copyright file="GetUserByEntityIdActivity.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Activities;
using DataAccessLayer;

namespace EntityActivities
{
    /// <summary>
    /// Activity for getting users by their entity id
    /// </summary>
    /// <remarks>
    /// Gets the user with the specified EntityId
    /// RequiredValues:
    ///   UserEntityId - ExternalEntityId of the user to get
    /// ResultValues:
    ///   User - The user as json
    /// </remarks>
    [Name("GetUserByEntityId"), RequiredValues("EntityId"), ResultValues("User")]
    public class GetUserByEntityIdActivity : GetEntityByEntityIdActivityBase
    {
        /// <summary>
        /// Gets the expected EntityCategory of the returned entity
        /// </summary>
        protected override string EntityCategory
        {
            get { return UserEntity.UserEntityCategory; }
        }

        /// <summary>
        /// Gets the name of the result value in which to return the entity
        /// </summary>
        protected override string ResultValue
        {
            get { return "User"; }
        }

        /// <summary>
        /// Create a return json string for the User entity type
        /// </summary>
        /// <param name="entity">the entity</param>
        /// <returns>a json string</returns>
        protected override string CreateJsonResult(IEntity entity)
        {
            UserEntity companyEntity = new UserEntity(entity);
            return companyEntity.SerializeToJson();
        }
    }
}
