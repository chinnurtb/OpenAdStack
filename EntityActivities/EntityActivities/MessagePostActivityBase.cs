//-----------------------------------------------------------------------
// <copyright file="MessagePostActivityBase.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Activities;
using EntityActivities;
using EntityUtilities;

namespace EntityActivities
{
    /// <summary>
    /// Base Class Messages Posted to Entities
    /// </summary>
    [RequiredValues(EntityActivityValues.EntityId, EntityActivityValues.MessagePayload)]
    public abstract class MessagePostActivityBase : EntityActivity
    {
    }
}
