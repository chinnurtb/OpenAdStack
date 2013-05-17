// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReportRunner.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using Activities;
using DataAccessLayer;
using Diagnostics;
using DynamicAllocation;
using EntityUtilities;
using ReportingActivities;
using ReportingUtilities;
using SimulatedDataStore;
using TestUtilities;
using Utilities.Serialization;
using daName = DynamicAllocationUtilities.DynamicAllocationEntityProperties;

namespace RunReport
{
    /// <summary>Class to manage running a report.</summary>
    public class ReportRunner
    {
        /// <summary>
        /// Gets or sets Repository.
        /// </summary>
        private IEntityRepository Repository { get; set; }

        /// <summary>Main run method.</summary>
        /// <param name="arguments">The command-line arguments.</param>
        public void Run(RunReportArgs arguments)
        {
            this.Initialize(arguments);

            var companyId = arguments.CompanyEntityId;
            var campaignId = arguments.CampaignEntityId;
            var outputDir = arguments.OutFile.FullName;

            // Load the company and campaign to touch the local cache (and provide an early fail)
            var context = new RequestContext { ExternalCompanyId = companyId };
            this.Repository.GetEntity(context, companyId);
            this.Repository.GetEntity(context, campaignId);
            
            var reportType = ReportTypes.ClientCampaignBilling;
            if (arguments.IsDataProviderReport)
            {
                reportType = ReportTypes.DataProviderBilling;
            }

            // Setup activity request
            var request = new ActivityRequest
            {
                Values =
                {
                    { EntityActivityValues.AuthUserId, "user123" },
                    { EntityActivityValues.CompanyEntityId, companyId },
                    { EntityActivityValues.CampaignEntityId, campaignId },
                    { ReportingActivityValues.ReportType, reportType }
                }
            };

            if (arguments.IsLegacy)
            {
                request.Values[ReportingActivityValues.SaveLegacyConversion] = string.Empty;
            }

            if (arguments.IsVerbose)
            {
                request.Values[ReportingActivityValues.VerboseReport] = string.Empty;
            }

            // Run the activity
            // Set up our activity
            var activity = Activity.CreateActivity(
                    typeof(CreateCampaignReportActivity),
                    new Dictionary<Type, object> { { typeof(IEntityRepository), this.Repository } },
                    SubmitActivityRequest) as CreateCampaignReportActivity;
            var result = activity.Run(request);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "CreateCampaignReportActivity failed. {0}".FormatInvariant(result.Error.Message));
            }

            // Load the report
            var campaignEntity = this.Repository.GetEntity<CampaignEntity>(context, campaignId);
            var currentReportsJson = campaignEntity.TryGetPropertyByName<string>(ReportingPropertyNames.CurrentReports, null);
            var currentReports = AppsJsonSerializer.DeserializeObject<Dictionary<string, CurrentReportItem>>(currentReportsJson);
            var reportDate = currentReports[reportType].ReportDate;
            var reportBlobId = currentReports[reportType].ReportEntityId;
            var reportBlob = this.Repository.GetEntity<BlobEntity>(context, reportBlobId);
            var report = reportBlob.DeserializeBlob<string>();

            // Write report to file
            WriteReport(outputDir, reportType, campaignEntity.ExternalName, report, reportDate);
        }

        /// <summary>Dummy method for activity call</summary>
        /// <param name="request">The activity request.</param>
        /// <param name="sourceName">The source name.</param>
        /// <returns>always true</returns>
        private static bool SubmitActivityRequest(ActivityRequest request, string sourceName)
        {
            return true;
        }

        /// <summary>Write a report to a csv file.</summary>
        /// <param name="path">Directory to write report.</param>
        /// <param name="reportType">The report type.</param>
        /// <param name="name">The report name.</param>
        /// <param name="report">The report string.</param>
        /// <param name="reportDate">The report date.</param>
        private static void WriteReport(string path, string reportType, string name, string report, DateTime reportDate)
        {
            var reportDay = reportDate.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
            var reportTime = reportDate.ToString("HH_mm", CultureInfo.InvariantCulture);

            var reportFile = @"{0}_{1}_{2}_{3}.csv".FormatInvariant(
                reportType, name, reportDay, reportTime);

            var fullPath = Path.Combine(Path.GetFullPath(path), reportFile);

            File.WriteAllText(fullPath, report);
        }

        /// <summary>Initialize runtime dependencies.</summary>
        /// <param name="arguments">The command-line arguments.</param>
        private void Initialize(RunReportArgs arguments)
        {
            var logFilePath = @"C:\logs\ReportRuns.log";
            if (arguments.LogFile != null)
            {
                logFilePath = arguments.LogFile.FullName;
            }

            LogManager.Initialize(new[]
                {
                    new FileLogger(logFilePath)
                });

            // TODO: This is a hacky way to get allocation params initialized. How do we have the same defaults
            // as prod?
            AllocationParametersDefaults.Initialize();
            ConfigurationManager.AppSettings["DynamicAllocation.Margin"] = "{0}".FormatInvariant(1 / 0.85);
            ConfigurationManager.AppSettings["DynamicAllocation.PerMilleFees"] = "{0}".FormatInvariant(0);

            MeasureSourceFactory.Initialize(new IMeasureSourceProvider[]
                {
                    new AppNexusActivities.Measures.AppNexusLegacyMeasureSourceProvider(),
                    new AppNexusActivities.Measures.AppNexusMeasureSourceProvider(),
                    new GoogleDfpActivities.Measures.DfpMeasureSourceProvider()
                });

            this.Repository = new SimulatedEntityRepository(
                ConfigurationManager.AppSettings["IndexLocal.ConnectionString"],
                ConfigurationManager.AppSettings["EntityLocal.ConnectionString"],
                ConfigurationManager.AppSettings["IndexReadOnly.ConnectionString"],
                ConfigurationManager.AppSettings["EntityReadOnly.ConnectionString"]);
        }
    }
}
