// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICampaignService.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiLayer
{
    /// <summary>
    /// This is an interface that exposes interaction with campaign entity
    /// </summary>
    public interface ICampaignService
    {
        /// <summary>
        /// Creates a new campaign
        /// </summary>
        /// <param name="instance">New campaign that is to be created</param>
        /// <returns>Newly created campaign that is created</returns>
        string CreateCampaign(string instance);
        
        /// <summary>
        /// Returns an existing campaign
        /// </summary>
        /// <param name="id">id of the campaign that is to be returned</param>
        /// <returns>Campaign with all the data which is requested</returns>
        string GetCampaign(string id);
        
        /// <summary>
        /// Updates an existing campaign
        /// </summary>
        /// <param name="id">id of the campaign that is to be updated</param>
        /// <param name="instance">Campaign with all the data that is to be updated</param>
        /// <returns>Campaign that is updated with all the data</returns>
        string UpdateCampaign(string id, string instance);
    }
}