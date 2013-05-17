// -----------------------------------------------------------------------
// <copyright file="CampaignManagement.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
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
    /// Test Script for all the Campaign related work flows (Create Campaigns etc.)
    /// </summary>
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class CampaignManagement
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

        #region Test Methods

        /// <summary>
        /// Test Method for creating Campaigns
        /// </summary>
        [TestMethod]
        [TestCategory("NonBVT")]
        public void CampaignCreate()
        {
            string errorMessage = string.Empty;
            List<TestResult> testResults = new List<TestResult>();

            string campaignCreateSheetName = Constants.Get(Constants.CampaignCreateSheetName);
            DataTable campaignTestData = ExcelHelper.GetDataTable(ConfigReader.DataFile, campaignCreateSheetName);

            // Verify whether the user is on the Home Page
            if (!UIHelper.VerifyPageTitle(Constants.HomePageTitle))
            {
                WebDriverFactory.Driver.NavigateUrl(string.Format(Constants.Get(Constants.HomePageUrl), ConfigReader.Website));
            }

            // Navigate to the Campaign Page
            UIHelper.ElementClick(Constants.Get(Constants.CampaignPageNavigationLink), ElementBy.XPath);
         
            foreach (DataRow row in campaignTestData.Rows)
            {
                TestStatus testStatus = TestStatus.Fail;

                UIHelper.ElementClick(Constants.Get(Constants.CampaignPageCreateNew), ElementBy.XPath);
                IWebElement campaignFrame = WebDriverFactory.Driver.FindElementByXPath(Constants.Get(Constants.CampaignCreateFrameId));
                WebDriverFactory.Driver.SwitchTo().Frame(campaignFrame);
                
                // Create Campaign
                bool status = SeleniumHelper.ProcessInputData(campaignCreateSheetName, row, out errorMessage);

                if (status)
                {
                    // Data Verification
                    if (UIHelper.CompareMessageText(row["Verification_Text"].String(), WebDriverFactory.Driver.FindElementByXPath(Constants.Get(Constants.CampaignCreationId)).Text))
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
                    errorMessage = "Error while creating Campaign : " + row["Campaign_Name"] + Constants.Enter + errorMessage;
                }

                // Add Test Results to display
                testResults.Add(new TestResult()
                {
                    ScenarioId = row["Scenario_ID"].String(),
                    Mode = row["Mode"].String(),
                    Status = testStatus,
                    Message = errorMessage
                });

                WebDriverFactory.Driver.SwitchTo().DefaultContent();
            }

            // Assert all test data
            TestHelper.AssertTest(testResults);
        }

        #endregion
    }
}
