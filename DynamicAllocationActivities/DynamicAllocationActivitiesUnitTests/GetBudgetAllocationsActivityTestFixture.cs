// -----------------------------------------------------------------------
// <copyright file="GetBudgetAllocationsActivityTestFixture.cs" company="Rare Crowds Inc">
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

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Activities;
using ActivityTestUtilities;
using DataAccessLayer;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationUtilities;
using EntityTestUtilities;
using EntityUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using ScheduledActivities;
using Utilities.Serialization;
using Utilities.Storage.Testing;

namespace DynamicAllocationActivitiesUnitTests
{
    /// <summary>
    /// This is a test class for GetBudgetAllocationsActivity.
    /// It is intended to contain for things other than the full activity tests contained in 
    /// BudgetAllocationsTestFixture and BudgetReallocationsTestFixture
    /// </summary>
    [TestClass]
    public class GetBudgetAllocationsActivityTestFixture
    {
        /// <summary>dynamic allocation engine used in test setup</summary>
        private DynamicAllocationEngine dynamicAllocationEngine;

        /// <summary>a set of test ValuationsInputs</summary>
        private ValuationInputs testValuationsInputs;

        /// <summary>a test CampaignDefinition</summary>
        private CampaignDefinition testCampaignDefinition;

        /// <summary>a set of test Valuations</summary>
        private IDictionary<MeasureSet, decimal> testValuations;

        /// <summary>a set of test AllocationParameters</summary>
        private AllocationParameters testAllocationParameters;

        /// <summary>a set of test BudgetAllocation</summary>
        private BudgetAllocation newTestBudgetAllocation;

        /// <summary>a set of test BudgetAllocation</summary>
        private BudgetAllocation oldTestBudgetAllocation;

        /// <summary>
        /// Mock entity repository used for tests
        /// </summary>
        private IEntityRepository repository;

        /// <summary>Activity for testing</summary>
        private GetBudgetAllocationsActivity testActivity;

        /// <summary>Campaign for testing</summary>
        private CampaignEntity testCampaignEntity;

        /// <summary>Company for testing</summary>
        private CompanyEntity testCompanyEntity;

        /// <summary>ActiveAllocation entity ID</summary>
        private EntityId activeAllocationEntityId;

        /// <summary>
        /// Gets the TimeSlottedRegistry for campaigns to reallocate
        /// </summary>
        private static TimeSlottedRegistry<Tuple<string, DateTime, bool>> CampaignsToReallocate
        {
            get
            {
                return Scheduler.GetRegistry<Tuple<string, DateTime, bool>>(
                    DynamicAllocationActivitySchedulerRegistries.CampaignsToReallocate);
            }
        }

        /// <summary>
        /// Initialize simulated storage before each test
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "00:00:00|04:00:00";
            ConfigurationManager.AppSettings["DynamicAllocation.UpdateAllocationsRequestExpiry"] = "01:00:00";
            ConfigurationManager.AppSettings["DynamicAllocation.CleanupCampaignsRequestExpiry"] = "01:00:00";

            // Setup simulated persistent storage
            Scheduler.Registries = null;
            SimulatedPersistentDictionaryFactory.Initialize();

            this.testCampaignEntity = EntityTestHelpers.CreateTestCampaignEntity(
                new EntityId(), "Test Campaign", 1000, DateTime.UtcNow, DateTime.UtcNow, "Test Persona");

            this.testCompanyEntity = EntityTestHelpers.CreateTestCompanyEntity(new EntityId(), "Test Company");

            // Inialize allocation parameters on the test campaign
            TestUtilities.AllocationParametersDefaults.Initialize();
            this.testAllocationParameters = new AllocationParameters();

            var measureInputs = new MeasureSetsInput
                {
                    Measures =
                        new List<MeasuresInput>
                            {
                                new MeasuresInput { Measure = 1106006, Valuation = 100 },
                                new MeasuresInput { Measure = 1106007, Valuation = 50 },
                                new MeasuresInput { Measure = 1106008, Valuation = 75 },
                            },
                    MaxValuation = 10m,
                    IdealValuation = 5m,
                };

            this.testValuationsInputs = new ValuationInputs(measureInputs, null);
            this.testCampaignDefinition = this.testValuationsInputs.CreateCampaignDefinition();

            var measureMap = new MeasureMap(new[]
                {
                    new EmbeddedJsonMeasureSource(
                        Assembly.GetExecutingAssembly(),
                        "DynamicAllocationActivitiesUnitTests.Resources.MeasureMap.js")
                });

            this.dynamicAllocationEngine = new DynamicAllocationEngine(measureMap);

            this.testValuations = this.dynamicAllocationEngine.GetValuations(this.testCampaignDefinition);

            this.oldTestBudgetAllocation = new BudgetAllocation
            {
                PerNodeResults = this.testValuations.ToDictionary(
                    v => v.Key, 
                    v => new PerNodeBudgetAllocationResult 
                    { 
                        Valuation = v.Value,
                        PeriodMediaBudget = 0m,
                        PeriodTotalBudget = 0m,
                        PeriodImpressionCap = 1,
                    }),
                NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                CampaignStart = new DateTime(2011, 12, 28),
                CampaignEnd = new DateTime(2011, 12, 31),
                RemainingBudget = 2000,
                TotalBudget = 2000,
                AllocationParameters = this.testAllocationParameters,
                PeriodStart = new DateTime(2011, 12, 28),
                PeriodDuration = new TimeSpan(1, 0, 0, 0), // one day
            };

