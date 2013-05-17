//-----------------------------------------------------------------------
// <copyright file="DynamicAllocationActivityUtilities.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;
using Activities;
using DataAccessLayer;
using DeliveryNetworkUtilities;
using Diagnostics;
using DynamicAllocation;

namespace DynamicAllocationUtilities
{
    /// <summary>Utilities for activities working with DynamicAllocation</summary>
    public static class DynamicAllocationActivityUtilities
    {
        /// <summary>Makes an name for an export unit (AppNexus campaign, DFP line-item, etc)</summary>
        /// <remarks>
        /// WARNING: Changes to the campaign name format here require corresponding
        /// changes to GetAllocationIdFromCampaignName which may break compatibility
        /// with currently running campaigns.
        /// </remarks>
        /// <param name="allocation">The allocation</param>
        /// <param name="campaignEntity">The campaign entity</param>
        /// <param name="measures">The measures</param>
        /// <param name="lifetimeBudget">The lifetime budget</param>
        /// <returns>The export unit name</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only valid for CampaignEntities")]
        public static string MakeExportUnitNameForAllocation(PerNodeBudgetAllocationResult allocation, CampaignEntity campaignEntity, MeasureSet measures, decimal lifetimeBudget)
        {
            var values = new object[]
            {
                campaignEntity.ExternalName.ToString().Replace("--", "_"),
                allocation.AllocationId,
                measures.Count,
                lifetimeBudget,
                allocation.PeriodMediaBudget,
                allocation.ExportBudget
            };
            return string.Join("--", values);
        }

        /// <summary>Parses the AllocationId from an export unit's name</summary>
        /// <remarks>
        /// WARNING: Must be kept in sync with MakeCampaignName. Any changes must
        /// maintain backwards compatibility with currently running campaigns.
        /// </remarks>
        /// <param name="exportUnitName">The export unit's name</param>
        /// <returns>The AllocationId for the export unit</returns>
        public static string ParseAllocationIdFromExportUnitName(string exportUnitName)
        {
            var nameParts = exportUnitName.Split(new[] { "--" }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 3)
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Invalid campaign name: '{0}'. Unable to get allocation id from export unit name.",
                    exportUnitName);
                return null;
            }

            // AllocationId is the second name part, following the CampaignEntity name
            var allocationId = nameParts[1];

            // IMPORTANT: This check must be kept up-to-date with any changes to
            // the assignment of AllocationIds in the allocationNodeMap in
            // GetBudgetAllocationsActivity.ProcessRequest
            Guid guid;
            if (!Guid.TryParse(allocationId, out guid))
            {
                LogManager.Log(
                    LogLevels.Warning,
                    "Invalid export unit name: '{0}'. '{1}' is not a valid AllocationId.",
                    exportUnitName,
                    allocationId);
                return null;
            }

            return allocationId;
        }

        /// <summary>Gets a measure map for the given campaign, owner and company</summary>
        /// <param name="companyEntity">Company entity</param>
        /// <param name="campaignEntity">Campaign entity</param>
        /// <param name="campaignOwner">Campaign owner</param>
        /// <returns>The MeasureMap</returns>
        public static MeasureMap GetMeasureMap(CompanyEntity companyEntity, CampaignEntity campaignEntity, UserEntity campaignOwner)
        {
            return new MeasureMap(
                MeasureSourceFactory.CreateMeasureSources(
                    campaignEntity.GetDeliveryNetwork(),
                    campaignEntity.GetExporterVersion(),
                    companyEntity,
                    campaignEntity,
                    campaignOwner));
        }

        /// <summary>
        /// Verifies the campaign's valuation inputs are acceptable to submit
        /// the approve valuation inputs activity request.
        /// </summary>
        /// <param name="companyEntity">Company entity</param>
        /// <param name="campaignEntity">Campaign entity</param>
        /// <param name="campaignOwner">Campaign owner</param>
        /// <exception cref="ActivityException">
        /// Thrown with ActivityErrorId.MissingRequiredInput if any required valuation input values are missing/invalid.
        /// </exception>
        public static void VerifyHasRequiredValuationInputs(CompanyEntity companyEntity, CampaignEntity campaignEntity, UserEntity campaignOwner)
        {
            var measureIds = GetAllMeasureIds(campaignEntity);
            var measureMap = GetMeasureMap(companyEntity, campaignEntity, campaignOwner);
            VerifyMeasuresHaveDataCosts(measureIds, measureMap);
        }

        /// <summary>Gets all measure ids in the campaign's measure info list</summary>
        /// <param name="campaignEntity">Campaign entity</param>
        /// <returns>The measure ids</returns>
        [SuppressMessage("Microsoft.Design", "CA1011", Justification = "Only appropriate for campaigns")]
        public static long[] GetAllMeasureIds(CampaignEntity campaignEntity)
        {
            var measureListInfoJson = campaignEntity.TryGetPropertyValueByName(DynamicAllocationEntityProperties.MeasureList);
            if (measureListInfoJson == null)
            {
                throw new ActivityException(ActivityErrorId.MissingRequiredInput, "Missing measure info");
            }

            try
            {
                var measureListInfo = new JavaScriptSerializer().Deserialize<IDictionary<string, object>>(measureListInfoJson);
                return ((object[])measureListInfo["Measures"])
                    .Cast<IDictionary<string, object>>()
                    .Select(measure => measure["measureId"])
                    .Select(id => Convert.ToInt64(id, CultureInfo.InvariantCulture))
                    .ToArray();
            }
            catch (Exception e)
            {
                throw new ActivityException(ActivityErrorId.MissingRequiredInput, "Malformed measure info", e);
            }
        }

        /// <summary>Verifies all measures have defined data costs</summary>
        /// <param name="measureIds">The measure ids</param>
        /// <param name="measureMap">The measure map</param>
        /// <exception cref="ActivityException">
        /// Thrown with ActivityErrorId.MissingRequiredInput if any measures have undefined data costs
        /// </exception>
        private static void VerifyMeasuresHaveDataCosts(long[] measureIds, MeasureMap measureMap)
        {
            var undefinedDataCostMeasureIds = measureIds
                .Where(id =>
                {
                    try
                    {
                        var provider = measureMap.TryGetDataProviderForMeasure(id);
                        return string.IsNullOrWhiteSpace(provider) || provider == MeasureInfo.DataProviderUnknown;
                    }
                    catch (KeyNotFoundException)
                    {
                        return true;
                    }
                })
                .ToArray();
            if (undefinedDataCostMeasureIds.Length > 0)
            {
                var message =
                    "The following measures do not have data costs defined. Provide data costs or remove the measures.\nMeasures: {0}"
                    .FormatInvariant(string.Join(", ", undefinedDataCostMeasureIds));
                throw new ActivityException(ActivityErrorId.MissingRequiredInput, message);
            }
        }
    }
}
