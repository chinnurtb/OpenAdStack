// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CurrentReportItem.cs" company="Rare Crowds Inc">
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