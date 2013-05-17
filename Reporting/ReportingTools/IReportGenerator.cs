// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IReportGenerator.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
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