// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AllocationParameters.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using ConfigManager;

namespace DynamicAllocation
{
    /// <summary>
    /// The set of allocation parameters. These enable some control over the details of the allocation
    /// on a per campaign basis
    /// </summary>
    public class AllocationParameters
    {
        /// <summary>The configuration</summary>
        private IConfig config;

        /// <summary>
        /// Initializes a new instance of the AllocationParameters class
        /// </summary>
        public AllocationParameters()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AllocationParameters class
        /// </summary>
        /// <param name="config">The configuration</param>
        public AllocationParameters(IConfig config) : this(null, config)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AllocationParameters class
        /// </summary>
        /// <param name="parameterOverrides">overrides for the config parameter values</param>
        public AllocationParameters(IDictionary<string, object> parameterOverrides)
            : this(parameterOverrides, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the AllocationParameters class
        /// </summary>
        /// <param name="parameterOverrides">overrides for the config parameter values</param>
        /// <param name="config">The configuration</param>
        public AllocationParameters(IDictionary<string, object> parameterOverrides, IConfig config)
        {
            this.config = config ?? new CustomConfig();

            var parameters = this.GetType().GetProperties();
            foreach (var parameter in parameters)
            {
                // TODO: add exception handling / logging
                object valueObject = null; 
                dynamic value = null; 

                // if there is an override, use that instead
                if (parameterOverrides != null && parameterOverrides.ContainsKey(parameter.Name))
                {
                    valueObject = parameterOverrides[parameter.Name];

                    // try to coerce the valueObject into parameter.PropertyType
                    try 
                    {
                        value = ConvertToParamterType(parameter, valueObject);
                    }
                    catch (Exception ex)
                    {
                        if (ex is InvalidCastException ||
                            ex is FormatException ||
                            ex is OverflowException)
                        {
                            // TODO: log error
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                value = value ?? this.GetConfigValue(parameter);
                parameter.SetValue(this, value, null);
            }
        }

        /// <summary>
        /// Gets or sets the Margin. The fraction we add to the spend for our margin.
        /// </summary>
        public decimal Margin { get; set; }
          
        /// <summary>
        /// Gets or sets the PerMilleFees. PerMilleFees are the serving fees currently. They are not counted as part of our margin.
        /// </summary>
        public decimal PerMilleFees { get; set; }
        
        /// <summary> 
        /// Gets or sets the DefaultEstimatedCostPerMille. the default eCpm we choose for lack of historical information for a node 
        /// </summary>
        public decimal DefaultEstimatedCostPerMille { get; set; }
       
        /// <summary>
        /// Gets or sets the InitialAllocationTotalPeriodDuration. the total length of time we will be doing intial allocation (currently one day)
        /// </summary>
        public TimeSpan InitialAllocationTotalPeriodDuration { get; set; }

        /// <summary>
        /// Gets or sets the InitialAllocationSinglePeriodDuration. 
        /// The length of each intitial allocation period (currently 4 hours)
        /// should be a divisor of InitialAllocationTotalPeriodDuration
        /// </summary>
        public TimeSpan InitialAllocationSinglePeriodDuration { get; set; }

        /// <summary>
        /// Gets or sets the AllocationTopTier. 
        /// The top tier to allocate to in the intial allocation
        /// </summary>
        public int AllocationTopTier { get; set; }
 
        /// <summary>
        /// Gets or sets the AllocationNumberofTiersToAllocateTo. 
        /// The number of tiers to allocate to in the intial allocation
        /// </summary>
        public int AllocationNumberOfTiersToAllocateTo { get; set; }

        /// <summary> 
        /// Gets or sets the BudgetBuffer. 
        /// the amount of headroom given to budgets during initial allocation to account 
        /// for the fact that some of the exported nodes will not spend their budget 
        /// </summary>
        public decimal BudgetBuffer { get; set; }

        /// <summary>
        /// Gets or sets the AllocationNumberOfNodes. 
        /// The number of nodes to allocate to in the intial allocation
        /// </summary>
        public int AllocationNumberOfNodes { get; set; }

        /// <summary>
        /// Gets or sets the InitialMaxNumberOfNodes. 
        /// The number of nodes to allocate to in the intial allocation per period. TODO: this makes AllocationNumberOfNodes redundant so it should be removed
        /// </summary>
        public int InitialMaxNumberOfNodes { get; set; }
        
        /// <summary>
        /// Gets or sets the MaxNodesToExport. 
        /// The maximum number of nodes we will export during reallocation
        /// doesn't include experimental nodes (see: UnderspendExperimentNodeCount)
        /// </summary>
        public int MaxNodesToExport { get; set; } 
     
        /// <summary>
        /// Gets or sets the ExportBudgetBoost. 
        /// boost to the export budgets that is added uniformly during 
        /// reallocation after nodes have been selected to export
        /// </summary>
        public decimal ExportBudgetBoost { get; set; } 
      
        /// <summary>
        /// Gets or sets the LargestBudgetPercentAllowed. 
        /// The largest percent of the period budget allowed on any single node during reallocation
        /// </summary>
        public decimal LargestBudgetPercentAllowed { get; set; } 
     
        /// <summary>
        /// Gets or sets the NeutralBudgetCappingTier. 
        /// Tier at which LargetBudgetPercentageAllowed is not adjusted.
        /// </summary>
        public int NeutralBudgetCappingTier { get; set; } 
     
        /// <summary>
        /// Gets or sets the LineagePenalty. 
        /// Penalty applied in ranking nodes of non-performing ancestors and descendants.
        /// </summary>
        public double LineagePenalty { get; set; } 
      
        /// <summary>
        /// Gets or sets the LineagePenaltyNeutral. 
        /// Neutral penalty (no penalty) applied in ranking nodes
        /// </summary>
        public double LineagePenaltyNeutral { get; set; }

        /// <summary>
        /// Gets or sets the MinimumImpressionCap
        /// this is the lowest impression cap that will be exported
        /// </summary>
        public long MinimumImpressionCap { get; set; }

        /// <summary>
        /// Gets or sets the MinBudget
        /// this is the lowest impression cap that will be exported
        /// </summary>
        public decimal MinBudget { get; set; }

        /// <summary>
        /// Gets or sets the insight threshold
        /// this is the target ratio of nodes in the allocation tiers for which we have some 
        /// strong delivery information vs the total number of nodes in those tiers 
        /// </summary>
        public double InsightThreshold { get; set; }

        /// <summary>
        /// Gets or sets the phase one exit percentage of the campaign
        /// When this percentage of camapign runtime has passed we exit phase one even without insight 
        /// </summary>
        public double PhaseOneExitPercentage { get; set; }

        /// <summary>
        /// Converts the valueObject into the parameter type, with a special case got handling TimeSpan strings
        /// </summary>
        /// <param name="parameter">the parameter</param>
        /// <param name="valueObject">the value as an object</param>
        /// <returns>the value as the parameter type</returns>
        private static dynamic ConvertToParamterType(PropertyInfo parameter, object valueObject)
        {
            var valueObjectAsString = valueObject as string;
            if (parameter.PropertyType == typeof(TimeSpan) && valueObjectAsString != null)
            {
                return TimeSpan.Parse(valueObjectAsString, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(valueObject, parameter.PropertyType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the config value for the paramter
        /// </summary>
        /// <param name="parameter">the parameter</param>
        /// <returns>the config value of the paramter</returns>
        private dynamic GetConfigValue(PropertyInfo parameter)
        {
            var valueObject = this.config.GetValue("DynamicAllocation." + parameter.Name);
            return ConvertToParamterType(parameter, valueObject);
        }
    }
}
