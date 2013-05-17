// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssociationType.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DataAccessLayer
{
    /// <summary>
    /// The association types we support
    /// TODO: Come up with the real set of association relations we will support
    /// </summary>
    public enum AssociationType
    {
        /// <summary>Abritrary named relationship to target entity.</summary>
        Relationship,

        /// <summary>The entity is a parent of the target entity.</summary>
        Parent,

        /// <summary>The entity is a child of the target entity.</summary>
        Child
    }
}
