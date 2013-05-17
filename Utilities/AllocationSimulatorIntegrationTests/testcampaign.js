{
	"Campaign" : {
		"ExternalEntityId" : "8b9a3f92cb1d4f5dbd5e24ac951d0538",
		"EntityCategory" : "Campaign",
		"CreateDate" : "2012-05-14T15:20:46.0000000Z",
		"LastModifiedDate" : "2012-06-15T20:42:15.0768970Z",
		"LastModifiedUser" : "1AD0DD27B0DE4605B8DA6F5C6C26A9E8",
		"LocalVersion" : "553",
		"ExternalName" : "Some Campaign for Simulating",
		"ExternalType" : "DynamicAllocationCampaign",
		"Properties" : {
			"Status" : "Approved",
			"Budget" : "2000",
			"StartDate" : "2012-10-15T00:00:00.0000000Z",
			"EndDate" : "2012-10-25T00:01:00.0000000Z",
			"DA_InitialAllocationDone" : "true",
			"APNX_LineItemId" : "148991",
			"RemainingBudget" : "2000",
		},
		"ExtendedProperties" : {
			"MeasureInfoSet" : {
				"IdealValuation" : "5.0",
				"MaxValuation" : "10.0",
				"Measures" : [{
						"measureId" : "107000000000018024",
                		"valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "107000000000025034",
					    "valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "107000000000035044",
					    "valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "110000000000142534",
					    "valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "110000000000142535",
    					"valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "110000000000142539",
    					"valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "110000000000142540",
					    "valuation" : 50,
						"group" : "",
						"pinned" : false
					}, {
						"measureId" : "110000000000142544",
    					"valuation" : 50,
						"group" : "",
						"pinned" : false
					}
				]
			},
			"NodeValuationSet" : []
		},
		"SystemProperties" : {
			"SimulatorConfig" : {
				"RandPercentage" : "0.50",
				"BaseTierDeliveryProbability" : ".80",
				"DeliveryProbilityDecayRate" : ".75",
				"RandomSeed" : "-1",
				"AverageEcpm" : "1",
				"RateBaseTier" : "4",
				"RateTopTier" : "6",
				"BaseTierRate" : "2",
				"AllocationTopTierRate" : ".01",
				"MeasureRateDeviationPercentage" : ".9",
				"DeliverySimulationType" : "2"
			},
			"AllocationParameters" : {
				"DefaultEstimatedCostPerMille" : "1.5",
				"Margin" : "1",
				"PerMilleFees" : ".06",
				"BudgetBuffer" : "1.1",
				"InitialAllocationTotalPeriodDuration" : "1.00:00:00",
				"InitialAllocationSinglePeriodDuration" : "6:00:00",
				"AllocationTopTier" : "7",
				"AllocationNumberOfTiersToAllocateTo" : "4",
				"AllocationNumberOfNodes" : "50",
				"MaxNodesToExport" : "25",
				"UnderSpendExperimentNodeCount" : "10",
				"UnderSpendExperimentTier" : "3",
				"MinBudget" : ".6",
				"ExportBudgetBoost" : "1",
				"LargestBudgetPercentAllowed" : ".02",
				"NeutralBudgetCappingTier" : "4",
				"LineagePenalty" : ".1",
				"LineagePenaltyNeutral" : "1",
				"MinimumImpressionCap" : "165",
				"InitialMaxNumberOfNodes" : "50"
			}
		}
	}
}