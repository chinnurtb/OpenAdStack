// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Simulator.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Activities;
using AppNexusUtilities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;
using DynamicAllocationActivities;
using DynamicAllocationUtilities;
using EntityUtilities;
using ScheduledActivities;
using SimulatedDataStore;
using Utilities.AllocationSimulator;
using Utilities.Serialization;
using Utilities.Storage;
using Utilities.Storage.Testing;

using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace AllocationSimulator
{
    /// <summary>
    /// Campaign simulator class
    /// </summary>
    public class Simulator
    {
        /// <summary>Initializes a new instance of the <see cref="Simulator"/> class.</summary>
        /// <param name="args">Command-line args.</param>
        public Simulator(AllocationSimulatorArgs args)
            : this(args, new FileHandler())
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Simulator"/> class.</summary>
        /// <param name="args">Command-line args.</param>
        /// <param name="fileHandler">The file Handler.</param>
        public Simulator(AllocationSimulatorArgs args, IFileHandler fileHandler)
        {
            this.FileHandler = fileHandler;
            this.SimInputFile = args.InFile.FullName;
            this.SaveRoot = @"C:\";
            if (args.OutFile != null)
            {
                this.SaveRoot = args.OutFile.FullName;
            }

            if (args.LogFile != null)
            {
                LogManager.Initialize(new[]
                {
                    new FileLogger(args.LogFile.FullName)
                });
            }

            this.IsRepositoryCampaign = args.IsRepCampaign;
            this.IsDryRun = args.IsDryRun;
            this.TargetProfile = args.TargetProfile;

            this.UserId = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));

            this.CompanyEntityId = new EntityId();
            if (args.CompanyEntityId != null)
            {
                this.CompanyEntityId = new EntityId(args.CompanyEntityId);
            }

            this.CampaignEntityId = new EntityId();
            if (args.CampaignEntityId != null)
            {
                this.CampaignEntityId = new EntityId(args.CampaignEntityId);
            }

            this.DryRunStart = DateTime.UtcNow;
            DateTime dryRunStart;
            if (DateTime.TryParse(
                args.DryRunStart, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dryRunStart))
            {
                this.DryRunStart = dryRunStart;
            }

            // Setup global app config
            SetUpConfigurationManager();

            // Set up the simulator config based on the command line input file
            this.SetupSimulatorConfig();

            // Setup simulated storage
            SetUpSimulatedPersistantStorage();
            this.SetUpMeasureSourceProviders(this.TargetProfile);
            this.SetupSimulatedRepository();
        }

        /// <summary>Gets the IEntityRepository instance.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Gets the Campaign Id</summary>
        internal string CampaignEntityId { get; private set; }

        /// <summary>Gets the Company Id</summary>
        internal string CompanyEntityId { get; private set; }

        /// <summary>Gets the raw delivery simulator.</summary>
        internal RawDeliverySimulator RawDeliverySimulator { get; private set; }

        /// <summary>Gets the input file allocation parameters.</summary>
        internal Dictionary<string, string> CampaignAllocationParameters { get; private set; }

        /// <summary>Gets the measure map resource name.</summary>
        internal string MeasureMapResourceName { get; private set; }

        /// <summary>Gets the User Id</summary>
        internal string UserId { get; private set; }

        /// <summary>Gets the dry run start time.</summary>
        internal DateTime DryRunStart { get; private set; }

        /// <summary>Gets the target profile for the measure sources.</summary>
        internal string TargetProfile { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the simulator is a dry run of the next allocation on a campaign
        /// stored in the repository.
        /// </summary>
        internal bool IsDryRun { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the campaign parameters come from a campaign
        /// stored in the repository.
        /// </summary>
        internal bool IsRepositoryCampaign { get; private set; }

        /// <summary>Gets the directory in which to save simulator run data files.</summary>
        internal string SaveRoot { get; private set; }

        /// <summary>Gets the simulation parameters input file path.</summary>
        internal string SimInputFile { get; private set; }

        /// <summary>Gets the IFileHandler instance.</summary>
        internal IFileHandler FileHandler { get; private set; }

        /// <summary>Gets or sets a value indicating whether to skip delivery.</summary>
        internal bool SkipDelivery { get; set; }
        
        /// <summary>
        /// run the simulator
        /// </summary>
        public void Run()
        {
            // setup output js file
            var now = DateTime.Now;
            var outputFolderName = "{0}_{1}_{2}_{3}_{4}_{5}".FormatInvariant(
                now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            var directoryName = Path.Combine(this.SaveRoot, outputFolderName);
            var outputPath = this.FileHandler.CreateDirectory(directoryName);

            // Dump the input file to the output folder
            var inputFile = File.ReadAllText(this.SimInputFile);
            this.FileHandler.WriteFile(Path.Combine(outputPath, "simulatorconfig.js"), inputFile);

            // Single allocation, repository-based delivery, repository-based campaign
            if (this.IsDryRun)
            {
                this.SetupRepositoryCampaign();
                this.RunDry(outputPath);
                return;
            }

            // Full run, simulated delivery, repository-based campaign
            if (this.IsRepositoryCampaign)
            {
                this.SetupRepositoryCampaign();
                this.RunSimulated(outputPath);
                return;
            }

            // Full run, simulated delivery, inputfile-based campaign
            this.SetupFileBasedCampaign();
            this.RunSimulated(outputPath);
        }

        /// <summary>Set up the mock repository with a campaign from the simulator input file.</summary>
        /// <param name="totalBudget">the total budget</param>
        /// <param name="remainingBudget">the remaining budget</param>
        /// <param name="startDate">Campaign start date.</param>
        /// <param name="endDate">Campaign end date.</param>
        /// <param name="nodeValuationSetJson">NodeValuationSet json</param>
        /// <param name="measureSetsJson">MeasureSets json</param>
        internal void BuildFileBasedCampaign(
            decimal totalBudget,
            decimal remainingBudget,
            DateTime startDate,
            DateTime endDate,
            string nodeValuationSetJson,
            string measureSetsJson)
        {
            // Setup company
            var companyEntity = CreateTestCompanyEntity(
                this.CompanyEntityId,
                "Test Company");
            companyEntity.SetPropertyValueByName("DeliveryNetwork", DeliveryNetworkDesignation.AppNexus.ToString());
            this.Repository.SaveEntity(null, companyEntity);

            // Setup campaign owner user
            var campaignOwnerEntity = CreateTestUserEntity(this.UserId, "Test User");
            this.Repository.SaveUser(null, campaignOwnerEntity);

            // Setup campaign
            var campaign = CreateTestCampaignEntity(
                this.CampaignEntityId,
                "test",
                totalBudget,
                startDate,
                endDate,
                "mike");
            campaign.SetOwnerId(this.UserId);
            campaign.SetRemainingBudget(remainingBudget);
            campaign.SetSystemProperty(DeliveryNetworkEntityProperties.ExporterVersion, 1);

            // Set valuation inputs
            campaign.SetPropertyByName(daName.MeasureList, measureSetsJson);
            campaign.SetPropertyByName(daName.NodeValuationSet, nodeValuationSetJson);

            // Setup allocation parameters on campaign
            this.SetupCampaignAllocationParameters(campaign);

            this.Repository.TrySaveEntity(null, campaign);
        }

        /// <summary>
        /// Setup the simulation to run a campaign based on the input file.
        /// </summary>
        internal void SetupFileBasedCampaign()
        {
            var inputJson = File.ReadAllText(this.SimInputFile);
            var inputDictionary = AppsJsonSerializer.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(inputJson);
            var properties = AppsJsonSerializer.DeserializeObject<Dictionary<string, string>>(inputDictionary["Campaign"]["Properties"].ToString());
            var extendedProperties = AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(inputDictionary["Campaign"]["ExtendedProperties"].ToString());

            // Get the campaign properties
            var totalBudget = decimal.Parse(properties["Budget"], CultureInfo.InvariantCulture);
            var remainingBudget = decimal.Parse(properties["RemainingBudget"], CultureInfo.InvariantCulture);
            var campaignStart = DateTime.Parse(properties["StartDate"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var campaignEnd = DateTime.Parse(properties["EndDate"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

            // Get the valuation inputs
            var nodeValuationSetJson = extendedProperties[daName.NodeValuationSet].ToString();
            var measureSetsJson = extendedProperties[daName.MeasureList].ToString();

            this.BuildFileBasedCampaign(
                totalBudget,
                remainingBudget,
                campaignStart,
                campaignEnd,
                nodeValuationSetJson,
                measureSetsJson);
        }

        /// <summary>
        /// Set Up Simulated Persistant Storage
        /// </summary>
        private static void SetUpSimulatedPersistantStorage()
        {
            // Setup simulated persistent storage
            SimulatedPersistentDictionaryFactory.Initialize();
            Scheduler.Registries = null;
        }

        /// <summary>
        /// Set Up Configuration Manager
        /// </summary>
        private static void SetUpConfigurationManager()
        {
            TestUtilities.AllocationParametersDefaults.Initialize();
            var types = (PersistentDictionaryType[])Enum.GetValues(typeof(PersistentDictionaryType));
            ConfigurationManager.AppSettings["PersistentDictionary.DefaultType"] = types.First().ToString();
            ConfigurationManager.AppSettings["DynamicAllocation.ReallocationSchedule"] = "00:00:00|12:00:00";
        }

        /// <summary>
        /// Creates a test campaign entity with the specified values
        /// </summary>
        /// <param name="campaignEntityId">The campaign Id</param>
        /// <param name="externalName">The external name</param>
        /// <param name="budget">The budget</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="personaName">The persona name</param>
        /// <returns>Campaign json</returns>
        private static CampaignEntity CreateTestCampaignEntity(string campaignEntityId, string externalName, decimal budget, DateTime startDate, DateTime endDate, string personaName)
        {
            // TODO: copied this method here as a quick fix for referencing the it across solutions. de-dupe.  
            return EntityJsonSerializer.DeserializeCampaignEntity(new EntityId(campaignEntityId), CreateCampaignJson(campaignEntityId, externalName, budget, startDate, endDate, personaName));
        }

        /// <summary>
        /// Creates a test Company entity with the specified values
        /// </summary>
        /// <param name="companyEntityId">Company Id</param>
        /// <param name="externalName">External Name</param>
        /// <returns>The Company company entity</returns>
        private static CompanyEntity CreateTestCompanyEntity(string companyEntityId, string externalName)
        {
            // TODO: copied this method here as a quick fix for referencing the it across solutions. de-dupe.  
            return new CompanyEntity(
                new EntityId(companyEntityId),
                new Entity { ExternalName = externalName, LocalVersion = 1 });
        }

        /// <summary>
        /// Creates a test User entity with the specified values
        /// </summary>
        /// <param name="userId">User Id</param>
        /// <param name="externalName">External Name</param>
        /// <returns>The user entity</returns>
        private static UserEntity CreateTestUserEntity(string userId, string externalName)
        {
            var user = new UserEntity(
                new EntityId(),
                new Entity { ExternalName = externalName, LocalVersion = 1 })
                {
                    UserId = userId,
                };
            user.SetUserType(UserType.StandAlone);
            return user;
        }

        /// <summary>
        /// Creates campaign json for tests
        /// </summary>
        /// <param name="campaignEntityId">The campaign Id</param>
        /// <param name="externalName">The external name</param>
        /// <param name="budget">The budget</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <param name="personaName">The persona name</param>
        /// <returns>Campaign json</returns>
        private static string CreateCampaignJson(string campaignEntityId, string externalName, decimal budget, DateTime startDate, DateTime endDate, string personaName)
        {
            // TODO: copied this method here as a quick fix for referencing the it across solutions. de-dupe.  
            var jsonFormat =
@"{{
    ""ExternalEntityId"":""{0}"",
    ""ExternalName"":""{1}"",
    ""LocalVersion"":""{6}"",
    ""Properties"":
    {{
        ""Budget"":""{2}"",
        ""EndDate"":""{3}"",
        ""PersonaName"":""{4}"",
        ""StartDate"":""{5}""
    }}
}}";
            return jsonFormat.FormatInvariant(
                campaignEntityId, 
                externalName, 
                budget, 
                (PropertyValue)endDate, 
                personaName,
                (PropertyValue)startDate,
                1);
        }

        /// <summary>Submits an activity request from within an activity</summary>
        /// <param name="request">The request to submit</param>
        /// <param name="sourceName">The source of the request</param>
        /// <returns>True if the request was submitted successfully; otherwise, false.</returns>
        private static bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            // TODO: copied this method here as a quick fix for referencing the it across solutions. de-dupe.  
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set up the simulated entity repository
        /// </summary>
        private void SetupSimulatedRepository()
        {
            if (!this.IsRepositoryCampaign)
            {
                this.Repository = new SimulatedEntityRepository();
                return;
            }

            this.Repository = new SimulatedEntityRepository(
                ConfigurationManager.AppSettings["IndexLocal.ConnectionString"],
                ConfigurationManager.AppSettings["EntityLocal.ConnectionString"],
                ConfigurationManager.AppSettings["IndexReadOnly.ConnectionString"],
                ConfigurationManager.AppSettings["EntityReadOnly.ConnectionString"],
                true);
        }
        
        /// <summary>
        /// Set Up Measure Source Provider Factories
        /// </summary>
        /// <param name="targetProfile">The target profile</param>
        private void SetUpMeasureSourceProviders(string targetProfile)
        {
            var measureSourceProvider = new AppNexusMeasureSourceProvider(targetProfile);
            MeasureSourceFactory.Initialize(new IMeasureSourceProvider[]
            {
                measureSourceProvider
            });

            this.MeasureMapResourceName = measureSourceProvider.AppNexusMeasureMapResourceName;
        }

        /// <summary>Do a dry run of the latest allocation cycle.</summary>
        /// <param name="outputPath">The output file directory.</param>
        private void RunDry(string outputPath)
        {
            Console.WriteLine("Starting Dry Run");
            Console.WriteLine(DateTime.Now);

            var valuationsJson = this.ApproveAndGetValuations();
            this.FileHandler.WriteFile(Path.Combine(outputPath, "valuations.js"), valuationsJson);

            var measureMapJson = this.GetFilteredMeasureMapForCampaign();
            this.FileHandler.WriteFile(Path.Combine(outputPath, "measuremap.js"), measureMapJson);

            // This will be benign for no delivery data, but fail if an allocation hasn't been done.
            this.CreateAndRunGetCampaignDeliveryDataActivity();

            // Run the allocation
            this.CreateAndRunGetBudgetAllocationActivity(this.DryRunStart, false);

            // Get index entries (oldest first)
            var index = this.GetAllocationHistoryIndex();
            index.Reverse();

            // Process the allocation histories
            var allocationCount = 0;
            foreach (var historyElement in index)
            {
                // Get active allocation
                var context = new RequestContext { ExternalCompanyId = this.CompanyEntityId };
                var budgetAllocationBlobEntityId = historyElement.AllocationOutputsId;
                var budgetAllocationBlob = this.Repository.TryGetEntity(context, budgetAllocationBlobEntityId) as BlobEntity;
                var budgetAllocationJson = budgetAllocationBlob.DeserializeBlob<string>();

                // write alloc to file
                var fileName = Path.Combine(outputPath, "simrun{0}.js".FormatInvariant(allocationCount));
                this.FileHandler.WriteFile(fileName, budgetAllocationJson);

                allocationCount++;
            }

            Console.WriteLine("Ending Dry Run");
            Console.WriteLine(DateTime.Now);
        }

        /// <summary>Do a simulated run of the campaign with simulated delivery data.</summary>
        /// <param name="outputPath">The output file directory.</param>
        private void RunSimulated(string outputPath)
        {
            var campaign = this.Repository.GetEntity<CampaignEntity>(null, this.CampaignEntityId);
            var simStart = (DateTime)campaign.StartDate;
            var simEnd = (DateTime)campaign.EndDate;

            var campaignTime = simStart;
            var timeMap = new Dictionary<DateTime, DateTime> { { campaignTime, DateTime.UtcNow } };

            Console.WriteLine("ApproveValuations & GetMeasureMap Starting");
            var start = DateTime.Now;
            
            var valuationsJson = this.ApproveAndGetValuations();
            this.FileHandler.WriteFile(Path.Combine(outputPath, "valuations.js"), valuationsJson);

            var measureMapJson = this.GetFilteredMeasureMapForCampaign();
            this.FileHandler.WriteFile(Path.Combine(outputPath, "measuremap.js"), measureMapJson);

            var duration = Math.Round((DateTime.Now - start).TotalSeconds, 1);
            Console.WriteLine("Dur: {0} sec. - ApproveValuations & GetMeasureMap Complete".FormatInvariant(duration));

            int counter = 0;
            var lastProcessedIndexEntry = DateTime.MinValue;
            var initialAllocation = true;
            var unreportedExportAllocations = new List<BudgetAllocation>();
            this.SkipDelivery = false;

            do
            {
                ////
                // run budget allocation
                Console.WriteLine(initialAllocation
                    ? "InitialAllocation for {0} Starting".FormatInvariant(campaignTime)
                    : "Allocation #{0} for {1} Starting".FormatInvariant(counter, campaignTime));
                start = DateTime.Now;

                this.CreateAndRunGetBudgetAllocationActivity(campaignTime, initialAllocation);
                initialAllocation = false;
                
                duration = Math.Round((DateTime.Now - start).TotalSeconds, 1);
                Console.WriteLine("Dur: {0} sec. - Allocation #{1} Complete".FormatInvariant(duration, counter));

                ////
                // Simulate delivery on the allocations

                // Get index entries we have not processed (oldest first)
                var index = this.GetAllocationHistoryIndex();
                index = index.Where( 
                    i => (DateTime)new PropertyValue(PropertyType.Date, i.AllocationStartTime) > lastProcessedIndexEntry)
                        .Reverse().ToList();
                lastProcessedIndexEntry =
                    index.Max(i => (DateTime)new PropertyValue(PropertyType.Date, i.AllocationStartTime));

                // Process the allocation histories
                foreach (var historyElement in index)
                {
                    var budgetAllocationBlobEntityId = historyElement.AllocationOutputsId;
                    var budgetAllocationBlob =
                        this.Repository.TryGetEntity(null, budgetAllocationBlobEntityId) as BlobEntity;
                    var budgetAllocationJson = budgetAllocationBlob.DeserializeBlob<string>();
                    var budgetAllocation = AppsJsonSerializer.DeserializeObject<BudgetAllocation>(budgetAllocationJson);

                    Console.WriteLine("IncrementExportCountsActivity Starting");
                    start = DateTime.Now;
                    
                    // Increment the export counts
                    this.CreateAndRunIncrementExportsActivity(budgetAllocation);
                    
                    duration = Math.Round((DateTime.Now - start).TotalSeconds, 1);
                    Console.WriteLine("Dur: {0} sec. - IncrementExportCountsActivity Complete".FormatInvariant(duration));

                    // Reconstitute the budget allocation json & write to output file
                    budgetAllocationJson = AppsJsonSerializer.SerializeObject(budgetAllocation);
                    var fileName = Path.Combine(outputPath, "simrun{0}.js".FormatInvariant(counter));
                    this.FileHandler.WriteFile(fileName, budgetAllocationJson);

                    // Our exit point is after export, but before delivery.
                    // The final simulator realloc didn't correspond to a real allocation.
                    // It was run to incorporate the last simulated delivery into the
                    // allocation history so it is reflected in the simulator output.
                    if (campaignTime >= simEnd)
                    {
                        return;
                    }

                    // Advance the campaign time to the end of the allocation period
                    campaignTime += budgetAllocation.PeriodDuration;

                    unreportedExportAllocations.Add(budgetAllocation);
                    if (!this.SkipDelivery)
                    {
                        // Simulate delivery
                        Console.WriteLine("Simulated Delivery #{0} for {1} Starting".FormatInvariant(counter, campaignTime));
                        start = DateTime.Now;

                        this.SimulateDelivery(unreportedExportAllocations, campaignTime);
                        unreportedExportAllocations.Clear();
                        
                        duration = Math.Round((DateTime.Now - start).TotalSeconds, 1);
                        Console.WriteLine("Dur: {0} sec. - Simulated Delivery #{1} Complete"
                            .FormatInvariant(duration, counter));
                    }
                    else
                    {
                        Console.WriteLine("Delay Delivery #{0} for {1}".FormatInvariant(counter, campaignTime));
                    }

                    // Run the delivery data activity
                    Console.WriteLine("Run Delivery History Activity #{0} for {1} Starting".FormatInvariant(counter, campaignTime));
                    start = DateTime.Now;

                    this.CreateAndRunGetCampaignDeliveryDataActivity();
                    
                    duration = Math.Round((DateTime.Now - start).TotalSeconds, 1);
                    Console.WriteLine("Dur: {0} sec. - Delivery History Activity #{1} Complete"
                        .FormatInvariant(duration, counter));
                    Console.WriteLine("////////////////////////\n");

                    counter++;
                }

                timeMap.Add(campaignTime, DateTime.UtcNow);
                this.CleanOldMemory(campaignTime, timeMap);
            }
            while (true);
        }

        /// <summary>Get a subset of the measure map matching this campaign.</summary>
        /// <returns>The measure map json.</returns>
        private string GetFilteredMeasureMapForCampaign()
        {
            var campaign = this.Repository.GetEntity<CampaignEntity>(null, this.CampaignEntityId);
            var inputs = ValuationsCache.BuildValuationInputs(campaign);
            var measures = inputs.MeasureSetsInput.Measures.Select(i => "{0}".FormatInvariant(i.Measure));

            var res = Assembly.GetCallingAssembly().GetManifestResourceStream(this.MeasureMapResourceName);

            string measureMapJson = null;
            if (res != null)
            {
                measureMapJson = new StreamReader(res).ReadToEnd();
            }

            var measureMap = AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(measureMapJson);
            var filteredMeasureMap = measureMap.Where(kvp => measures.Contains(kvp.Key));
            var interiorJson = filteredMeasureMap.Select(kvp => "\"{0}\":{1}"
                .FormatInvariant(kvp.Key, AppsJsonSerializer.SerializeObject(kvp.Value)));
            var filteredJson = "{{{0}}}".FormatInvariant(string.Join(",", interiorJson));
            return filteredJson;
        }

        /// <summary>Clean old entities that should not be needed from the simulated repository memorycache</summary>
        /// <param name="time">Current allocation time</param>
        /// <param name="timeMap">Map of allocation times to real time.</param>
        private void CleanOldMemory(DateTime time, Dictionary<DateTime, DateTime> timeMap)
        {
            // Get a cutoff "real time" for entities that have not been touched for 10 "campaign days"
            var timeMinus10Days = time.AddDays(-10);
            var oldTimes = timeMap.Where(kvp => kvp.Key < timeMinus10Days).ToDictionary().Values;
            if (!oldTimes.Any())
            {
                return;
            }

            var cutoff = oldTimes.First();
            foreach (var oldTime in oldTimes)
            {
                if (cutoff < oldTime)
                {
                    cutoff = oldTime;
                }
            }

            var simRepository = (SimulatedEntityRepository)this.Repository;
            simRepository.CleanOldMemory(cutoff);
        }

        /// <summary>
        /// Gets the allocation history index
        /// </summary>
        /// <returns>the history index</returns>
        private List<HistoryElement> GetAllocationHistoryIndex()
        {
            // get the allocation history
            var campaign = this.Repository.TryGetEntity(null, this.CampaignEntityId);

            // Get the budget allocation history index
            var budgetAllocationHistoryAssociation =
                campaign.TryGetAssociationByName(DynamicAllocationEntityProperties.AllocationHistoryIndex);
            var blobEntity = this.Repository.TryGetEntity(null, budgetAllocationHistoryAssociation.TargetEntityId) as BlobEntity;
            var existingJson = blobEntity.DeserializeBlob<string>();
            var index = AppsJsonSerializer.DeserializeObject<List<HistoryElement>>(existingJson);
            return index;
        }

        /// <summary>Setup simulator configuration parameters.</summary>
        private void SetupSimulatorConfig()
        {
            // Convert the input file json to dictionary form
            var inputJson = File.ReadAllText(this.SimInputFile);
            var inputDictionary = AppsJsonSerializer.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(inputJson);
            var systemProperties = AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(inputDictionary["Campaign"]["SystemProperties"].ToString());
            var simulatorConfigJson = systemProperties["SimulatorConfig"].ToString();
            var simulatorConfig = AppsJsonSerializer.DeserializeObject<Dictionary<string, object>>(simulatorConfigJson);

            // Set up the allocation parameters
            this.CampaignAllocationParameters = AppsJsonSerializer.DeserializeObject<Dictionary<string, string>>(systemProperties["AllocationParameters"].ToString());

            // Set up simulator assumptions
            var randomSeed = int.Parse(simulatorConfig["RandomSeed"].ToString(), CultureInfo.InvariantCulture);
            var baseTier = int.Parse(simulatorConfig["RateBaseTier"].ToString(), CultureInfo.InvariantCulture);
            var topTier = int.Parse(simulatorConfig["RateTopTier"].ToString(), CultureInfo.InvariantCulture);
            this.RawDeliverySimulator = new RawDeliverySimulator();
            this.RawDeliverySimulator.RandPercentage = double.Parse(
                simulatorConfig["RandPercentage"].ToString(), CultureInfo.InvariantCulture);
            this.RawDeliverySimulator.BaseTierDeliveryProbability =
                double.Parse(simulatorConfig["BaseTierDeliveryProbability"].ToString(), CultureInfo.InvariantCulture);
            this.RawDeliverySimulator.DeliveryProbabilityDecayRate =
                double.Parse(simulatorConfig["DeliveryProbilityDecayRate"].ToString(), CultureInfo.InvariantCulture);
            this.RawDeliverySimulator.AverageEstimatedCostPerMille = decimal.Parse(
                simulatorConfig["AverageEcpm"].ToString(), CultureInfo.InvariantCulture);
            this.RawDeliverySimulator.MeasureRateDeviationPercentage =
                decimal.Parse(simulatorConfig["MeasureRateDeviationPercentage"].ToString(), CultureInfo.InvariantCulture);
            var baseTierRate = double.Parse(simulatorConfig["BaseTierRate"].ToString(), CultureInfo.InvariantCulture);
            var allocationTopTierRate = double.Parse(
                simulatorConfig["AllocationTopTierRate"].ToString(), CultureInfo.InvariantCulture);
            this.RawDeliverySimulator.AverageMeasureRate = baseTierRate == 0 ? 0 :
                (decimal)Math.Pow(allocationTopTierRate / baseTierRate, 1.0 / (topTier - baseTier));
            this.RawDeliverySimulator.RateConversionFactor = baseTierRate == 0 ? 0 :
                (decimal)(baseTierRate / Math.Pow((double)this.RawDeliverySimulator.AverageMeasureRate, baseTier));
            this.RawDeliverySimulator.DeliverySimulationType = simulatorConfig["DeliverySimulationType"].ToString();

            this.RawDeliverySimulator.Random = randomSeed == -1 ? new Random() : new Random(randomSeed);
        }

        /// <summary>Simulate Delivery</summary>
        /// <param name="exportedAllocations">the budget allocations of the period for which we are simulating delivery</param>
        /// <param name="campaignTime">The current "campaign time".</param>
        private void SimulateDelivery(IList<BudgetAllocation> exportedAllocations, DateTime campaignTime)
        {
            var campaign = this.Repository.TryGetEntity(null, this.CampaignEntityId);

            // create new raw delivery data
            var rawDeliveryData = this.RawDeliverySimulator.DeliverySimulationType == "1" ? 
                this.RawDeliverySimulator.GetSimulatedRawDeliveryDataTierBased(exportedAllocations) :
                this.RawDeliverySimulator.GetSimulatedRawDeliveryDataLineageBased(exportedAllocations);

            // update the repository with the new data
            var deliveryDataEntityId = new EntityId();
            var dynDeliveryDataBlob = BlobEntity.BuildBlobEntity(deliveryDataEntityId, rawDeliveryData) as IEntity;
            this.Repository.TrySaveEntity(null, dynDeliveryDataBlob);
            
            // TODO: Encapsulation break with a real repository
            dynDeliveryDataBlob.LastModifiedDate = campaignTime;

            // if this is the first run, do some intial setup
            var deliveryDataIndex = new List<string>();
            var deliveryDataIndexBlobAssociation =
                campaign.TryGetAssociationByName(AppNexusEntityProperties.AppNexusRawDeliveryDataIndex);
            if (deliveryDataIndexBlobAssociation != null)
            {
                var oldDeliveryDataIndexBlob = this.Repository.TryGetEntity(
                    null, deliveryDataIndexBlobAssociation.TargetEntityId) as BlobEntity;
                deliveryDataIndex = oldDeliveryDataIndexBlob.DeserializeBlob<List<string>>();
            }

            // Otherwise update the index
            deliveryDataIndex.Add(deliveryDataEntityId.ToString());
            var deliveryDataIndexEntityId = new EntityId();
            var deliveryDataIndexBlob = BlobEntity.BuildBlobEntity(
                deliveryDataIndexEntityId, deliveryDataIndex) as IEntity;
            this.Repository.TrySaveEntity(null, deliveryDataIndexBlob);

            // TODO: IEntityRepository encapsulation break
            deliveryDataIndexBlob.LastModifiedDate = campaignTime;

            campaign.TryAssociateEntities(
                AppNexusEntityProperties.AppNexusRawDeliveryDataIndex,
                string.Empty,
                new HashSet<IEntity>(new[] { deliveryDataIndexBlob }),
                AssociationType.Relationship,
                true);

            this.Repository.TrySaveEntity(null, campaign);
        }

        /// <summary>
        /// Create And Run Get Campaign Delivery Data Activity
        /// </summary>
        private void CreateAndRunGetCampaignDeliveryDataActivity()
        {
            var activity = Activity.CreateActivity(
                    typeof(GetCampaignDeliveryDataActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.Repository } },
                    SubmitActivityRequest) as GetCampaignDeliveryDataActivity;

            var request = new ActivityRequest
            {
                Values =
                {
                    { "AuthUserId", this.UserId },
                    { "CompanyEntityId", this.CompanyEntityId },
                    { "CampaignEntityId", this.CampaignEntityId }
                }
            };

            var result = activity.Run(request);
            if (!result.Succeeded)
            {
                throw new ActivityException("GetBudgetAllocationsActivity failed: {0}\n{1}"
                    .FormatInvariant(result.Error.ErrorId, result.Error.Message));
            }
        }

        /// <summary>
        /// Create and run the budget allocation activity
        /// </summary>
        /// <param name="time">the current time in the simulation</param>
        /// <param name="forceInitialAllocation">Whether to explicitly run an initial allocation</param>
        private void CreateAndRunGetBudgetAllocationActivity(DateTime time, bool forceInitialAllocation)
        {
            var activity = Activity.CreateActivity(
                typeof(GetBudgetAllocationsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.Repository } },
                SubmitActivityRequest)
                as DynamicAllocationActivity;

            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetBudgetAllocations,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.UserId },
                    { EntityActivityValues.CompanyEntityId, this.CompanyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.CampaignEntityId },
                    { DynamicAllocationActivityValues.AllocationStartDate, time.ToString("o", CultureInfo.InvariantCulture) },
                    { DynamicAllocationActivityValues.IsInitialAllocation, forceInitialAllocation.ToString(CultureInfo.InvariantCulture) },
                    { "time", time.ToString("o", CultureInfo.InvariantCulture) }
                }
            };

            var result = activity.Run(request);
            if (!result.Succeeded)
            {
                throw new ActivityException("GetBudgetAllocationsActivity failed: {0}\n{1}"
                    .FormatInvariant(result.Error.ErrorId, result.Error.Message));
            }
        }

        /// <summary>Create and run the Increrment Exports Activity</summary>
        /// <param name="budgetAllocation">The budget Allocation.</param>
        private void CreateAndRunIncrementExportsActivity(BudgetAllocation budgetAllocation)
        {
            var activity = Activity.CreateActivity(
                typeof(IncrementExportCountsActivity),
                new Dictionary<Type, object> { { typeof(IEntityRepository), this.Repository } },
                SubmitActivityRequest)
                as DynamicAllocationActivity;

            var exportedAllocationIds = budgetAllocation.PerNodeResults.Values.Where(n => n.ExportBudget > 0).Select(n => n.AllocationId);
            var requestIds = string.Join(",", exportedAllocationIds);

            var request = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.IncrementExportCounts,
                Values =
                {
                    { EntityActivityValues.AuthUserId, this.UserId },
                    { EntityActivityValues.CompanyEntityId, this.CompanyEntityId },
                    { EntityActivityValues.CampaignEntityId, this.CampaignEntityId },
                    { DeliveryNetworkActivityValues.ExportedAllocationIds, requestIds }
                }
            };

            var result = activity.Run(request);
            if (!result.Succeeded)
            {
                throw new ActivityException("IncrementExportCountsActivity failed: {0}\n{1}"
                    .FormatInvariant(result.Error.ErrorId, result.Error.Message));
            }

            // Now we need to update the budget allocation passed in to reflect the updated count
            // in order for the output allocation to be correct
            var dac = new DynamicAllocationCampaign(this.Repository, this.CompanyEntityId, this.CampaignEntityId);
            var activeAllocation = dac.RetrieveActiveAllocation();
            
            foreach (var perNodeResult in activeAllocation.PerNodeResults)
            {
                var exportCount = perNodeResult.Value.ExportCount;
                if (exportCount != 0)
                {
                    budgetAllocation.PerNodeResults[perNodeResult.Key].ExportCount = exportCount;
                }
            }
        }

        /// <summary>Method to call the approve valuations activity</summary>
        /// <returns>The valuations json.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1811", Justification = "bah")]
        private string ApproveAndGetValuations()
        {
            // Simulate approving valuation inputs
            var approveActivity =
                Activity.CreateActivity(
                    typeof(ApproveValuationInputsActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.Repository } },
                    SubmitActivityRequest) as DynamicAllocationActivity;
            var approveRequest = new ActivityRequest
                {
                    Task = DynamicAllocationActivityTasks.ApproveValuationInputs,
                    Values =
                        {
                            { EntityActivityValues.AuthUserId, this.UserId },
                            { EntityActivityValues.CompanyEntityId, this.CompanyEntityId },
                            { EntityActivityValues.CampaignEntityId, this.CampaignEntityId }
                        }
                };

            var approveResult = approveActivity.Run(approveRequest);
            if (!approveResult.Succeeded)
            {
                throw new ActivityException(
                    "ApproveValuationInputs failed: {0}\n{1}".FormatInvariant(
                        approveResult.Error.ErrorId, approveResult.Error.Message));
            }

            var getValuationsActivity =
                Activity.CreateActivity(
                    typeof(GetValuationsActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.Repository } },
                    SubmitActivityRequest) as DynamicAllocationActivity;

            var getValuationsRequest = new ActivityRequest
            {
                Task = DynamicAllocationActivityTasks.GetValuations,
                Values =
                        {
                            { EntityActivityValues.AuthUserId, this.UserId },
                            { "ParentEntityId", this.CompanyEntityId },
                            { "EntityId", this.CampaignEntityId },
                            { "Approved", "TRUE" }
                        }
            };

            var getValuationsResult = getValuationsActivity.Run(getValuationsRequest);
            if (!approveResult.Succeeded)
            {
                throw new ActivityException(
                    "GetValuations failed: {0}\n{1}".FormatInvariant(
                        approveResult.Error.ErrorId, getValuationsResult.Error.Message));
            }

            var valuationsJson = "{{\"Valuations\":{0}}}".FormatInvariant(getValuationsResult.Values["Valuations"]);

            return valuationsJson;
        }

        /// <summary>Set up a campaign from the repository.</summary>
        private void SetupRepositoryCampaign()
        {
            this.PrimeLocalCampaignCache();

            var campaign = this.Repository.GetEntity<CampaignEntity>(null, this.CampaignEntityId);
            this.SetupCampaignAllocationParameters(campaign);

            if (!this.IsDryRun)
            {
                // Reset campaign to initial state
                campaign.Associations.Clear();
                var nodeMetrics = campaign.TryGetEntityPropertyByName(DynamicAllocationEntityProperties.AllocationNodeMetrics);
                if (nodeMetrics != null)
                {
                    campaign.Properties.Remove(nodeMetrics);
                }
            }

            this.Repository.SaveEntity(null, campaign);

            // Setup campaign owner
            var owner = campaign.GetOwnerId();
            this.UserId = owner;
            var campaignOwnerEntity = CreateTestUserEntity(this.UserId, "Test User");
            this.Repository.SaveUser(null, campaignOwnerEntity);
        }

        /// <summary>
        /// Performs initial synchronization of a local campaign with a campaign
        /// in the read-only store (prime the cache).
        /// </summary>
        private void PrimeLocalCampaignCache()
        {
            var context = new RequestContext
            {
                ExternalCompanyId = this.CompanyEntityId
            };

            // Touch the company
            var companyEntity = this.Repository.TryGetEntity(context, this.CompanyEntityId);
            if (companyEntity == null)
            {
                return;
            }

            // Touch the campaign
            var campaignEntity = this.Repository.TryGetEntity(context, this.CampaignEntityId);

            if (campaignEntity == null)
            {
                return;
            }

            // This will make sure explicit associations of the production campaign exist locally.
            // Implied associations (e.g - through an index) will have to be lazy loaded on demand.
            foreach (var association in campaignEntity.Associations)
            {
                if (this.Repository.TryGetEntity(context, association.TargetEntityId) == null)
                {
                    Console.WriteLine("Sync Association failed: {0}, {1}".FormatInvariant(association.ExternalName, association.TargetEntityId.ToString()));
                    return;
                }

                Console.WriteLine("Sync Association succeeded: {0}, {1}".FormatInvariant(association.ExternalName, association.TargetEntityId.ToString()));
            }
        }

        /// <summary>Setup the allocation parameters on the campaign</summary>
        /// <param name="testCampaign">The campaign.</param>
        private void SetupCampaignAllocationParameters(CampaignEntity testCampaign)
        {
            var configs = testCampaign.GetConfigSettings();
            foreach (var allocationParameter in this.CampaignAllocationParameters)
            {
                configs[allocationParameter.Key] = allocationParameter.Value;
            }

            testCampaign.SetConfigSettings(configs);
        }
    }
}
