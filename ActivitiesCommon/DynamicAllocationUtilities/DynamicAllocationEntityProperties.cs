//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationEntityProperties.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DynamicAllocationUtilities
{
    /// <summary>
    /// Names of the entity properties in which Dynamic Allocation related values are kept
    /// </summary>
    public static class DynamicAllocationEntityProperties
    {
        /// <summary>Association name for the active allocation set</summary>
        public const string AllocationSetActive = "DAAllocationSetActive";

        /// <summary>Association name for the allocations history</summary>
        public const string AllocationsHistory = "DAAllocationsHistory";

        /// <summary>ExternalType name for the delivery data</summary>
        public const string DeliveryData = "DADeliveryData";
         
        /// <summary>Association name for the period delivery data</summary>
        public const string DeliveryDataForPeriod = "DADeliveryDataForPeriod";

        /// <summary>Property name of the raw delivery data payload on the storage entity.</summary>
        public const string RawDeliveryDataEntityPayloadName = "DeliveryDataPayload";

        /// <summary>Association name for the map from AllocationIds to MeasureSets</summary>
        public const string AllocationNodeMap = "DAAllocationNodeMap";

        /// <summary>Name in the allocations history for inputs history</summary>
        public const string AllocationInputsHistory = "BudgetAllocationInputsHistory";

        /// <summary>Name in the allocations history for outputs history (TODO: change this name to be better)</summary>
        public const string AllocationOutputsHistory = "BudgetAllocationsHistory";

        /// <summary>Name of the Budget Allocations history index for inputs and outputs</summary>
        public const string AllocationHistoryIndex = "DAAllocationHistoryIndex";

        /// <summary>Association name of the campaign configuration</summary>
        public const string CampaignConfiguration = "CampaignConfiguration";

        /// <summary>Name of the allocation parameters within the campaign configuration dictionary</summary>
        public const string AllocationParameters = "AllocationParameters";

        /// <summary>Association name of the custom measure map</summary>
        public const string MeasureMap = "MeasureMap";

        /// <summary>Property name of the node delivery metrics.</summary>
        public const string AllocationNodeMetrics = "AllocationNodeMetrics";

        /// <summary>CampaignEntity property name for InitializationPhaseComplete</summary>
        public const string InitializationPhaseComplete = "DAInitializationPhaseComplete";

        /// <summary>CampaignEntity property name for RemainingBudget</summary>
        public const string RemainingBudget = "RemainingBudget";

        /// <summary>CampaignEntity property name for lifetime media budget cap</summary>
        public const string LifetimeMediaBudgetCap = "APNXLifetimeMediaBudgetCap";

        /// <summary>CampaignEntity property name for MeasureList valuation inputs.</summary>
        public const string MeasureList = "MeasureInfoSet";

        /// <summary>CampaignEntity property name for NodeValuationSet valuation inputs.</summary>
        public const string NodeValuationSet = "NodeValuationSet";

        /// <summary>CampaignEntity approval status name.</summary>
        public const string Status = "Status";

        /// <summary>CampaignEntity approved status value.</summary>
        public const string StatusApproved = "Approved";

        /// <summary>CampaignEntity draft status value.</summary>
        public const string StatusDraft = "Draft";

        /// <summary>CampaignEntity property name for cached valuations.</summary>
        public const string CachedValuations = "CachedValuations";

        /// <summary>CampaignEntity property name for fingerprint of valuation inputs matching cached valuations.</summary>
        public const string ValuationInputsFingerprint = "ValuationInputsFingerprints";

        /// <summary>Version of campaign with approved inputs.</summary>
        public const string InputsApprovedVersion = "DAInputsApprovedVersion";
    }
}
