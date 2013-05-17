//-----------------------------------------------------------------------
// <copyright file="EntityActivity.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Activities;
using ConfigManager;
using DataAccessLayer;
using EntityActivities.Chaining;
using EntityUtilities;
using ResourceAccess;

namespace EntityActivities
{
    /// <summary>
    /// Abstract base class for activities dealing with entities
    /// </summary>
    /// <remarks>
    /// RequiredValues:
    ///   AuthUserId - UserId of the user creating the company
    /// </remarks>
    [RequiredValues(EntityActivityValues.AuthUserId)]
    public abstract class EntityActivity : Activity
    {
        /// <summary>
        /// Gets the default IEntityFilter for saving an incoming client entity.
        /// Do not save extended properties and associations.
        /// Currently system properties are managed by the activity - if present they will be saved.
        /// </summary>
        public static IEntityFilter DefaultSaveEntityFilter
        {
            get { return new RepositoryEntityFilter(true, true, false, false); }
        }

        /// <summary>
        /// Gets the default IEntityFilter for retrieving an entity that will be returned in a client response.
        /// By default extended properties are not retrieved for performance reasons.
        /// Currently system properties are managed by the activity and will be filtered in the response rather
        /// than in the repository request.
        /// </summary>
        public static IEntityFilter DefaultGetEntityFilter
        {
            get { return new RepositoryEntityFilter(true, true, false, true); }
        }

        /// <summary>
        /// Gets the default IEntityFilter for internal respository save operations.
        /// This is appropriate for most operations that will not save entities from the client
        /// or return the entity to the client.
        /// By default extended properties are excluded for performance reasons.
        /// </summary>
        public static IEntityFilter DefaultInternalSaveEntityFilter
        {
            get { return new RepositoryEntityFilter(true, true, false, true); }
        }

        /// <summary>
        /// Gets the default IEntityFilter for internally consumed respository operations.
        /// This is appropriate for most operations that will not save entities from the client
        /// or return the entity to the client.
        /// Extended properties are included.
        /// </summary>
        public static IEntityFilter DefaultInternalGetEntityFilter
        {
            get { return new RepositoryEntityFilter(true, true, true, true); }
        }

        /// <summary>
        /// Gets the entity repository from the activity context
        /// </summary>
        protected IEntityRepository Repository
        {
            get { return (IEntityRepository)this.Context[typeof(IEntityRepository)]; }
        }

        /// <summary>
        /// Gets the user access repository from the activity context
        /// </summary>
        protected IUserAccessRepository UserAccessRepository
        {
            get { return (IUserAccessRepository)this.Context[typeof(IUserAccessRepository)]; }
        }

        /// <summary>
        /// Gets the access handler from the activity context, or returns a new access handler
        /// </summary>
        protected IResourceAccessHandler AccessHandler
        {
            get
            {
                if (this.Context.ContainsKey(typeof(IResourceAccessHandler)))
                {
                    return (IResourceAccessHandler)this.Context[typeof(IResourceAccessHandler)];
                }
                else
                {
                    return new ResourceAccessHandler(this.UserAccessRepository, this.Repository);
                }
            }
        }

        /// <summary>
        /// Gets the chained activity request(s) to submit if specific properties are modified
        /// </summary>
        protected virtual IEnumerable<ModifiedPropertyActivityRequest> ModifiedPropertyActivityRequests
        {
            get { return new ModifiedPropertyActivityRequest[0]; }
        }

        /// <summary>
        /// Gets the chained activity request(s) to submit if specific associations do not match
        /// </summary>
        protected virtual IEnumerable<NonMatchingAssociationActivityRequest> NonMatchingAssociationActivityRequests
        {
            get { return new NonMatchingAssociationActivityRequest[0]; }
        }

        /// <summary>Gets the system auth user id</summary>
        private static string SystemAuthUserId
        {
            get { return Config.GetValue("System.AuthUserId"); }
        }

