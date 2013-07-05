// -----------------------------------------------------------------------
// <copyright file="TestHelper.cs" company="Rare Crowds Inc">
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
