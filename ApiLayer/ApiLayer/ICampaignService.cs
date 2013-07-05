// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICampaignService.cs" company="Rare Crowds Inc">
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