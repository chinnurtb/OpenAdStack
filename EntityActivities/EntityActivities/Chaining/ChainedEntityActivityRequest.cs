//-----------------------------------------------------------------------
// <copyright file="ChainedEntityActivityRequest.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Activities;
using DataAccessLayer;

namespace EntityActivities.Chaining
{
    /// <summary>
    /// Method that given the current activity request's context and an entity
    /// creates a new activity request to be submitted in the chain.
    /// </summary>
    /// <param name="context">Entity repository request context</param>
    /// <param name="entity">The entity</param>
    /// <returns>The activity request</returns>
    public delegate ActivityRequest ChainedEntityActivityRequest(RequestContext context, EntityWrapperBase entity);
}
