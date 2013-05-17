// -----------------------------------------------------------------------
// <copyright file="TestResult.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// TestResult schema for storing Test Data and Status
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Gets or sets Scenario Id for Test
        /// </summary>
        public string ScenarioId { get; set; }

        /// <summary>
        /// Gets or sets Test Mode (Add/Update...)
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets Test Status
        /// </summary>
        public TestStatus Status { get; set; }

        /// <summary>
        /// Gets or sets Message associated with Test data
        /// </summary>
        public string Message { get; set; }
    }
}
