// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IReportGenerator.cs" company="Rare Crowds Inc">
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

using System.Text;

namespace ReportingTools
{
    /// <summary>
    /// Interface definition for a class that supports report generation.
    /// </summary>
    public interface IReportGenerator
    {
        /// <summary>Build an report of the specified type.</summary>
        /// <param name="reportType">A ReportTypes string constant.</param>
        /// <param name="verbose">True for a verbose report.</param>
        /// <returns>A StringBuilder with the report.</returns>
        StringBuilder BuildReport(string reportType, bool verbose);
    }
}