        /// <summary>
        /// Builds a CustomConfig from the Config system properties of the provided entities.
        /// </summary>
        /// <remarks>
        /// Configuration settings are composited with those from entities later in the
        /// enumeration overwriting those of earlier entities.
        /// </remarks>
        /// <param name="entities">Entities from which to build custom config</param>
        /// <returns>The custom config</returns>
        public static CustomConfig BuildCustomConfigFromEntities(params IEntity[] entities)
        {
            return BuildCustomConfigFromEntities(true, entities);
        }

        /// <summary>
        /// Builds a CustomConfig from the Config system properties of the provided entities.
        /// </summary>
        /// <remarks>
        /// Configuration settings are composited with those from entities later in the
        /// enumeration overwriting those of earlier entities.
        /// </remarks>
        /// <param name="transparent">
        /// Whether to include settings from earlier entities that
        /// are not present in configs from later entities.
        /// (default is true)
        /// </param>
        /// <param name="entities">
        /// Entities from which to build custom config
        /// </param>
        /// <returns>The custom config</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Exception is captured in log")]
        public static CustomConfig BuildCustomConfigFromEntities(bool transparent, params IEntity[] entities)
        {
            return EntityActivityUtilities.BuildCustomConfigFromEntities(transparent, entities);
        }

        /// <summary>
        /// Creates an entity repository RequestContext from an ActivityRequest
        /// </summary>
        /// <param name="contextType">The type of operation being performed.</param>
        /// <param name="request">The request containing context information</param>
        /// <returns>The created request context</returns>
        public static RequestContext CreateRepositoryContext(
            RepositoryContextType contextType, ActivityRequest request)
        {
            return CreateRepositoryContext(
                contextType,
                request,
                null);
        }

        /// <summary>
        /// Creates an entity repository RequestContext from an ActivityRequest
        /// </summary>
        /// <param name="contextType">The type of operation being performed.</param>
        /// <param name="request">The request containing context information</param>
        /// <param name="contextCompanyValueName">
        /// The name of a request value to use as the context's ExternalCompanyId.
        /// Required if you are updating an entity that is not a user, or
        /// creating an entity that is not a user or company.
        /// </param>
        /// <returns>The created request context</returns>
        public static RequestContext CreateRepositoryContext(
            RepositoryContextType contextType, ActivityRequest request, string contextCompanyValueName)
        {
            var contextTypeMap = new Dictionary<RepositoryContextType, IEntityFilter>
                { 
                    { RepositoryContextType.ExternalEntityGet, DefaultGetEntityFilter }, 
                    { RepositoryContextType.ExternalEntitySave, DefaultSaveEntityFilter }, 
                    { RepositoryContextType.InternalEntityGet, DefaultInternalGetEntityFilter }, 
                    { RepositoryContextType.InternalEntitySave, DefaultInternalSaveEntityFilter }, 
                };

            return CreateRepositoryContext(
                contextTypeMap[contextType],
                request,
                contextCompanyValueName);
        }

        /// <summary>
        /// Creates an entity repository RequestContext from an ActivityRequest.
        /// Use the version that takes a RepositoryContextType unless you need to override default behavior.
        /// </summary>
        /// <param name="entityFilter">The request entity filter</param>
        /// <param name="request">The request containing context information</param>
        /// <param name="contextCompanyValueName">
        /// The name of a request value to use as the context's ExternalCompanyId.
        /// Required if you are saving an entity that is not a user or a company.
        /// </param>
        /// <returns>The created request context</returns>
        public static RequestContext CreateRepositoryContext(
            IEntityFilter entityFilter, ActivityRequest request, string contextCompanyValueName)
        {
            // Create a context with the AuthUserId
            var context = new RequestContext
            {
                UserId = request.Values[EntityActivityValues.AuthUserId],
                EntityFilter = entityFilter
            };

            // If the name of a context company value was also given...
            if (contextCompanyValueName != null)
            {
                // Make sure the value is present
                if (!request.Values.ContainsKey(contextCompanyValueName))
                {
                    var message = string.Format(
                        CultureInfo.InvariantCulture,
                        "ActivityRequest missing value for '{0}'. Unable to create entity repository RequestContext.",
                        contextCompanyValueName);
                    throw new ArgumentException(message);
                }

                // Add it to the context
                context.ExternalCompanyId = new EntityId(request.Values[contextCompanyValueName]);
            }

            return context;
        }

