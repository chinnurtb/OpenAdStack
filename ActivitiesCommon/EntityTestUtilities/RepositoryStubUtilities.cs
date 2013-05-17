// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RepositoryStubUtilities.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DataAccessLayer;
using Rhino.Mocks;

namespace EntityTestUtilities
{
    /// <summary>Utility class for setting up repository stubs.</summary>
    public static class RepositoryStubUtilities
    {
        /// <summary>Set up a stub for GetEntity.</summary>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="entityId">The entity id - required.</param>
        /// <param name="entity">The entity to return - can be null if 'fail' is true.</param>
        /// <param name="fail">True for stub to throw.</param>
        public static void SetupGetEntityStub(IEntityRepository repositoryStub, EntityId entityId, IEntity entity, bool fail)
        {
            if (fail)
            {
                repositoryStub.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(entityId)))
                    .Throw(new DataAccessEntityNotFoundException());
            }
            else
            {
                repositoryStub.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Equal(entityId)))
                    .Return(entity);
            }
        }

        /// <summary>Set up a stub for GetEntity that matches and entity filter as well as Id.</summary>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="entityFilter">The entity filter to match</param>
        /// <param name="entityId">The entity id - required.</param>
        /// <param name="entity">The entity to return - can be null if 'fail' is true.</param>
        /// <param name="fail">True for stub to throw.</param>
        public static void SetupGetEntityStub(IEntityRepository repositoryStub, IEntityFilter entityFilter, EntityId entityId, IEntity entity, bool fail)
        {
            Func<RequestContext, IEntityFilter, bool> matchExpression = (ctx, filter) => 
                    filter == null
                    ? ctx.EntityFilter == null
                    : ctx.EntityFilter != null && ctx.EntityFilter.Filters.SequenceEqual(filter.Filters)
                        && ctx.EntityFilter.EntityQueries.QueryStringParams.SequenceEqual(filter.EntityQueries.QueryStringParams); 

            if (fail)
            {
                repositoryStub.Stub(f => f.GetEntity(
                    Arg<RequestContext>.Matches(c => matchExpression(c, entityFilter)),
                    Arg<EntityId>.Is.Equal(entityId)))
                    .Throw(new DataAccessEntityNotFoundException());
            }
            else
            {
                repositoryStub.Stub(f => f.GetEntity(
                    Arg<RequestContext>.Matches(c => matchExpression(c, entityFilter)),
                    Arg<EntityId>.Is.Equal(entityId)))
                    .Return(entity);
            }
        }

        /// <summary>Set up a stub for SaveEntity.</summary>
        /// <typeparam name="T">Type if IEntity</typeparam>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="captureEntity">
        /// A lambda expression to capture the entity being saved.
        /// e => { } will do nothing.
        /// e => { myvar = e } will capture the entity where 'myvar' is of type T
        /// </param>
        /// <param name="fail">True for stub to fail.</param>
        public static void SetupSaveEntityStub<T>(IEntityRepository repositoryStub, Action<T> captureEntity, bool fail) where T : IEntity
        {
            SetupSaveEntityStub<T>(repositoryStub, (c, e) => captureEntity(e), fail);
        }

        /// <summary>Set up a stub for SaveEntity.</summary>
        /// <typeparam name="T">Type if IEntity</typeparam>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="captureArgs">
        /// A lambda expression to capture the entity being saved and the RequestContext.
        /// c, e => { } will do nothing.
        /// c, e => { myctx = c, myvar = e } will capture the context and entity where 'myvar' is of type T
        /// </param>
        /// <param name="fail">True for stub to fail.</param>
        public static void SetupSaveEntityStub<T>(
            IEntityRepository repositoryStub, Action<RequestContext, T> captureArgs, bool fail) where T : IEntity
        {
            if (fail)
            {
                repositoryStub.Stub(f => f.SaveEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<IEntity>.Is.Anything))
                    .Throw(new DataAccessException());
            }
            else
            {
                repositoryStub.Stub(f => f.SaveEntity(
                    Arg<RequestContext>.Is.Anything,
                    Arg<IEntity>.Is.Anything))
                    .WhenCalled(call => captureArgs((RequestContext)call.Arguments[0], (T)call.Arguments[1]));
            }
        }

        /// <summary>Set up a stub for SaveUser.</summary>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="captureArgs">
        /// A lambda expression to capture the entity being saved and the RequestContext.
        /// c, e => { } will do nothing.
        /// c, e => { myctx = c, myvar = e } will capture the context and entity where 'myvar' is of type T
        /// </param>
        /// <param name="fail">True for stub to fail.</param>
        public static void SetupSaveUserStub(
            IEntityRepository repositoryStub, Action<RequestContext, UserEntity> captureArgs, bool fail)
        {
            if (fail)
            {
                repositoryStub.Stub(f => f.SaveUser(
                    Arg<RequestContext>.Is.Anything,
                    Arg<UserEntity>.Is.Anything))
                    .Throw(new DataAccessException());
            }
            else
            {
                repositoryStub.Stub(f => f.SaveUser(
                    Arg<RequestContext>.Is.Anything,
                    Arg<UserEntity>.Is.Anything))
                    .WhenCalled(call => captureArgs((RequestContext)call.Arguments[0], (UserEntity)call.Arguments[1]));
            }
        }

        /// <summary>Set up a stub for GetUser.</summary>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="userId">The user id - required.</param>
        /// <param name="user">The user to return - can be null if 'fail' is true.</param>
        /// <param name="fail">True for stub to throw.</param>
        public static void SetupGetUserStub(IEntityRepository repositoryStub, string userId, UserEntity user, bool fail)
        {
            if (fail)
            {
                repositoryStub.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Equal(userId)))
                    .Throw(new DataAccessEntityNotFoundException());
            }
            else
            {
                repositoryStub.Stub(f => f.GetUser(Arg<RequestContext>.Is.Anything, Arg<string>.Is.Equal(userId)))
                    .Return(user);
            }
        }

        /// <summary>Set up a stub for TrySaveEntity.</summary>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="entityFilter">The entity filter to match</param>
        /// <param name="entityId">The entity id of the entity being updated.</param>
        /// <param name="captureProperties">
        /// A lambda expression to capture the properties being saved.
        /// e => { } will do nothing.
        /// e => { myvar = e } will capture the properties 
        /// where 'myvar' is of type IEnumerable&lt;EntityProperty&gt;
        /// </param>
        /// <param name="fail">True for stub to fail.</param>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Action only used in test helper.")]
        public static void SetupTryUpdateEntityStub(IEntityRepository repositoryStub, IEntityFilter entityFilter, EntityId entityId, Action<IEnumerable<EntityProperty>> captureProperties, bool fail)
        {
            repositoryStub.Stub(f => f.TryUpdateEntity(
                Arg<RequestContext>.Matches(
                    c => entityFilter == null
                        ? c.EntityFilter == null
                        : c.EntityFilter.Filters.SequenceEqual(entityFilter.Filters)),
                Arg<EntityId>.Is.Equal(entityId),
                Arg<IEnumerable<EntityProperty>>.Is.Anything))
                .Return(!fail)
                .WhenCalled(call => captureProperties(((IEnumerable<EntityProperty>)call.Arguments[2]).ToList()));
        }

        /// <summary>Set up a stub for TrySaveEntity.</summary>
        /// <param name="repositoryStub">The repository stub.</param>
        /// <param name="entityId">The entity id of the entity being updated.</param>
        /// <param name="captureProperties">
        /// A lambda expression to capture the properties being saved.
        /// e => { } will do nothing.
        /// e => { myvar = e } will capture the properties 
        /// where 'myvar' is of type IEnumerable&lt;EntityProperty&gt;
        /// </param>
        /// <param name="fail">True for stub to fail.</param>
        [SuppressMessage("Microsoft.Design", "CA1006", Justification = "Action only used in test helper.")]
        public static void SetupTryUpdateEntityStub(IEntityRepository repositoryStub, EntityId entityId, Action<IEnumerable<EntityProperty>> captureProperties, bool fail)
        {
            repositoryStub.Stub(f => f.TryUpdateEntity(
                Arg<RequestContext>.Is.Anything,
                Arg<EntityId>.Is.Equal(entityId),
                Arg<IEnumerable<EntityProperty>>.Is.Anything))
                .Return(!fail)
                .WhenCalled(call => captureProperties(((IEnumerable<EntityProperty>)call.Arguments[2]).ToList()));
        }
    }
}