            this.newTestBudgetAllocation = this.dynamicAllocationEngine.GetBudgetAllocations(this.oldTestBudgetAllocation);

            // add export counts to this.oldTestBudgetAllocation as if the export has happened
            this.oldTestBudgetAllocation = this.dynamicAllocationEngine.IncrementExportCounts(
                this.oldTestBudgetAllocation,
                this.oldTestBudgetAllocation.PerNodeResults.Select(pnr => pnr.Key));

            this.activeAllocationEntityId = new EntityId();
            this.testCampaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = DynamicAllocationEntityProperties.AllocationSetActive,
                TargetEntityCategory = BlobEntity.CategoryName,
                TargetEntityId = this.activeAllocationEntityId,
                TargetExternalType = "???"
            });

            this.repository = MockRepository.GenerateStub<IEntityRepository>();

            this.testActivity = Activity.CreateActivity(
                typeof(GetBudgetAllocationsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest)
                as GetBudgetAllocationsActivity;
        }

        /// <summary>Test that the next allocation is scheduled correctly</summary>
        [TestMethod]
        public void ScheduleNextReallocation()
        {
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "00:00:00";

            var now = DateTime.UtcNow;
            var today = DateTime.UtcNow.Date;
            var expectedNextReallocation = today.AddDays(1);

            // Setup test objects
            this.testCampaignEntity.StartDate = today.AddHours(DateTime.UtcNow.Hour);
            this.testCampaignEntity.EndDate = today.AddDays(7);

            // Schedule the next reallocation
            var actual = GetBudgetAllocationsActivity.ScheduleNextReallocation(
                new DynamicAllocationCampaign(null, this.testCompanyEntity, this.testCampaignEntity), 
                false, // !immediate
                ReallocationScheduleType.RegularReallocation,
                now);

            Assert.AreEqual(expectedNextReallocation, actual.Date);
            Assert.AreEqual(0, CampaignsToReallocate[now].Count);
            Assert.AreEqual(1, CampaignsToReallocate[now.AddDays(1)].Count);
        }

        /// <summary>
        /// Test that the next allocation is scheduled correctly when that allocation
        /// would be the second of the day.
        /// </summary>
        [TestMethod]
        public void ScheduleSecondReallocationForDay()
        {
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "00:00:00|04:00:00";
            var now = DateTime.UtcNow;

            // Setup times
            var campaignStart = now.AddHours(-2);
            var expectedNextReallocation = campaignStart.AddHours(4);
            if (campaignStart.Date < now.Date)
            {
                // Campaign started yesterday, expected next reallocation is campaign start time
                expectedNextReallocation = now.Date + campaignStart.TimeOfDay;
            }

            // Setup test objects
            this.testCampaignEntity.StartDate = campaignStart;
            this.testCampaignEntity.EndDate = campaignStart.AddDays(7);
            this.testCampaignEntity.SetInitializationPhaseComplete(true);
            
            // Schedule the next reallocation
            var actual = GetBudgetAllocationsActivity.ScheduleNextReallocation(
                new DynamicAllocationCampaign(null, this.testCompanyEntity, this.testCampaignEntity),
                false, // !immediate
                ReallocationScheduleType.RegularReallocation,
                now);

            Assert.AreEqual(expectedNextReallocation, actual);
            Assert.AreEqual(0, CampaignsToReallocate[now].Count);
            Assert.AreEqual(0, CampaignsToReallocate[expectedNextReallocation.AddHours(-1)].Count);
            Assert.AreEqual(1, CampaignsToReallocate[expectedNextReallocation].Count);
        }

        /// <summary>
        /// Test that the next allocation is scheduled correctly when that allocation
        /// would be the second of the day.
        /// </summary>
        [TestMethod]
        public void ScheduleNextReallocationTomorrow()
        {
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "00:00:00|04:00:00";

            // Setup times
            var now = DateTime.UtcNow;
            var campaignStart = now.AddHours(-6);
            var expectedNextReallocation = campaignStart.AddDays(1);

            // Setup test objects
            this.testCampaignEntity.StartDate = campaignStart;
            this.testCampaignEntity.EndDate = campaignStart.AddDays(7);
            this.testCampaignEntity.SetInitializationPhaseComplete(true);

            // Schedule the next reallocation
            var actual = GetBudgetAllocationsActivity.ScheduleNextReallocation(
                new DynamicAllocationCampaign(null, this.testCompanyEntity, this.testCampaignEntity),
                false, // !immediate
                ReallocationScheduleType.RegularReallocation,
                now);

            Assert.AreEqual(expectedNextReallocation, actual);
            Assert.AreEqual(0, CampaignsToReallocate[DateTime.UtcNow].Count);
            Assert.AreEqual(0, CampaignsToReallocate[expectedNextReallocation.AddHours(-1)].Count);
            Assert.AreEqual(1, CampaignsToReallocate[expectedNextReallocation].Count);
        }

        /// <summary>Test that the next allocation is scheduled correctly</summary>
        [TestMethod]
        public void NextReallocationNotScheduledAfterEndDate()
        {
            // Setup test objects
            var startDate = DateTime.UtcNow.Date.AddDays(-7).AddHours(12);
            var endDate = DateTime.UtcNow.Date.AddHours(6);
            this.testCampaignEntity.StartDate = startDate;
            this.testCampaignEntity.EndDate = endDate;

            // Should not schedule the next reallocation
            var actual = GetBudgetAllocationsActivity.ScheduleNextReallocation(
                new DynamicAllocationCampaign(null, this.testCompanyEntity, this.testCampaignEntity),
                false, // !immediate
                ReallocationScheduleType.RegularReallocation,
                DateTime.UtcNow);
           
            Assert.AreEqual(endDate, actual);
            Assert.AreEqual(0, CampaignsToReallocate[DateTime.UtcNow].Count);
            Assert.AreEqual(0, CampaignsToReallocate[DateTime.UtcNow.AddDays(1)].Count);
        }

        /// <summary>
        /// Test scheduling the first reallocation after initialization phase
        /// </summary>
        [TestMethod]
        public void ScheduleFirstReallocation()
        {
            // Setup times
            var now = DateTime.UtcNow;
            var campaignStart = now.AddHours(-6);

            // When scheduling allocation before initialization phase has completed,
            // the next reallocation should be after the restarted initialization phase.
            var expectedNextReallocation =
                now + this.testAllocationParameters.InitialAllocationTotalPeriodDuration;

            // Setup test objects
            this.testCampaignEntity.StartDate = campaignStart;
            this.testCampaignEntity.EndDate = campaignStart.AddDays(7);

            // Schedule the next reallocation
            var actual = GetBudgetAllocationsActivity.ScheduleNextReallocation(
                new DynamicAllocationCampaign(null, this.testCompanyEntity, this.testCampaignEntity),
                false, // !immediate
                ReallocationScheduleType.FirstReallocation,
                now);

            Assert.AreEqual(expectedNextReallocation, actual);
            Assert.AreEqual(0, CampaignsToReallocate[now].Count);
            Assert.AreEqual(0, CampaignsToReallocate[expectedNextReallocation.AddHours(-1)].Count);
            Assert.AreEqual(1, CampaignsToReallocate[expectedNextReallocation].Count);
        }

        /// <summary>
        /// Test that allocation period start dates prior to the campaign
        /// start date are correctly snapped to the campaign start date
        /// </summary>
        [TestMethod]
        public void AllocationPeriodStartSnapsToCampaignStartDate()
        {
            // Setup test objects
            this.testCampaignEntity.StartDate = DateTime.UtcNow.Date.AddDays(2);
            this.testCampaignEntity.EndDate = DateTime.UtcNow.Date.AddDays(10);

            // Should not schedule the next reallocation
            var actual = GetBudgetAllocationsActivity.ScheduleNextReallocation(
                new DynamicAllocationCampaign(null, this.testCompanyEntity, this.testCampaignEntity),
                false, // !immediate
                ReallocationScheduleType.Initial,
                DateTime.UtcNow);
            var reallocationSlots = CampaignsToReallocate.GetTimeSlotKeys();
            var expectedTimeSlotKey = Scheduler.GetTimeSlotKey(this.testCampaignEntity.StartDate);
            Assert.IsTrue(reallocationSlots.Contains(expectedTimeSlotKey));
            var scheduledReallocations = CampaignsToReallocate[DateTime.UtcNow.AddDays(2)];
            Assert.AreEqual(1, scheduledReallocations.Count);
            var scheduleEntry = scheduledReallocations[0];
            var allocationPeriodStart = scheduleEntry.Item3.Item2;
            Assert.AreEqual<DateTime>(this.testCampaignEntity.StartDate, allocationPeriodStart);
            Assert.AreEqual((DateTime)this.testCampaignEntity.StartDate, actual);
        }

        /// <summary>
        /// Test for CreateListOfAllocationIdsToExport
        /// </summary>
        [TestMethod]
        public void CreateListOfAllocationIdsToExport()
        {
            var budgetAllocationOutputs = new BudgetAllocation
            {
                PerNodeResults = Enumerable.Range(1, 1000)
                .ToDictionary(
                    i => new MeasureSet(new[] { (long)i }),
                    i => new PerNodeBudgetAllocationResult
                    {
                        ExportBudget = 1,
                        AllocationId = Guid.NewGuid().ToString("N")
                    })
            };

            var expected  = string.Join(",", budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value.AllocationId));
            var actual = GetBudgetAllocationsActivity.CreateAllocationIdsString(budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value).ToList());

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// Test for CreateListsOfAllocationdsToExport
        /// </summary>
        [TestMethod]
        public void CreateListsOfAllocationIdsToExport()
        {
            var budgetAllocationOutputs = new BudgetAllocation
            {
                PerNodeResults = Enumerable.Range(1, 1000)
                .ToDictionary(
                    i => new MeasureSet(new[] { (long)i }), 
                    i => new PerNodeBudgetAllocationResult
                    {
                        ExportBudget = 1,
                        AllocationId = Guid.NewGuid().ToString("N")
                    })
            };

            var expected = new[] 
            {
                string.Join(",", budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value.AllocationId).Take(250)),
                string.Join(",", budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value.AllocationId).Skip(250).Take(250)),
                string.Join(",", budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value.AllocationId).Skip(500).Take(250)),
                string.Join(",", budgetAllocationOutputs.PerNodeResults.Select(pnr => pnr.Value.AllocationId).Skip(750).Take(250))
            };

            var actual = GetBudgetAllocationsActivity.CreateListsOfMeasureSetsToExport(budgetAllocationOutputs, 4)
                .Select(
                msl => GetBudgetAllocationsActivity.CreateAllocationIdsString(
                    budgetAllocationOutputs
                        .PerNodeResults
                        .Where(pnr => msl.ContainsKey(pnr.Key))
                        .Select(pnr => pnr.Value)
                        .ToList()));

            Assert.IsTrue(expected.SequenceEqual(actual));
        }

        /// <summary>
        /// test for AddBudgetAllocationsOutputToHistory when there is an index already
        /// </summary>
        [TestMethod]
        public void AddBudgetAllocationsOutputToHistory()
        {
            var allocationStartTime = DateTime.UtcNow;
            var oldOutputsEntityId = new EntityId();
            var index = new List<HistoryElement> 
            { 
                new HistoryElement 
                { 
                    AllocationStartTime = allocationStartTime.ToString("o"), 
                    AllocationOutputsId = oldOutputsEntityId.ToString()
                } 
            };
            var indexJson = AppsJsonSerializer.SerializeObject(index);
            var indexEntityId = new EntityId();
            var indexBlob = BlobEntity.BuildBlobEntity(indexEntityId, indexJson);
            var newOutputsEntityId = new EntityId();

            this.repository = MockRepository.GenerateMock<IEntityRepository>();
            this.repository.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Anything))
                .Return(null)
                .WhenCalled(call =>
                {
                    var blobId = (EntityId)call.Arguments[1];

                    if (blobId == indexEntityId)
                    {
                        call.ReturnValue = indexBlob;
                    }
                });

            this.testCampaignEntity.StartDate = this.oldTestBudgetAllocation.CampaignStart;
            this.testCampaignEntity.EndDate = this.oldTestBudgetAllocation.CampaignEnd;

            this.testCampaignEntity.Associations.Add(new Association
            {
                AssociationType = AssociationType.Relationship,
                ExternalName = DynamicAllocationEntityProperties.AllocationHistoryIndex,
                TargetEntityCategory = BlobEntity.CategoryName,
                TargetEntityId = indexEntityId,
                TargetExternalType = "???"
            });

            IEntity newIndexBlob = null;
            Action<IEntity> saveSideEffect = e =>
            {
                if ((string)e.ExternalName == DynamicAllocationEntityProperties.AllocationHistoryIndex)
                {
                    newIndexBlob = e;
                }
            };

            RepositoryStubUtilities.SetupSaveEntityStub(this.repository, saveSideEffect, false);

            var activity = Activity.CreateActivity(
                typeof(GetBudgetAllocationsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest)
                as GetBudgetAllocationsActivity;
            Assert.IsNotNull(activity);

            activity.AddBudgetAllocationsOutputToHistory(
                DynamicAllocationActivity.CreateContext(
                new ActivityRequest
                {
                    Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                    Values =
                    {
                        { EntityActivityValues.AuthUserId, new EntityId() },
                        { EntityActivityValues.CompanyEntityId, new EntityId() },
                        { EntityActivityValues.CampaignEntityId, new EntityId() },
                        { DynamicAllocationActivityValues.AllocationStartDate, DateTime.UtcNow.ToString("o") },
                    }
                }),
                this.testCampaignEntity,
                allocationStartTime.AddHours(4),
                newOutputsEntityId);

            var newIndexJson = ((BlobEntity)newIndexBlob).DeserializeBlob<string>();
            var newIndex = AppsJsonSerializer.DeserializeObject<List<HistoryElement>>(newIndexJson);
            Assert.AreEqual(2, newIndex.Count);
            Assert.AreEqual(oldOutputsEntityId, new EntityId(newIndex[1].AllocationOutputsId));
            Assert.AreEqual(newOutputsEntityId, new EntityId(newIndex[0].AllocationOutputsId));
            Assert.AreEqual(
                allocationStartTime.AddHours(4),
                DateTime.Parse(newIndex[0].AllocationStartTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
            Assert.AreEqual(
                allocationStartTime,
                DateTime.Parse(newIndex[1].AllocationStartTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        /// <summary>
        /// test for AddBudgetAllocationsOutputToHistory when there isn't an index already
        /// </summary>
        [TestMethod]
        public void AddBudgetAllocationsOutputToHistoryNoIndex()
        {
            var allocationStartTime = DateTime.UtcNow;
            var newOutputsEntityId = new EntityId();
            this.repository = MockRepository.GenerateMock<IEntityRepository>();
          
            this.testCampaignEntity.StartDate = this.oldTestBudgetAllocation.CampaignStart;
            this.testCampaignEntity.EndDate = this.oldTestBudgetAllocation.CampaignEnd;

            IEntity newIndexBlob = null;
            Action<IEntity> saveSideEffect = e =>
            {
                if ((string)e.ExternalName == DynamicAllocationEntityProperties.AllocationHistoryIndex)
                {
                    newIndexBlob = e;
                }
            };

            RepositoryStubUtilities.SetupSaveEntityStub(this.repository, saveSideEffect, false);

            var activity = Activity.CreateActivity(
                typeof(GetBudgetAllocationsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.repository } },
                ActivityTestHelpers.SubmitActivityRequest)
                as GetBudgetAllocationsActivity;
            Assert.IsNotNull(activity);

            activity.AddBudgetAllocationsOutputToHistory(
                DynamicAllocationActivity.CreateContext(
                new ActivityRequest
                {
                    Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                    Values =
                    {
                        { EntityActivityValues.AuthUserId, new EntityId() },
                        { EntityActivityValues.CompanyEntityId, new EntityId() },
                        { EntityActivityValues.CampaignEntityId, new EntityId() },
                        { DynamicAllocationActivityValues.AllocationStartDate, DateTime.UtcNow.ToString("o") },
                    }
                }),
                this.testCampaignEntity,
                allocationStartTime,
                newOutputsEntityId);

            var newIndexJson = ((BlobEntity)newIndexBlob).DeserializeBlob<string>();
            var newIndex = AppsJsonSerializer.DeserializeObject<List<HistoryElement>>(newIndexJson);
            Assert.AreEqual(1, newIndex.Count);
            Assert.AreEqual(newOutputsEntityId, new EntityId(newIndex[0].AllocationOutputsId));
            Assert.AreEqual(
                allocationStartTime,
                DateTime.Parse(newIndex[0].AllocationStartTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        ////FindNextReallocation

        /// <summary>
        /// test for FindNextReallocation for intial allocations
        /// </summary>
        [TestMethod]
        public void FindNextReallocationInitial()
        {
            var campaign = EntityJsonSerializer.DeserializeCampaignEntity(new EntityId(), string.Empty);
            campaign.StartDate = DateTime.UtcNow.AddDays(3);
            var actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, DateTime.UtcNow);
            Assert.AreEqual((DateTime)campaign.StartDate, actual);
        }

        /// <summary>
        /// Test reallocations are correctly scheduled per the config when the campaign starts at midnight
        /// </summary>
        [TestMethod]
        public void FindNextReallocationScheduleFromMidnight()
        {
            // Schedule for start time-of-day +7, 15 and 23 hours
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "07:00:00|15:00:00|23:00:00";
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Campaign started 3 days ago at midnight
            var campaign = EntityJsonSerializer.DeserializeCampaignEntity(new EntityId(), string.Empty);
            campaign.StartDate = today.AddDays(-3);

            // Begin test 12 minutes after what should've been the first realloc of the day
            var now = today + TimeSpan.FromHours(7.02);

            // Next scheduled realloc should be at 15:00Z
            var expected = today.AddHours(15);
            var actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, now);
            Assert.AreEqual(expected, actual);

            // After that the next scheduled realloc should be at 23:00Z
            now = today + TimeSpan.FromHours(15.1); // 6 minutes after the 15:00Z reallocation has started
            expected = today.AddHours(23);
            actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, now);
            Assert.AreEqual(expected, actual);

            // After the 23:00Z reallocation the next should be at 07:00Z "tomorrow"
            now = today + TimeSpan.FromHours(23.15); // 9 minutes after the 23:00Z reallocation has started
            expected = tomorrow.AddHours(7);
            actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, now);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Test reallocations are correctly scheduled per the config when the campaign starts at midnight
        /// </summary>
        /// <remarks>This behavior is expected, however, it is probably not correct.</remarks>
        [TestMethod]
        public void FindNextReallocationScheduleFrom0700Eastern()
        {
            // Schedule for start time-of-day +7, 15 and 23 hours
            // This results in a schedule of 19:00Z, 03:00Z, 11:00Z
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "07:00:00|15:00:00|23:00:00";
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Campaign started 3 days ago at 7am EST (12:00Z)
            var campaign = EntityJsonSerializer.DeserializeCampaignEntity(new EntityId(), string.Empty);
            campaign.StartDate = today.AddDays(-3).AddHours(12);

            // Begin test 12 minutes after what should've been the first realloc of the day (UTC)
            // campaign.StartDate.TimeOfDay + 15:00 = 03:00Z
            var now = today + TimeSpan.FromHours(3.02);

            // Next scheduled realloc should be at 19:00Z
            // campaign.StartDate.TimeOfDay + 07:00 = 19:00Z
            var expected = today.AddHours(19);
            var actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, now);
            Assert.AreEqual(expected, actual);

            // After that the next scheduled realloc should be at 19:00Z
            // campaign.StartDate.TimeOfDay + 15:00 = 03:00Z
            now = today + TimeSpan.FromHours(19.1); // 6 minutes after the 15:00Z reallocation has started
            expected = tomorrow.AddHours(3);
            actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, now);
            Assert.AreEqual(expected, actual);

            // After another 03:00Z reallocation it goes back to 19:00Z. 11:00Z never gets scheduled.
            now = tomorrow + TimeSpan.FromHours(3.15); // 9 minutes after the 03:00Z reallocation has started
            expected = tomorrow.AddHours(19);
            actual = GetBudgetAllocationsActivity.FindNextReallocation(campaign, now);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// test for the AddAllocationIdToAllocations method
        /// </summary>
        [TestMethod]
        public void AddAllocationIdToAllocationsTest()
        {
            // set up the allocationNodeMap to contain one value. We will make sure it gets reused.
            var testAllocationId = Guid.NewGuid().ToString("N");
            var testMeasureSet = this.newTestBudgetAllocation.PerNodeResults.Keys.First();
            var allocationNodeMap = new Dictionary<string, MeasureSet>
            {
                { testAllocationId, testMeasureSet }
            };

            GetBudgetAllocationsActivity.AddAllocationIdToAllocations(ref this.newTestBudgetAllocation, ref allocationNodeMap);

            Assert.AreEqual(testMeasureSet, allocationNodeMap[testAllocationId]);
            Assert.AreEqual(this.newTestBudgetAllocation.PerNodeResults.Count, allocationNodeMap.Count);
            Assert.IsTrue(this.newTestBudgetAllocation.PerNodeResults.All(pnr => allocationNodeMap.ContainsValue(pnr.Key)));
        }

        /// <summary>Get the current active allocation - happy path.</summary>
        [TestMethod]
        public void GetActiveAllocation()
        {
            var activeAllocationJson = AppsJsonSerializer.SerializeObject(this.oldTestBudgetAllocation);
            var activeAllocationBlob = BlobEntity.BuildBlobEntity(this.activeAllocationEntityId, activeAllocationJson);
            this.repository.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Anything))
                .Return(activeAllocationBlob);

            var activeAllocation = this.testActivity.GetActiveAllocation(
                new RequestContext(), this.testCampaignEntity);

            Assert.IsFalse(activeAllocation.PerNodeResults.Count == 0);
        }

        /// <summary>Get the current active allocation - not present on campaign.</summary>
        [TestMethod]
        public void GetActiveAllocationNotYetCreated()
        {
            var activeAllocationAssoc = this.testCampaignEntity.Associations
                .Single(a => a.ExternalName == DynamicAllocationEntityProperties.AllocationSetActive);
            this.testCampaignEntity.Associations.Remove(activeAllocationAssoc);

            var activeAllocation = this.testActivity.GetActiveAllocation(
                new RequestContext(), this.testCampaignEntity);

            Assert.IsNull(activeAllocation.PerNodeResults);
        }

        /// <summary>Get the current active allocation - association exists but deserialize fails.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetActiveAllocationFailDeserialization()
        {
            var activeAllocationBlob = BlobEntity.BuildBlobEntity(this.activeAllocationEntityId, string.Empty);
            activeAllocationBlob.BlobData = "NotABudgetAllocation";
            this.repository.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Anything))
                .Return(activeAllocationBlob);

            this.testActivity.GetActiveAllocation(new RequestContext(), this.testCampaignEntity);
        }

        /// <summary>Get the current active allocation - association exists but entity not found.</summary>
        [TestMethod]
        [ExpectedException(typeof(DataAccessEntityNotFoundException))]
        public void GetActiveAllocationEntityNotFound()
        {
            this.repository.Stub(f => f.GetEntity(Arg<RequestContext>.Is.Anything, Arg<EntityId>.Is.Anything))
                .Throw(new DataAccessEntityNotFoundException());
            this.testActivity.GetActiveAllocation(new RequestContext(), this.testCampaignEntity);
        }

        /// <summary>Get the Node Delivery Metrics - happy path.</summary>
        [TestMethod]
        public void GetNodeDeliveryMetrics()
        {
            var nodeMetrics = BuildNodeDeliveryMetrics();

            var measureSet0 = new MeasureSet(new long[] { 1 });
            var measureSet1 = new MeasureSet(new long[] { 2 });
            var nodeMetricsCollection = new Dictionary<MeasureSet, NodeDeliveryMetrics>
                {
                    { measureSet0, nodeMetrics }, { measureSet1, nodeMetrics } 
                };
            var serializedNodeMetrics = AppsJsonSerializer.SerializeObject(nodeMetricsCollection);

            this.testCampaignEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.AllocationNodeMetrics, serializedNodeMetrics);

            var nodeDeliveryMetricsCollection = GetBudgetAllocationsActivity.GetNodeDeliveryMetrics(this.testCampaignEntity);

            Assert.IsNotNull(nodeDeliveryMetricsCollection);

            // Lightweight assert that the retrieved data is the same as the saved
            var actualNodeMetrics1 = (NodeDeliveryMetrics)nodeDeliveryMetricsCollection[measureSet0];
            Assert.AreEqual(2, nodeDeliveryMetricsCollection.Count);
            Assert.AreEqual(2, actualNodeMetrics1.DeliveryProfile.Count);
        }

        /// <summary>Succeed if there are no node metrics on the campaign.</summary>
        [TestMethod]
        public void GetNodeMetricsNotPresent()
        {
            // Make sure we don't have node results on the campaign
            Assert.AreEqual(
                0,
                this.testCampaignEntity.Properties.Count(p => p.Name == DynamicAllocationEntityProperties.AllocationNodeMetrics));

            var nodeDeliveryMetricsCollection = GetBudgetAllocationsActivity.GetNodeDeliveryMetrics(this.testCampaignEntity);

            Assert.IsNotNull(nodeDeliveryMetricsCollection);
        }

        /// <summary>Fail if we could not deserialize the node metrics on the campaign.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void GetNodeMetricsFailDeserialize()
        {
            this.testCampaignEntity.SetPropertyValueByName(
                DynamicAllocationEntityProperties.AllocationNodeMetrics,
                "*&*BogusJson");

            GetBudgetAllocationsActivity.GetNodeDeliveryMetrics(this.testCampaignEntity);
        }

        /// <summary>Empty PerNodeResults should return true.</summary>
        [TestMethod]
        public void IsInitialAllocationPerNodeResultsEmpty()
        {
            var budgetAllocation = new BudgetAllocation
                { PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>() };
            Assert.IsTrue(GetBudgetAllocationsActivity.IsInitialAllocation(budgetAllocation));
        }

        /// <summary>Null PerNodeResults should return true.</summary>
        [TestMethod]
        public void IsInitialAllocationPerNodeResultsNull()
        {
            var budgetAllocation = new BudgetAllocation { PerNodeResults = null };
            Assert.IsTrue(GetBudgetAllocationsActivity.IsInitialAllocation(budgetAllocation));
        }

        /// <summary>No exports should return true.</summary>
        [TestMethod]
        public void IsInitialAllocationNoExportBudget()
        {
            var budgetAllocation = new BudgetAllocation
                {
                    PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                        {
                            { new MeasureSet { 1 }, new PerNodeBudgetAllocationResult { ExportCount = 0 } }
                        }
                };
            Assert.IsTrue(GetBudgetAllocationsActivity.IsInitialAllocation(budgetAllocation));
        }

        /// <summary>PerNodeResults with export bugdets should return false.</summary>
        [TestMethod]
        public void IsInitialAllocationExportBudgets()
        {
            var budgetAllocation = new BudgetAllocation
            {
                PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                        {
                            { new MeasureSet { 1 }, new PerNodeBudgetAllocationResult { ExportCount = 1 } }
                        }
            };
            Assert.IsFalse(GetBudgetAllocationsActivity.IsInitialAllocation(budgetAllocation));
        }

        /// <summary>AddValueVolumeScoreToAllocations - happy path</summary>
        [TestMethod]
        public void AddValueVolumeScoreToAllocations()
        {
            var measureSet1 = new MeasureSet { 1 };
            var measureSet2 = new MeasureSet { 2 };
            var measureSet3 = new MeasureSet { 3 };

            var nodeDeliveryMetrics = BuildNodeDeliveryMetrics();
            var perNodeResult = new PerNodeBudgetAllocationResult { Valuation = 1 };

            // Set up no inputs with a new node (not in metrics)
            var allocationInputs = new BudgetAllocation
                { 
                    NodeDeliveryMetricsCollection = new Dictionary<MeasureSet, IEffectiveNodeMetrics>
                    {
                        { measureSet1, nodeDeliveryMetrics },
                        { measureSet2, nodeDeliveryMetrics },
                    },
                    PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                    {
                        { measureSet1, perNodeResult },
                        { measureSet2, perNodeResult },
                        { measureSet3, perNodeResult },
                    }
                };

            var activeAllocation = new BudgetAllocation
                {
                    PeriodDuration = new TimeSpan(12, 0, 0),
                    PerNodeResults = new Dictionary<MeasureSet, PerNodeBudgetAllocationResult>
                        {
                            { measureSet1, perNodeResult },
                            { measureSet2, perNodeResult },
                        }
                };

            GetBudgetAllocationsActivity.AddValueVolumeScoreToAllocations(ref allocationInputs, activeAllocation);

            // Should get 1 impression per hour / 1000 = .012 * valuation = 1 * nodes with history = 2
            Assert.AreEqual(.024m, allocationInputs.ValueVolumeScore);
        }

        /// <summary>Should get default initialization on initial allocation.</summary>
        [TestMethod]
        public void BuildBudgetAllocationInputsInitial()
        {
            var allocationInputs = GetBudgetAllocationsActivity.BuildBudgetAllocationInputs(
                this.testCampaignEntity,
                new BudgetAllocation(),
                this.testValuations,
                new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                this.testAllocationParameters);

            Assert.IsNotNull(allocationInputs);
            Assert.AreEqual((DateTime)this.testCampaignEntity.StartDate.Value, allocationInputs.CampaignStart);
            Assert.AreEqual((DateTime)this.testCampaignEntity.EndDate.Value, allocationInputs.CampaignEnd);
            Assert.AreEqual((decimal)this.testCampaignEntity.Budget.Value, allocationInputs.RemainingBudget);
            Assert.AreEqual(0, allocationInputs.NodeDeliveryMetricsCollection.Count);
            Assert.AreEqual(this.testAllocationParameters, allocationInputs.AllocationParameters);
            Assert.AreEqual(0, allocationInputs.ValueVolumeScore);

            this.AssertValuationsTransferredToAllocationInputs(this.testValuations, allocationInputs);
            Assert.IsFalse(allocationInputs.PerNodeResults.Any(r => r.Value.ExportCount > 0));
        }

        /// <summary>Fail if realloc and remaining budget not set.</summary>
        [TestMethod]
        [ExpectedException(typeof(ActivityException))]
        public void BuildBudgetAllocationInputsRemainingBudgetNotSet()
        {
            GetBudgetAllocationsActivity.BuildBudgetAllocationInputs(
                this.testCampaignEntity,
                this.oldTestBudgetAllocation, 
                new Dictionary<MeasureSet, decimal>(), 
                new Dictionary<MeasureSet, IEffectiveNodeMetrics>(),
                this.testAllocationParameters);
        }

        /// <summary>Realloc - happy path.</summary>
        [TestMethod]
        public void BuildBudgetAllocationInputsSuccess()
        {
            var remainingBudget = 10001;
            this.testCampaignEntity.SetRemainingBudget(remainingBudget);

            var nodeDeliveryMetrics = BuildNodeDeliveryMetrics();
            var nodeDeliveryMetricsCollection = this.testValuations.ToDictionary(
                kvp => kvp.Key, kvp => (IEffectiveNodeMetrics)nodeDeliveryMetrics);

            var allocationInputs = GetBudgetAllocationsActivity.BuildBudgetAllocationInputs(
                this.testCampaignEntity,
                this.oldTestBudgetAllocation,
                this.testValuations,
                nodeDeliveryMetricsCollection, 
                this.testAllocationParameters);

            Assert.IsNotNull(allocationInputs);
            Assert.AreEqual((DateTime)this.testCampaignEntity.StartDate.Value, allocationInputs.CampaignStart);
            Assert.AreEqual((DateTime)this.testCampaignEntity.EndDate.Value, allocationInputs.CampaignEnd);
            Assert.AreEqual(remainingBudget, allocationInputs.RemainingBudget);
            Assert.AreEqual(nodeDeliveryMetricsCollection.Count, allocationInputs.NodeDeliveryMetricsCollection.Count);
            Assert.AreEqual(this.testAllocationParameters, allocationInputs.AllocationParameters);
            Assert.AreNotEqual(0m, allocationInputs.ValueVolumeScore);

            this.AssertValuationsTransferredToAllocationInputs(this.testValuations, allocationInputs);
            var oldExportedNodes = this.oldTestBudgetAllocation.PerNodeResults.Where(r => r.Value.ExportCount != 0).Select(r => r.Key).ToList();
            var newExportedNodes = allocationInputs.PerNodeResults.Where(r => r.Value.ExportCount != 0).Select(r => r.Key).ToList();
            Assert.IsTrue(newExportedNodes.Any());
            Assert.IsFalse(newExportedNodes.Except(oldExportedNodes).Any());
        }

        /// <summary>CheckExport is false if campaign is over.</summary>
        [TestMethod]
        public void CheckExportCampaignOver()
        {
            var now = DateTime.UtcNow;
            this.testCampaignEntity.EndDate = now;
            var result = GetBudgetAllocationsActivity.CheckExport(
                new BudgetAllocation { PeriodStart = now },
                this.testCampaignEntity,
                new Dictionary<MeasureSet, decimal> { { new MeasureSet(new[] { 1L, 2L }), 1m } });

            Assert.IsFalse(result);
        }

        /// <summary>CheckExport is false there are no exported nodes.</summary>
        [TestMethod]
        public void CheckExportNoNodes()
        {
            var now = DateTime.UtcNow;
            this.testCampaignEntity.EndDate = now.AddDays(1);
            var result = GetBudgetAllocationsActivity.CheckExport(
                new BudgetAllocation { PeriodStart = now },
                this.testCampaignEntity,
                new Dictionary<MeasureSet, decimal>());

            Assert.IsFalse(result);
        }

        /// <summary>CheckExport is true if we have exported nodes and are not past the campaign end.</summary>
        [TestMethod]
        public void CheckExportTrue()
        {
            var now = DateTime.UtcNow;
            this.testCampaignEntity.EndDate = now.AddDays(1);
            var result = GetBudgetAllocationsActivity.CheckExport(
                new BudgetAllocation { PeriodStart = now },
                this.testCampaignEntity,
                new Dictionary<MeasureSet, decimal> { { new MeasureSet(new[] { 1L, 2L }), 1m } });

            Assert.IsTrue(result);
        }

        /// <summary>Build a NodeDeliveryMetrics object for testing.</summary>
        /// <returns>The NodeDeliveryMetrics object</returns>
        private static NodeDeliveryMetrics BuildNodeDeliveryMetrics()
        {
            // Json serialization truncates to milliseconds on dates
            var lastProcessedEligibilityHour = new DateTime(2012, 1, 1, 1, 1, 1, 111, DateTimeKind.Utc);

            var hourMetrics1 = new NodeHourMetrics { AverageImpressions = 1, AverageMediaSpend = 1, EligibilityCount = 1, };
            hourMetrics1.LastNImpressions.Add(new[] { 1L });
            hourMetrics1.LastNMediaSpend.Add(new[] { 1m });
            var hourMetrics2 = new NodeHourMetrics { AverageImpressions = 1, AverageMediaSpend = 1, EligibilityCount = 1, };
            hourMetrics2.LastNImpressions.Add(new[] { 1L });
            hourMetrics2.LastNMediaSpend.Add(new[] { 1m });

            var nodeMetrics = new NodeDeliveryMetrics
            {
                TotalEligibleHours = 1,
                TotalSpend = 1,
                TotalImpressions = 1,
                TotalMediaSpend = 1,
                LastProcessedEligibilityHour = lastProcessedEligibilityHour,
            };
            nodeMetrics.DeliveryProfile[1] = hourMetrics1;
            nodeMetrics.DeliveryProfile[2] = hourMetrics2;
            return nodeMetrics;
        }

        /// <summary>Assert that valuations are transferred to allocation inputs.</summary>
        /// <param name="valuations">The valuations.</param>
        /// <param name="allocationInputs">The allocation inputs.</param>
        private void AssertValuationsTransferredToAllocationInputs(
            IDictionary<MeasureSet, decimal> valuations, 
            BudgetAllocation allocationInputs)
        {
            foreach (var perNodeResult in allocationInputs.PerNodeResults)
            {
                Assert.AreEqual(valuations[perNodeResult.Key], perNodeResult.Value.Valuation);
            }

            Assert.AreEqual(valuations.Count, allocationInputs.PerNodeResults.Count);
        }
    }
}
