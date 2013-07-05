// -----------------------------------------------------------------------
// <copyright file="RequestContext.cs" company="Rare Crowds Inc">
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

namespace DataAccessLayer
{
    /// <summary>Context information needed for all repository requests</summary>
    public class RequestContext
    {
        /// <summary>Initializes a new instance of the <see cref="RequestContext"/> class. Default constructor.</summary>
        public RequestContext()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="RequestContext"/> class. Copy constructor.</summary>
        /// <param name="context">source context</param>
        public RequestContext(RequestContext context)
        {
            this.ExternalCompanyId = context.ExternalCompanyId;
            this.UserId = context.UserId;
            this.EntityFilter = context.EntityFilter != null ? context.EntityFilter.Clone() : null;
            this.RetryProvider = context.RetryProvider != null ? context.RetryProvider.Clone() : null;
            this.ForceOverwrite = context.ForceOverwrite;
            this.ReturnBlobReferences = context.ReturnBlobReferences;
        }

        /// <summary>Gets or sets the user id (user entity Id) associated with the call.</summary>
        public string UserId { get; set; }

        /// <summary>Gets or sets external entity id of the company the entities involved in the request are associated with.</summary>
        public EntityId ExternalCompanyId { get; set; }

        /// <summary>Gets or sets EntityFilter for the request.</summary>
        public IEntityFilter EntityFilter { get; set; }

        /// <summary>Gets or sets the retry provider for the request.</summary>
        public IRetryProvider RetryProvider { get; set; }

        /// <summary>Gets or sets a value indicating whether to force an overwrite rather than merge.</summary>
        public bool ForceOverwrite { get; set; }

        /// <summary>Gets or sets a value indicating whether to include only blob references for returned blob properties.</summary>
        public bool ReturnBlobReferences { get; set; }
    }
}
