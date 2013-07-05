// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WorkHandler.cs" company="Rare Crowds Inc">
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
using DataAccessLayer;
using EntityUtilities;
using Utilities.Serialization;

namespace SetAllocationParameters
{
    /// <summary>Class to do the work of setting dynamic allocation parameters.</summary>
    public class WorkHandler
    {
        /// <summary>Initializes a new instance of the <see cref="WorkHandler"/> class.</summary>
        /// <param name="repository">An IEntityRepository instance.</param>
        /// <param name="paramsJson">Allocation parameters json.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="targetEntityId">Target entity id.</param>
        /// <param name="replace">True to destructively overwrite params.</param>
        public WorkHandler(
            IEntityRepository repository, 
            string paramsJson, 
            EntityId companyEntityId, 
            EntityId targetEntityId, 
            bool replace)
        {
            this.ParamsJson = paramsJson;
            this.CompanyEntityId = companyEntityId;
            this.TargetEntityId = targetEntityId;
            this.Replace = replace;
            this.Repository = repository;
        }

        /// <summary>Gets the IEntityRepository instance.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Gets a value indicating whether the params should be replaced or merged.</summary>
        internal bool Replace { get; private set; }

        /// <summary>Gets the company entity id.</summary>
        internal EntityId CompanyEntityId { get; private set; }

        /// <summary>Gets the target entity id.</summary>
        internal EntityId TargetEntityId { get; private set; }

        /// <summary>Gets the parameter json.</summary>
        internal string ParamsJson { get; private set; }

        /// <summary>Do the work.</summary>
        public void Run()
        {
            var context = new RequestContext
                {
                    ExternalCompanyId = this.CompanyEntityId, 
                    EntityFilter = new RepositoryEntityFilter(true, true, false, false)
                };

            var targetEntity = this.Repository.GetEntity(context, this.TargetEntityId);
            
            var allocationParameters = AppsJsonSerializer.DeserializeObject<Dictionary<string, string>>(this.ParamsJson);
            var configs = targetEntity.GetConfigSettings();
            var dynAllocConfigKeys = configs.Where(kvp => kvp.Key.Contains("DynamicAllocation.")).Select(kvp => kvp.Key).ToList();

            Console.WriteLine("{0} allocation parameters on target {1}".FormatInvariant(
                    this.Replace ? "Replacing" : "Setting", this.TargetEntityId.ToString()));

            // Remove the existing allocation params
            if (this.Replace)
            {
                foreach (var key in dynAllocConfigKeys)
                {
                    configs.Remove(key);
                }
            }

            // Add/update the new allocation params
            foreach (var allocationParameter in allocationParameters)
            {
                Console.WriteLine("{0} : {1}".FormatInvariant(allocationParameter.Key, allocationParameter.Value));
                configs[allocationParameter.Key] = allocationParameter.Value;
            }

            targetEntity.SetConfigSettings(configs);

            this.Repository.SaveEntity(context, targetEntity);

            Console.WriteLine("Success");
        }
    }
}