        /// <summary>
        /// Error ActivityResult for entity not found errors
        /// </summary>
        /// <param name="entityId">Id of the entity not found</param>
        /// <returns>The error result</returns>
        public ActivityResult EntityNotFoundError(EntityId entityId)
        {
            return this.ErrorResult(
                ActivityErrorId.InvalidEntityId,
                "Entity not found: '{0}'",
                (string)entityId);
        }

        /// <summary>
        /// Error ActivityResult for user not found errors
        /// </summary>
        /// <param name="userId">Id of the user not found</param>
        /// <returns>The error result</returns>
        public ActivityResult UserNotFoundError(string userId)
        {
            return this.ErrorResult(
                ActivityErrorId.InvalidEntityId,
                "User not found: '{0}'",
                userId);
        }

        /// <summary>
        /// Error ActivityResult for entity not found errors
        /// </summary>
        /// <param name="exception">Exception thrown by IEntityRepository</param>
        /// <returns>The error result</returns>
        public ActivityResult EntityNotFoundError(DataAccessEntityNotFoundException exception)
        {
            return this.ErrorResult(
                ActivityErrorId.InvalidEntityId,
                "Entity not found: '{0}'",
                exception);
        }

        /// <summary>
        /// Error ActivityResult for user not authorized
        /// </summary>
        /// <param name="entity">Entity Id</param>
        /// <returns>The error result</returns>
        public ActivityResult UserNotAuthorized(string entity)
        {
            return this.ErrorResult(
                ActivityErrorId.UserAccessDenied,
                "User does not permission on: '{0}'",
                entity);
        }

        /// <summary>
        /// Submits any chained requests based upon the modified properies or associations
        /// </summary>
        /// <param name="context">Entity repository request context for the new request</param>
        /// <param name="original">The original entity</param>
        /// <param name="updated">The updated entity</param>
        internal void SubmitChainedRequests(RequestContext context, EntityWrapperBase original, EntityWrapperBase updated)
        {
            // Check for any changes which require additional action
            var chainedRequests = this.GetModifiedPropertyActivityRequests(context, original, updated);
            if (updated.TryGetPropertyValueByName("Status") == "Approved")
            {
                chainedRequests = chainedRequests.Concat(this.GetUnmatchingAssociationActivityRequests(context, updated));
            }

            // Submit the requests
            foreach (var request in chainedRequests)
            {
                // TODO: Handle failures and category
                this.SubmitRequest(request, true); 
            }            
        }

        /// <summary>
        /// Gets activity requests to submit due to property modifications
        /// </summary>
        /// <param name="context">The entity repository request context</param>
        /// <param name="original">The original entity</param>
        /// <param name="updated">The updated entity</param>
        /// <returns>The activity requests</returns>
        internal IEnumerable<ActivityRequest> GetModifiedPropertyActivityRequests(RequestContext context, EntityWrapperBase original, EntityWrapperBase updated)
        {
            return this.ModifiedPropertyActivityRequests
                .Where(mpar => mpar.Applies(original, updated))
                .Select(mpar => mpar.ChainedActivityRequest(context, updated));
        }

