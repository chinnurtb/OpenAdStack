//-----------------------------------------------------------------------
// <copyright file="RepositoryContextType.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace EntityActivities
{
    /// <summary>
    /// IEntityRepository RequestContext type enumeration for use with EntityActivity.CreateRepositoryContext.
    /// </summary>
    public enum RepositoryContextType
    {
        /// <summary>
        /// For a repository get that will result in the entity being returned in a client response.
        /// </summary>
        ExternalEntityGet,
        
        /// <summary>
        /// For a repository save of an entity originating with a client request. 
        /// </summary>
        ExternalEntitySave,
        
        /// <summary>
        /// For an internal repository get where the entity is not directly inbound or outbound
        /// to a client.
        /// </summary>
        InternalEntityGet,
        
        /// <summary>
        /// For an internal repository save where the entity is not directly inbound or outbound
        /// to a client.
        /// </summary>
        InternalEntitySave
    }
}
