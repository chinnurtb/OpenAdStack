// -----------------------------------------------------------------------
// <copyright file="AccountManagement.cs" company="Rare Crowds Inc">
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
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SeleniumFramework.Driver;
using SeleniumFramework.Helpers;
using SeleniumFramework.Persistence;
using SeleniumFramework.Utilities;
using WebLayerTest.Helpers;

namespace WebLayerTest.TestScripts
{
    /// <summary>
    /// Test Class for Account Related Activities (Company/User creation etc.)
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class AccountManagement
    {
        #region ClassInitialize
        
        /// <summary>
        /// Initialize Method
        /// </summary>
        /// <param name="context">TestContext object</param>
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            // Verify whether the user is on the Home Page
            if (!UIHelper.VerifyPageTitle(Constants.HomePageTitle))
            {
                WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.HomePageUrl), ConfigReader.Website));                
            }
        }

        #endregion

        #region TestInitialize

        /// <summary>
        /// Method runs before each Test Method
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            // Check for login
            if (!SeleniumHelper.IsLogOnSuccessful)
            {
                Assert.Fail("User is not Logged in");
            }
        }

        #endregion

        #region TestMethods

        /// <summary>
        /// Test Method for creating Bulk Companies
        /// </summary>
        [TestMethod]
        [Ignore]
        public void CompanyCreate()
        {
            string errorMessage = string.Empty;
            List<TestResult> testResults = new List<TestResult>();

            string companyCreateSheetName = Constants.Get(Constants.CompanyCreateSheetName);
            DataTable companyTestData = ExcelHelper.GetDataTable(ConfigReader.DataFile, companyCreateSheetName);

            // Verify whether the user is on the Home Page
            if (!UIHelper.VerifyPageTitle(Constants.HomePageTitle))
            {
                WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.HomePageUrl), ConfigReader.Website));                
            }

            Utility.ImplicitWait();

            for (int i = 0; i < 4; i++)
            {
                // Navigate to the Company Page
                // UIHelper.ElementClick(Constants.Get(Constants.CompanyNavigationLink), ElementBy.XPath);
                WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.CompanyCreateUrl), ConfigReader.Website));

                // Explicit Wait
                Thread.Sleep(5000);
            }

            foreach (DataRow row in companyTestData.Rows)
            {
                // Verify whether user wants to create an Agency or Advertiser
                if (row["Role"].Equals("Agency"))
                {
                    // Click on the New Agency tab
                    string elementIdCompanyCreate = Constants.Get(Constants.CompanyCreationLink);
                    UIHelper.ElementClick(elementIdCompanyCreate, ElementBy.XPath);
                }
                else
                {
                    // Click on the New Advertiser tab
                    string elementIdAdvertiserCreate = Constants.Get(Constants.AdvertiserCreationLink);
                    UIHelper.ElementClick(elementIdAdvertiserCreate, ElementBy.XPath);
                }

                Utility.ImplicitWait();
          
                // Insert data in the fields for creating Agency/Advertiser
                bool status = SeleniumHelper.ProcessInputData(companyCreateSheetName, row, out errorMessage);

                // Explicit Wait
                Thread.Sleep(5000);

                TestStatus testStatus = TestStatus.Fail;

                // Data Verification
                if (status)
                {
                    // Verify if company page is loaded
                    ReadOnlyCollection<IWebElement> tableElements = null;
                    bool isCompanyPageLoaded = false;

                    while (!isCompanyPageLoaded)
                    {
                        // Redirected to Company page
                        WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.CompanyCreateUrl), ConfigReader.Website));

                        // Explicit Wait
                        Thread.Sleep(5000);

                        tableElements = WebDriverFactory.Driver.FindElements(By.XPath(Constants.Get(Constants.CompanyGridPath)));

                        if (tableElements != null && tableElements.Count > 1)
                        {
                            isCompanyPageLoaded = true;
                        }                        
                    }

                    int i = 1;
                    foreach (IWebElement element in tableElements)
                    {
                        if (i == 1)
                        {
                            i++;
                            continue;
                        }

                        if (element.Text.Contains(row["Agency_Name"].String()))
                        {
                            testStatus = TestStatus.Pass;
                            break;
                        }

                        i++;
                    }                    
                }

                if (testStatus == TestStatus.Fail)
                {
                    errorMessage = "Failed to create " + row["Agency_Name"] + " : status - " + status + " : " + errorMessage;
                }

                // Add Test Results to display
                testResults.Add(new TestResult()
                {
                    ScenarioId = row["Scenario_ID"].String(),
                    Mode = row["Mode"].String(),
                    Status = testStatus,
                    Message = errorMessage
                });
            }

            // Assert all test data
            TestHelper.AssertTest(testResults);
        }

        /// <summary>
        /// Test Method for the User Invitation
        /// </summary>
        [TestMethod]
        [TestCategory("NonBVT")]
        public void UserInvitation()
        {
            List<TestResult> testResults = new List<TestResult>();
            string errorMessage = string.Empty;
            string userInvitationSheetName = Constants.Get(Constants.UserInvitationSheetName);

            // Verify whether the user is on the Home Page
            if (!UIHelper.VerifyPageTitle(Constants.HomePageTitle))
            {
                WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.HomePageUrl), ConfigReader.Website));
            }

            // Click on the User Link to navigate to User page
            UIHelper.ElementClick(Constants.Get(Constants.UserNavigationLink), ElementBy.XPath);

            // Verification of page title
            if (!UIHelper.VerifyPageTitle(Constants.UserPageTitle))
            {
                // if user is not redirected on the Company Page
                Assert.Fail("User is not redirected to the Users Page");
            }

            // Click on the Create User link  
            UIHelper.ElementClick(Constants.Get(Constants.UserCreateLinkId), ElementBy.XPath);

            // Verification of User Invitation title
            if (!UIHelper.VerifyPageTitle(Constants.UserInvitationTitle))
            {
                // if user is not redirected on the Company Page
                Assert.Fail("User is not redirected to the User Invitation Page");
            }

            // Get User Invitation Test Data
            DataTable userTestData = ExcelHelper.GetDataTable(ConfigReader.DataFile, userInvitationSheetName);

            foreach (DataRow row in userTestData.Rows)
            {
                TestStatus testStatus = TestStatus.Fail;
                bool status = SeleniumHelper.ProcessInputData(userInvitationSheetName, row, out errorMessage);

                if (status)
                {
                    // TODO-Need to modify the expected text as we progress
                    if (UIHelper.CompareMessageText(row["Verification_Text"].String(), WebDriverFactory.Driver.FindElementById(Constants.Get(Constants.InvitationMessageId)).Text))
                    {
                        testStatus = TestStatus.Pass;
                    }
                    else
                    {
                        testStatus = TestStatus.Fail;
                    }
                }

                if (testStatus == TestStatus.Fail)
                {
                    errorMessage = "Error while Inviting User with Email : " + row["Email"] + Constants.Enter + errorMessage;
                }

                // Add Test Results to display
                testResults.Add(new TestResult()
                {
                    ScenarioId = row["Scenario_ID"].String(),
                    Mode = row["Mode"].String(),
                    Status = testStatus,
                    Message = errorMessage
                });
            }
            
            // Assert all test data
            TestHelper.AssertTest(testResults);
        }

        /// <summary>
        /// Test Method for reviewing User Verification Form 
        /// </summary>
        [TestMethod]
        [TestCategory("NonBVT")]
        public void UserVerification()
        {
            List<TestResult> testResults = new List<TestResult>();
            TestStatus testStatus = TestStatus.Pass;
            string errorMessage = string.Empty;

            // TODO - To be modified - Navigate to the Verification Form page
            WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.UserVerificationFormUrl), ConfigReader.Website));

            // Verify for the Verification Form Page Title
            if (!UIHelper.VerifyPageTitle(Constants.UserVerificationFormPageTitle))
            {
                Assert.Fail("User is not redirected to the Verification Form Page");
            }

            // Verify the presence of value in Email field
            if (!string.IsNullOrEmpty(WebDriverFactory.Driver.FindElementById(Constants.Get(Constants.UserVerificationFormEmailFieldId)).Text))
            {
                testStatus = TestStatus.Pass;

                // Click on the Confirm button
                UIHelper.ElementClick(Constants.Get(Constants.UserVerificationFormConfirmButtonId), ElementBy.Id);

                // Verify that user is navigated to Company page
                if (!UIHelper.VerifyPageTitle(Constants.CompanyPageTitle))
                {
                    testStatus = TestStatus.Fail;
                    errorMessage = "User is not redirected to the Company Page";
                }
            }
            else
            {
                testStatus = TestStatus.Fail;
                errorMessage = "User verification form does not contains the correct details of the User";
            }

            // Add Test Results to display
            testResults.Add(new TestResult()
            {
                Status = testStatus,
                Message = errorMessage
            });

            // Assert all test data
            TestHelper.AssertTest(testResults);
        }

        #endregion
    }
}
