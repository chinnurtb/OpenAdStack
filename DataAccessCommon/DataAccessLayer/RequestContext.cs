// -----------------------------------------------------------------------
// <copyright file="RequestContext.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
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
            this.ForceOverwrite = context.ForceOverwrite;
            this.ReturnBlobReferences = context.ReturnBlobReferences;
        }

        /// <summary>Gets or sets the user id (user entity Id) associated with the call.</summary>
        public string UserId { get; set; }

        /// <summary>Gets or sets external entity id of the company the entities involved in the request are associated with.</summary>
        public EntityId ExternalCompanyId { get; set; }

        /// <summary>Gets or sets EntityFilter for the request.</summary>
        public IEntityFilter EntityFilter { get; set; }

        /// <summary>Gets or sets a value indicating whether to force an overwrite rather than merge.</summary>
        public bool ForceOverwrite { get; set; }

        /// <summary>Gets or sets a value indicating whether to include only blob references for returned blob properties.</summary>
        public bool ReturnBlobReferences { get; set; }
    }
}