        /// <summary>
        /// Gets activity requests to submit due to unmatching associations
        /// </summary>
        /// <param name="context">The entity repository request context</param>
        /// <param name="entity">The entity</param>
        /// <returns>The activity requests</returns>
        internal IEnumerable<ActivityRequest> GetUnmatchingAssociationActivityRequests(RequestContext context, EntityWrapperBase entity)
        {
            return this.NonMatchingAssociationActivityRequests
                .Where(uaar => uaar.Applies(entity))
                .Select(uaar => uaar.ChainedActivityRequest(context, entity));
        }

        /// <summary>
        /// Creates a entity activity request containing values from the context
        /// </summary>
        /// <param name="context">The request context</param>
        /// <param name="taskName">Name of the activity</param>
        /// <returns>The created activity request</returns>
        protected static ActivityRequest CreateRequestFromContext(RequestContext context, string taskName)
        {
            return CreateRequestFromContext(context, taskName, new Dictionary<string, string> { });
        }

        /// <summary>
        /// Creates a entity activity request containing values from the context
        /// </summary>
        /// <param name="context">The request context</param>
        /// <param name="taskName">Name of the activity</param>
        /// <param name="additionalValues">Additional values to include in the request</param>
        /// <returns>The created activity request</returns>
        protected static ActivityRequest CreateRequestFromContext(RequestContext context, string taskName, IDictionary<string, string> additionalValues)
        {
            var request = new ActivityRequest
            {
                Task = taskName,
                Values =
                {
                    { EntityActivityValues.AuthUserId, context.UserId }
                }
            };

            if (context.ExternalCompanyId != null)
            {
                request.Values.Add(EntityActivityValues.CompanyEntityId, context.ExternalCompanyId);
            }

            foreach (var additionalValue in additionalValues)
            {
                request.Values.Add(additionalValue.Key, additionalValue.Value);
            }

            return request;
        }
        
        /// <summary>
        /// Creates a entity activity request containing values from an activity result
        /// </summary>
        /// <param name="result">An ActivityResult</param>
        /// <param name="taskName">Name of the activity</param>
        /// <param name="additionalValues">Additional values to include in the request</param>
        /// <returns>The created activity request</returns>
        protected static ActivityRequest CreateRequestFromResult(ActivityResult result, string taskName, IDictionary<string, string> additionalValues)
        {
            var request = new ActivityRequest
            {
                Task = taskName,
                Values =
                {
                    { EntityActivityValues.AuthUserId, SystemAuthUserId }
                }
            };

            if (result.Values[EntityActivityValues.CompanyEntityId] != null)
            {
                request.Values.Add(EntityActivityValues.CompanyEntityId, result.Values[EntityActivityValues.CompanyEntityId]);
            }

            foreach (var additionalValue in additionalValues)
            {
                request.Values.Add(additionalValue.Key, additionalValue.Value);
            }

            return request;
        }

        /// <summary>Copies properties from original if not set</summary>
        /// <remarks>Used for partial updates at property collection level</remarks>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="original">The original entity</param>
        /// <param name="destination">The new entity</param>
        protected static void CopyPropertiesFromOriginal<TEntity>(TEntity original, ref TEntity destination)
            where TEntity : IEntity
        {
            // TODO: Assert the two are versions of the same entity somehow?
            if (original.EntityCategory != destination.EntityCategory)
            {
                throw new ArgumentException(
                    "Original and destination entities must be of the same EntityCategory (original: {0} destination: {1})"
                    .FormatInvariant(original, destination),
                    "destination");
            }

            // If properties were not sent, copy from original (partial update)
            if (original.Properties.Any(p => p.IsDefaultProperty)
                && destination.Properties.Count(p => p.IsDefaultProperty) == 0)
            {
                destination.Properties.Add(original.Properties.Where(p => p.IsDefaultProperty));
            }

            // If system properties were not sent, copy from original (partial update)
            if (original.Properties.Any(p => p.IsSystemProperty) 
                && destination.Properties.Count(p => p.IsSystemProperty) == 0)
            {
                destination.Properties.Add(original.Properties.Where(p => p.IsSystemProperty));
            }
        }
    }
}
