// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrentReportItem.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;

namespace ReportingActivities
{
    /// <summary>An item in a list of current reports.</summary>
    public class CurrentReportItem
    {
        /// <summary>Gets or sets the report date.</summary>
        public DateTime ReportDate { get; set; }
        
        /// <summary>Gets or sets the report blob entity id.</summary>
        public string ReportEntityId { get; set; }
    }
}