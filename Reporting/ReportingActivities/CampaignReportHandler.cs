// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CampaignReportHandler.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Activities;
using DataAccessLayer;
using DynamicAllocation;
using ReportingTools;
using ReportingUtilities;
using Utilities;
using Utilities.Serialization;

namespace ReportingActivities
{
    /// <summary>Class to build a dynamic allocation report.</summary>
    public class CampaignReportHandler : IActivityHandler
    {
        /// <summary>Initializes a new instance of the <see cref="CampaignReportHandler"/> class.</summary>
        /// <param name="repository">Entity repository instance.</param>
        /// <param name="reportGenerators">The report generator for the report type.</param>
        /// <param name="companyEntityId">Company entity id.</param>
        /// <param name="campaignEntityId">Campaign entity id.</param>
        /// <param name="buildVerbose">Set true to build a verbose report.</param>
        /// <param name="reportType">The report type to build.</param>
        public CampaignReportHandler(
            IEntityRepository repository, 
            IDictionary<DeliveryNetworkDesignation, IReportGenerator> reportGenerators,
            EntityId companyEntityId,
            EntityId campaignEntityId, 
            bool buildVerbose, 
            string reportType)
        {
            if (repository == null)
            {
                throw new AppsGenericException("Null repository instance passed to CampaignReportHandler constructor.");
            }

            if (reportGenerators == null)
            {
                throw new AppsGenericException("Null report generators collection passed to CampaignReportHandler constructor.");
            }

            if (companyEntityId == null)
            {
                throw new AppsGenericException("Null company entity id passed to CampaignReportHandler constructor.");
            }

            if (campaignEntityId == null)
            {
                throw new AppsGenericException("Null campaign entity id passed to CampaignReportHandler constructor.");
            }

            if (string.IsNullOrEmpty(reportType))
            {
                throw new AppsGenericException("Null or empty report type passed to CampaignReportHandler constructor.");
            }

            this.Repository = repository;
            this.ReportGenerators = reportGenerators;
            this.CompanyEntityId = companyEntityId;
            this.CampaignEntityId = campaignEntityId;
            this.BuildVerbose = buildVerbose;
            this.ReportType = reportType;
        }

        /// <summary>Gets the Repository.</summary>
        internal IEntityRepository Repository { get; private set; }

        /// <summary>Gets the report generator for the type of report.</summary>
        internal IDictionary<DeliveryNetworkDesignation, IReportGenerator> ReportGenerators { get; private set; }

        /// <summary>Gets company entity id.</summary>
        internal EntityId CompanyEntityId { get; private set; }

        /// <summary>Gets campaign entity id.</summary>
        internal EntityId CampaignEntityId { get; private set; }

        /// <summary>Gets a value indicating whether to build a verbose report.</summary>
        internal bool BuildVerbose { get; private set; }

        /// <summary>Gets a constant representing the type of report to build.</summary>
        internal string ReportType { get; private set; }

        /// <summary>Execute the activity handler.</summary>
        /// <returns>The activity result.</returns>
        public IDictionary<string, string> Execute()
        {
            if (this.ReportGenerators.Count == 0)
            {
                throw new AppsGenericException("No report generators specified for report {0} on campaign {1}."
                    .FormatInvariant(this.ReportType, this.CampaignEntityId));
            }

            // TODO: Support multiple report generator merge
            if (this.ReportGenerators.Count > 1)
            {
                throw new AppsGenericException("Multiple report generators not supported yet on campaign {0}."
                    .FormatInvariant(this.CampaignEntityId));
            }

            var deliveryNetwork = this.ReportGenerators.First().Key;
            if (deliveryNetwork != DeliveryNetworkDesignation.AppNexus)
            {
                throw new AppsGenericException("Delivery network {0} not currently supported."
                    .FormatInvariant(deliveryNetwork));
            }

            var reportGenerator = this.ReportGenerators[deliveryNetwork];
            var report = reportGenerator.BuildReport(this.ReportType, this.BuildVerbose);
            this.SaveReport(report);

            // No result values are returned on success
            return new Dictionary<string, string>();
        }

        /// <summary>Save the report as a blob and update the reference in the campaign.</summary>
        /// <param name="report">The report</param>
        private void SaveReport(StringBuilder report)
        {
            var context = new RequestContext
                {
                    ExternalCompanyId = this.CompanyEntityId,
                    EntityFilter = new RepositoryEntityFilter(true, false, false, false)
                };
            
            // Build and save the report blob
            var reportBlobId = new EntityId();
            var reportBlob = BlobEntity.BuildBlobEntity(reportBlobId, report.ToString());
            reportBlob.LastModifiedDate = DateTime.UtcNow;
            var reportBlobSaved = this.Repository.TrySaveEntity(context, reportBlob);
            if (!reportBlobSaved)
            {
                throw new AppsGenericException(
                    "Report blob could not be saved. Campaign {0}, Report Blob {1}.".FormatInvariant(
                        this.CampaignEntityId, reportBlobId));
            }

            // Get the list of current report references
            var campaignEntity = this.Repository.GetEntity<CampaignEntity>(context, this.CampaignEntityId);

            var currentReportsJson = campaignEntity.TryGetPropertyByName<string>(ReportingPropertyNames.CurrentReports, null);
            var currentReports = new Dictionary<string, CurrentReportItem>();
            if (!string.IsNullOrEmpty(currentReportsJson))
            {
                currentReports = AppsJsonSerializer.DeserializeObject<Dictionary<string, CurrentReportItem>>(currentReportsJson);
            }

            // Replace any existing report of this type
            currentReports[this.ReportType] = new CurrentReportItem
                {
                    ReportDate = reportBlob.LastModifiedDate, ReportEntityId = reportBlobId
                };
            currentReportsJson = AppsJsonSerializer.SerializeObject(currentReports);

            // Update the entity. Last one in wins.
            var currentReportsProperty = new EntityProperty(ReportingPropertyNames.CurrentReports, currentReportsJson);
            var reportSaved = this.Repository.TryUpdateEntity(
                context, this.CampaignEntityId, new List<EntityProperty> { currentReportsProperty });

            if (!reportSaved)
            {
                throw new AppsGenericException(
                    "Campaign could not be saved with new report reference. Campaign {0}, Report Blob {1}.".FormatInvariant(
                        this.CampaignEntityId, reportBlobId));
            }
        }
    }
}
