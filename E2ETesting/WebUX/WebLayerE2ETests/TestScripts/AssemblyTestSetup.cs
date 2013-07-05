//-----------------------------------------------------------------------
// <copyright file="AssemblyTestSetup.cs" company="Rare Crowds Inc">
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
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SeleniumFramework.Driver;
using SeleniumFramework.Persistence;
using SeleniumFramework.Utilities;
using WebLayerTest.Helpers;

namespace SeleniumFramework.TestScripts
{
    /// <summary>
    /// This class contains methods which will be executed when Test execution starts and ends
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class AssemblyTestSetup
    {
        #region Variables
        
        /// <summary>
        /// Count the no of times TestSuiteInitialize is executed
        /// </summary>
        private static int count;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the test context which provides information about and functionality for the current test run
        /// </summary>
        public TestContext TestContext { get; set; }

        #endregion

        #region AssemblyInitialize

        /// <summary>
        /// Method executes before Test suite execution start
        /// </summary>
        /// <param name="context">TestContext object</param>
        [AssemblyInitialize]
        public static void TestSuiteInitialize(TestContext context)
        {
            if (count++ == 0)
            {
                // Create WebDriver object and Navigate to Home page
                // TODO - To be changed as we progress
                WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.HomePageUrl), ConfigReader.Website));

                // Set the Implicit WaitTime to tell Webdriver to poll the DOM for a certain amount of time when trying to find an element  
                WebDriverFactory.Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(ConfigReader.ImplicitWaitTime));

                string errorMessage = string.Empty;

                // Login to Lucy
                if (!SeleniumHelper.VerifyLogOn())
                {
                    // Login to Lucy application
                    errorMessage = SeleniumHelper.LogOnActivity();
                }

                // Verify the Login
                if (!SeleniumHelper.IsLogOnSuccessful)
                {
                    Assert.Fail("Login failed for user " + SeleniumHelper.LoggedInUsername + "/" + SeleniumHelper.LoggedInPassword + " in AssemblyInitialize " + errorMessage);
                    System.Environment.Exit(0);
                }
            }
        }

        #endregion

        #region AssemblyCleanup

        /// <summary>
        /// Method executes after Test suite execution ends
        /// </summary>
        [AssemblyCleanup]
        public static void TestSuiteCleanup()
        {
            if (--count == 0)
            {
                // Close browser
                WebDriverFactory.CloseWebDriver();
            }
        }

        #endregion
    }
}
