// -----------------------------------------------------------------------
// <copyright file="TestResult.cs" company="Rare Crowds Inc">
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
