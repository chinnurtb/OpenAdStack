// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationParametersDefaults.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Configuration;

namespace TestUtilities
{
    /// <summary>
    /// Helper class to create default AppSettings for AllocationParamteres for testing
    /// </summary>
    public static class AllocationParametersDefaults
    {
        /// <summary>
        /// Sets default values for the AllocationParameter class to make it easy to initialize for testing
        /// </summary>
        public static void Initialize()
        {
            foreach (var configSetting in InitializeDictionary())
            {
                ConfigurationManager.AppSettings[configSetting.Key] = configSetting.Value;
            }
        }

        /// <summary>
        /// Sets default values for the AllocationParameter class to make it easy to initialize for testing
        /// </summary>
        /// <returns>The initialize dictionary.</returns>
        public static IDictionary<string, string> InitializeDictionary()
        {
            var configs = new Dictionary<string, string>();
            configs["DynamicAllocation.DefaultEstimatedCostPerMille"] = "1.5";
            configs["DynamicAllocation.Margin"] = "1";
            configs["DynamicAllocation.PerMilleFees"] = ".06";
            configs["DynamicAllocation.BudgetBuffer"] = "1.1";
            configs["DynamicAllocation.InitialAllocationTotalPeriodDuration"] = "1.00:00:00";
            configs["DynamicAllocation.InitialAllocationSinglePeriodDuration"] = "6:00:00";
            configs["DynamicAllocation.AllocationTopTier"] = "7";
            configs["DynamicAllocation.AllocationNumberOfTiersToAllocateTo"] = "4";
            configs["DynamicAllocation.AllocationNumberOfNodes"] = "1000";
            configs["DynamicAllocation.MaxNodesToExport"] = "500";
            configs["DynamicAllocation.UnderSpendExperimentNodeCount"] = "10";
            configs["DynamicAllocation.UnderSpendExperimentTier"] = "3";
            configs["DynamicAllocation.MinBudget"] = ".6";
            configs["DynamicAllocation.ExportBudgetBoost"] = "1";
            configs["DynamicAllocation.LargestBudgetPercentAllowed"] = ".03";
            configs["DynamicAllocation.NeutralBudgetCappingTier"] = "4";
            configs["DynamicAllocation.LineagePenalty"] = ".1";
            configs["DynamicAllocation.LineagePenaltyNeutral"] = "1";
            configs["DynamicAllocation.MinimumImpressionCap"] = "100";
            configs["DynamicAllocation.InitialMaxNumberOfNodes"] = "500";
            configs["DynamicAllocation.InsightThreshold"] = ".9";
            configs["DynamicAllocation.PhaseOneExitPercentage"] = ".5";
            return configs;
        }
    }
}
