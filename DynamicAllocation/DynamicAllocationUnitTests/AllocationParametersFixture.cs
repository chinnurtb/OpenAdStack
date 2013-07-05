// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationParametersFixture.cs" company="Rare Crowds Inc">
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
using System.Configuration;
using System.Linq;
using System.Text;
using ConfigManager;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DynamicAllocationUnitTests
{
    /// <summary>
    /// Test fixture for the AllocationParameters class
    /// </summary>
    [TestClass]
    public class AllocationParametersFixture
    { 
        /// <summary>
        /// Initialize the app settings before each test
        /// </summary>
        /// <param name="context">test context</param>
        [ClassInitialize]
        public static void InitializeClass(TestContext context)
        {
            TestUtilities.AllocationParametersDefaults.Initialize();
        }

        /// <summary>
        /// Test for the default constructor of AllocationParameters
        /// </summary>
        [TestMethod]
        public void AllocationParametersDefaultConstructor()
        {
            var actual = new AllocationParameters();

            Assert.AreEqual(actual.DefaultEstimatedCostPerMille, 1.5m);
            Assert.AreEqual(actual.Margin, 1m);
            Assert.AreEqual(actual.PerMilleFees, .06m);
            Assert.AreEqual(actual.BudgetBuffer, 1.1m);
            Assert.AreEqual(actual.InitialAllocationTotalPeriodDuration, TimeSpan.Parse("1.00:00:00"));
            Assert.AreEqual(actual.InitialAllocationSinglePeriodDuration, TimeSpan.Parse("6:00:00"));
            Assert.AreEqual(actual.AllocationTopTier, 7);
            Assert.AreEqual(actual.AllocationNumberOfTiersToAllocateTo, 4);
            Assert.AreEqual(actual.AllocationNumberOfNodes, 1000);
            Assert.AreEqual(actual.MaxNodesToExport, 500);
            Assert.AreEqual(actual.ExportBudgetBoost, 1);
            Assert.AreEqual(actual.LargestBudgetPercentAllowed, .03m);
            Assert.AreEqual(actual.NeutralBudgetCappingTier, 4);
            Assert.AreEqual(actual.LineagePenalty, .1);
            Assert.AreEqual(actual.LineagePenaltyNeutral, 1);
        }

        /// <summary>
        /// Test for the constructor of AllocationParameters that takes overrides of parameter values
        /// </summary>
        [TestMethod]
        public void AllocationParametersNonDefaultConstructorOneOverride()
        {
            var parameterOverrides = new Dictionary<string, object>
            {
                { "Margin", "10.1" }
            };

            var actual = new AllocationParameters(parameterOverrides);

            Assert.AreEqual(actual.DefaultEstimatedCostPerMille, 1.5m);
            Assert.AreEqual(actual.Margin, 10.1m);
            Assert.AreEqual(actual.PerMilleFees, .06m);
            Assert.AreEqual(actual.BudgetBuffer, 1.1m);
            Assert.AreEqual(actual.InitialAllocationTotalPeriodDuration, TimeSpan.Parse("1.00:00:00"));
            Assert.AreEqual(actual.InitialAllocationSinglePeriodDuration, TimeSpan.Parse("6:00:00"));
            Assert.AreEqual(actual.AllocationTopTier, 7);
            Assert.AreEqual(actual.AllocationNumberOfTiersToAllocateTo, 4);
            Assert.AreEqual(actual.AllocationNumberOfNodes, 1000);
            Assert.AreEqual(actual.MaxNodesToExport, 500);
            Assert.AreEqual(actual.ExportBudgetBoost, 1);
            Assert.AreEqual(actual.LargestBudgetPercentAllowed, .03m);
            Assert.AreEqual(actual.NeutralBudgetCappingTier, 4);
            Assert.AreEqual(actual.LineagePenalty, .1);
            Assert.AreEqual(actual.LineagePenaltyNeutral, 1);
        }

        /// <summary>
        /// Test initializing AllocationParameters with custom config and no overrides
        /// </summary>
        [TestMethod]
        public void AllocationParametersCustomConfigNoOverrides()
        {
            var parameterOverrides = new Dictionary<string, object>(0);
            var customConfig = new CustomConfig(new Dictionary<string, string>
            {
                { "DynamicAllocation.Margin", "10.1" },
                { "DynamicAllocation.MaxNodesToExport", "654" }
            });

            var actual = new AllocationParameters(parameterOverrides, customConfig);

            Assert.AreEqual(1.5m, actual.DefaultEstimatedCostPerMille);
            Assert.AreEqual(10.1m, actual.Margin);
            Assert.AreEqual(.06m, actual.PerMilleFees);
            Assert.AreEqual(1.1m, actual.BudgetBuffer);
            Assert.AreEqual(TimeSpan.Parse("1.00:00:00"), actual.InitialAllocationTotalPeriodDuration);
            Assert.AreEqual(TimeSpan.Parse("6:00:00"), actual.InitialAllocationSinglePeriodDuration);
            Assert.AreEqual(7, actual.AllocationTopTier);
            Assert.AreEqual(4, actual.AllocationNumberOfTiersToAllocateTo);
            Assert.AreEqual(1000, actual.AllocationNumberOfNodes);
            Assert.AreEqual(654, actual.MaxNodesToExport);
            Assert.AreEqual(1, actual.ExportBudgetBoost);
            Assert.AreEqual(.03m, actual.LargestBudgetPercentAllowed);
            Assert.AreEqual(4, actual.NeutralBudgetCappingTier);
            Assert.AreEqual(.1, actual.LineagePenalty);
            Assert.AreEqual(1, actual.LineagePenaltyNeutral);
        }

        /// <summary>
        /// Test initializing AllocationParameters with custom config and no overrides
        /// </summary>
        /// <remarks>Overrides should trump custom config</remarks>
        [TestMethod]
        public void AllocationParametersCustomConfigAndOverrides()
        {
            var parameterOverrides = new Dictionary<string, object>
            {
                { "Margin", "12.1" },
                { "AllocationNumberOfTiersToAllocateTo", 6 }
            };

            var customConfig = new CustomConfig(new Dictionary<string, string>
            {
                { "DynamicAllocation.Margin", "10.1" },
                { "DynamicAllocation.MaxNodesToExport", "654" }
            });

            var actual = new AllocationParameters(parameterOverrides, customConfig);

            Assert.AreEqual(1.5m, actual.DefaultEstimatedCostPerMille);
            Assert.AreEqual(12.1m, actual.Margin);
            Assert.AreEqual(.06m, actual.PerMilleFees);
            Assert.AreEqual(1.1m, actual.BudgetBuffer);
            Assert.AreEqual(TimeSpan.Parse("1.00:00:00"), actual.InitialAllocationTotalPeriodDuration);
            Assert.AreEqual(TimeSpan.Parse("6:00:00"), actual.InitialAllocationSinglePeriodDuration);
            Assert.AreEqual(7, actual.AllocationTopTier);
            Assert.AreEqual(6, actual.AllocationNumberOfTiersToAllocateTo);
            Assert.AreEqual(1000, actual.AllocationNumberOfNodes);
            Assert.AreEqual(654, actual.MaxNodesToExport);
            Assert.AreEqual(1, actual.ExportBudgetBoost);
            Assert.AreEqual(.03m, actual.LargestBudgetPercentAllowed);
            Assert.AreEqual(4, actual.NeutralBudgetCappingTier);
            Assert.AreEqual(.1, actual.LineagePenalty);
            Assert.AreEqual(1, actual.LineagePenaltyNeutral);
        }

        /// <summary>
        /// Test for the constructor of AllocationParameters that takes overrides of all parameter values
        /// </summary>
        [TestMethod]
        public void AllocationParametersNonDefaultConstructorAllOverrides()
        {
            var parameterOverrides = new Dictionary<string, object>
            {
                { "DefaultEstimatedCostPerMille", "2.5" },
                { "Margin", "1.7897123" },
                { "PerMilleFees", "1.1" },
                { "BudgetBuffer", "1.2" },
                { "InitialAllocationTotalPeriodDuration", "1.10:11:12" },
                { "InitialAllocationSinglePeriodDuration", "1.12:00:01" },
                { "AllocationTopTier", "14" },
                { "AllocationNumberOfTiersToAllocateTo", "5" },
                { "AllocationNumberOfNodes", "1234" },
                { "MaxNodesToExport", "234" },
                { "UnderSpendExperimentNodeCount", "12" },
                { "UnderSpendExperimentTier", "34" },
                { "MinBudget", "1.01" },
                { "ExportBudgetBoost", "1.34" },
                { "LargestBudgetPercentAllowed", ".34" },
                { "NeutralBudgetCappingTier", "3" },
                { "LineagePenalty", ".2" },
                { "LineagePenaltyNeutral", "10" },
            };

            var actual = new AllocationParameters(parameterOverrides);

            Assert.AreEqual(actual.DefaultEstimatedCostPerMille, 2.5m);
            Assert.AreEqual(actual.Margin, 1.7897123m);
            Assert.AreEqual(actual.PerMilleFees, 1.1m);
            Assert.AreEqual(actual.BudgetBuffer, 1.2m);
            Assert.AreEqual(actual.InitialAllocationTotalPeriodDuration, TimeSpan.Parse("1.10:11:12"));
            Assert.AreEqual(actual.InitialAllocationSinglePeriodDuration, TimeSpan.Parse("1.12:00:01"));
            Assert.AreEqual(actual.AllocationTopTier, 14);
            Assert.AreEqual(actual.AllocationNumberOfTiersToAllocateTo, 5);
            Assert.AreEqual(actual.AllocationNumberOfNodes, 1234);
            Assert.AreEqual(actual.MaxNodesToExport, 234);
            Assert.AreEqual(actual.ExportBudgetBoost, 1.34m);
            Assert.AreEqual(actual.LargestBudgetPercentAllowed, .34m);
            Assert.AreEqual(actual.NeutralBudgetCappingTier, 3);
            Assert.AreEqual(actual.LineagePenalty, .2);
            Assert.AreEqual(actual.LineagePenaltyNeutral, 10);
        }

        /// <summary>
        /// Test for the constructor of AllocationParameters that takes overrides of parameter values of various types
        /// </summary>
        [TestMethod]
        public void AllocationParametersNonDefaultConstructorVariousTypes()
        {
            var parameterOverrides = new Dictionary<string, object>
            {
                { "DefaultEstimatedCostPerMille", 2.5 }, // decimal as double
                { "Margin", 1 }, // decimal as int
                { "PerMilleFees", "1.1" }, // decimal as double string
                { "BudgetBuffer", "1.2" },
                { "InitialAllocationTotalPeriodDuration", "1.10:11:12" }, // timespan as string
                { "InitialAllocationSinglePeriodDuration", TimeSpan.Parse("1.12:00:01") }, // timespan as timespan
                { "AllocationTopTier", 14 }, // int as int
                { "AllocationNumberOfTiersToAllocateTo", 5.0 }, // int as double
                { "AllocationNumberOfNodes", "1234" }, // int as int string
                { "MaxNodesToExport", (double)234 }, // int as double
                { "UnderSpendExperimentNodeCount", ((double)12).ToString() }, // int as double to string
                { "UnderSpendExperimentTier", "34" },
                { "MinBudget", "1.01" },
                { "ExportBudgetBoost", "1.34" },
                { "LargestBudgetPercentAllowed", ".34" },
                { "NeutralBudgetCappingTier", "3" },
                { "LineagePenalty", ".2" },
                { "LineagePenaltyNeutral", "10" },
            };

            var actual = new AllocationParameters(parameterOverrides);

            Assert.AreEqual(actual.DefaultEstimatedCostPerMille, 2.5m);
            Assert.AreEqual(actual.Margin, 1m);
            Assert.AreEqual(actual.PerMilleFees, 1.1m);
            Assert.AreEqual(actual.BudgetBuffer, 1.2m);
            Assert.AreEqual(actual.InitialAllocationTotalPeriodDuration, TimeSpan.Parse("1.10:11:12"));
            Assert.AreEqual(actual.InitialAllocationSinglePeriodDuration, TimeSpan.Parse("1.12:00:01"));
            Assert.AreEqual(actual.AllocationTopTier, 14);
            Assert.AreEqual(actual.AllocationNumberOfTiersToAllocateTo, 5);
            Assert.AreEqual(actual.AllocationNumberOfNodes, 1234);
            Assert.AreEqual(actual.MaxNodesToExport, 234);
            Assert.AreEqual(actual.ExportBudgetBoost, 1.34m);
            Assert.AreEqual(actual.LargestBudgetPercentAllowed, .34m);
            Assert.AreEqual(actual.NeutralBudgetCappingTier, 3);
            Assert.AreEqual(actual.LineagePenalty, .2);
            Assert.AreEqual(actual.LineagePenaltyNeutral, 10);
        }
    }
}
