//-----------------------------------------------------------------------
// <copyright file="DfpActivityFixtureBase.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Activities;
using DataAccessLayer;
using Diagnostics;
using Diagnostics.Testing;
using Google.Api.Ads.Common.Util;
using Google.Api.Ads.Dfp.Lib;
using GoogleDfpActivities;
using GoogleDfpClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using ScheduledActivities;
using Utilities.Storage.Testing;
using Dfp = Google.Api.Ads.Dfp.v201206;

namespace GoogleDfpIntegrationTests
{
    /// <summary>Base class for DfpActivity fixtures</summary>
    /// <typeparam name="TDfpActivity">Type of the DfpActivity being tested</typeparam>
    public class DfpActivityFixtureBase<TDfpActivity>
        where TDfpActivity : DfpActivity
    {
        /// <summary>List of entities for the mock repository</summary>
        private IList<IEntity> entities;

        /// <summary>Gets the test client instance as a GoogleDfpWrapper</summary>
        internal GoogleDfpWrapper Wrapper
        {
            get { return this.DfpClient as GoogleDfpWrapper; }
        }

        /// <summary>Gets or sets the test IGoogleDfpClient instance</summary>
        protected IGoogleDfpClient DfpClient { get; set; }

        /// <summary>Gets the mock entity repository for testing</summary>
        protected IEntityRepository RepositoryMock { get; private set; }

        /// <summary>Gets the list of requests submitted during the current test</summary>
        protected IList<ActivityRequest> SubmittedRequests { get; private set; }

        /// <summary>Gets the test logger</summary>
        protected TestLogger TestLogger { get; private set; }

        /// <summary>Gets an alpha numeric string unique to each test case</summary>
        protected string UniqueId { get; private set; }

        /// <summary>Initialize per-test object(s)/settings</summary>
        public virtual void TestInitialize()
        {
            ConfigurationManager.AppSettings["GoogleDfp.NetworkId"] = TestNetwork.NetworkId.ToString(CultureInfo.InvariantCulture);
            ConfigurationManager.AppSettings["GoogleDfp.Username"] = TestNetwork.Username;
            ConfigurationManager.AppSettings["GoogleDfp.Password"] = TestNetwork.Password;
            ConfigurationManager.AppSettings["GoogleDfp.NetworkTimezone"] = "Pacific Standard Time";
            ConfigurationManager.AppSettings["GoogleDfp.ReportFrequency"] = "01:00:00";
            
            Scheduler.Registries = null;
            SimulatedPersistentDictionaryFactory.Initialize();

            this.TestLogger = new TestLogger();
            LogManager.Initialize(new[] { this.TestLogger });
            
            this.DfpClient = new GoogleDfpWrapper();
            this.InitializeMockRepository();
            this.SubmittedRequests = new List<ActivityRequest>();
            
            this.UniqueId = Guid.NewGuid().ToString("N");
        }

        /// <summary>Creates a DfpActivity instance</summary>
        /// <returns>The activity instance</returns>
        protected TDfpActivity CreateActivity()
        {
            IDictionary<Type, object> context = new Dictionary<Type, object>
            {
                { typeof(IEntityRepository), this.RepositoryMock }
            };
            return Activity.CreateActivity(typeof(TDfpActivity), context, this.SubmitActivityRequest) as TDfpActivity;
        }

        /// <summary>Adds entities to the mock repository</summary>
        /// <param name="entities">Entities to add</param>
        protected void AddEntitiesToMockRepository(params IEntity[] entities)
        {
            this.entities.Add(entities);
        }

        /// <summary>Test submit activity request handler</summary>
        /// <param name="request">The request</param>
        /// <param name="sourceName">The source name</param>
        /// <returns>True if successful; otherwise, false.</returns>
        private bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            this.SubmittedRequests.Add(request);
            return true;
        }
 
        /// <summary>Initialize the entity repository mock</summary>
        private void InitializeMockRepository()
        {
            this.entities = new List<IEntity>();
            this.RepositoryMock = MockRepository.GenerateMock<IEntityRepository>();
            this.RepositoryMock.Stub(f =>
                f.TryGetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Anything))
                .WhenCalled(call =>
                {
                    var entityId = call.Arguments[1] as EntityId;
                    call.ReturnValue = this.entities
                        .SingleOrDefault(e =>
                            e.ExternalEntityId.ToString() == entityId.ToString());
                });
            this.RepositoryMock.Stub(f =>
                f.GetEntitiesById(Arg<RequestContext>.Is.Anything, Arg<EntityId[]>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var entityIds = ((EntityId[])call.Arguments[1]).Select(e => e.ToString());
                    call.ReturnValue = new HashSet<IEntity>(
                        this.entities
                            .Where(e =>
                                entityIds.Contains(e.ExternalEntityId.ToString())));
                });
        }
    }
}