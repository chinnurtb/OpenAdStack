// -----------------------------------------------------------------------
// <copyright file="TestHelper.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SeleniumFramework.Utilities;

namespace WebLayerTest.Helpers
{
    /// <summary>
    /// Test Helper component to build the test results table 
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class TestHelper
    {
        /// <summary>
        /// Test Result Row Template
        /// </summary>
        private static string testResultRow = "{0}\t{1}\t{2}\t{3}";

        /// <summary>
        /// Verify the test results and build results table to display
        /// </summary>
        /// <param name="testResults">Processed test data collection</param>
        public static void AssertTest(List<TestResult> testResults)
        {
            bool isTestPassed = true;
            StringBuilder strResults = new StringBuilder(string.Empty);

            foreach (var result in testResults)
            {
                strResults.AppendLine(string.Format(testResultRow, result.ScenarioId, result.Mode, result.Status, result.Message));

                if (result.Status == TestStatus.Fail)
                {
                    isTestPassed = false;
                }
            }

            Assert.IsTrue(isTestPassed, (isTestPassed ? "Test passed" : "Test failed") + Environment.NewLine + strResults);    
        }
    }
}
