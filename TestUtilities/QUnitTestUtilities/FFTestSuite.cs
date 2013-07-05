//-----------------------------------------------------------------------
// <copyright file="FFTestSuite.cs" company="Rare Crowds Inc">
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
// <remarks>
//      You may find some additional information about this at https://github.com/Sqdw/SQUnit.
// </remarks>
//-----------------------------------------------------------------------

using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenQA.Selenium;

namespace TestUtilities
{
    /// <summary>
    /// The class that takes care of firing a FireFox session using Selenium and parsing the DOM of the page to extract the QUnit information.
    /// </summary>
    public class FFTestSuite
    {
        /// <summary>
        /// Screenshot image format
        /// </summary>
        private static readonly ImageFormat ScreenshotFormat = ImageFormat.Png;

        /// <summary>
        /// Instance of the interface through which we can control the browser
        /// </summary>
        private readonly IWebDriver driver;

        /// <summary>
        /// The URI of the test page; either a URL or a file path
        /// </summary>
        private readonly string testFilePath;

        /// <summary>
        /// QUnit tests container of the test page
        /// </summary>
        private IWebElement qunitTestsElement;

        /// <summary>
        /// Initializes a new instance of the FFTestSuite class
        /// </summary>
        /// <param name="webDriver">Instance of the interface through which we can control the browser</param>
        /// <param name="filePath">The URI of the test page; either a URL or a file path</param>
        public FFTestSuite(IWebDriver webDriver, string filePath)
        {
            // Validate input arguments
            if (webDriver == null)
            {
                throw new ArgumentNullException("webDriver");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The test file '" + filePath + "'was not found.", filePath);
            }

            this.driver = webDriver;
            this.testFilePath = filePath;
            webDriver.Navigate().GoToUrl(new Uri(Path.GetFullPath(filePath)));
        }

        /// <summary>
        /// Gets Screenshot path
        /// </summary>
        private string ScreenshotPath
        { 
            get 
            { 
                return Path.ChangeExtension(this.testFilePath, "png"); 
            } 
        }

        /// <summary>
        /// Finds qunit tests container in the opened web page and creates a screenshot if it is missing
        /// </summary>
        public void Update()
        {
            var elements = this.driver.FindElements(By.Id("qunit-tests")).ToArray();

            if (elements != null && elements.Length == 0)
            {
                this.SaveScreenshot();
                var msg = string.Format(CultureInfo.CurrentCulture, "The test file is missing the list of qunit tests - element '#qunit-tests' was not found. See [{0}] for details.", this.ScreenshotPath);
            
                throw new InvalidFileException(msg);
            }

            this.qunitTestsElement = elements[0];
        }

        /// <summary>
        /// Returns a boolean values that indicates whether browser is still busy in executing qunit tests
        /// </summary>
        /// <returns>Whether or not the browser is buys in executing tests</returns>
        public bool IsRunning()
        {
            return this.qunitTestsElement.FindElements(By.CssSelector("li.running")).Any();
        }

        /// <summary>
        /// Returns an array of QUnit TestResult objects in the qunit tests container
        /// </summary>
        /// <returns>An array of Qunit TestResult objects</returns>
        public TestResult[] GetTestResults()
        {
            return this.qunitTestsElement
                    .FindElements(By.CssSelector("li[id^='test-output']"))
                    .Select(this.ParseTestResult)
                    .ToArray();
        }

        /// <summary>
        /// Saves a screenshot of the current FireFox session
        /// </summary>
        public void SaveScreenshot()
        {
            ((ITakesScreenshot)this.driver).GetScreenshot().SaveAsFile(this.ScreenshotPath, ScreenshotFormat);
        }

        /// <summary>
        /// Prepares a TestRusult object with the result of given qunit test web element
        /// </summary>
        /// <param name="testOutput">Container of a qunit test object that has final result</param>
        /// <returns>TestResult object with qunit result</returns>
        private TestResult ParseTestResult(IWebElement testOutput)
        {
            // Validate input arguments
            if (testOutput == null)
            {
                throw new ArgumentNullException("testOutput");
            }

            var testName = testOutput.FindElement(By.ClassName("test-name")).Text;
            var resultClass = testOutput.GetAttribute("class");

            if (resultClass == "pass")
            {
                return this.CreateTestResult(testName, true, string.Empty);
            }

            if (resultClass == "fail")
            {
                return this.CreateTestResult(testName, false, testOutput.FindElement(By.ClassName("fail")).Text);
            }

            if (resultClass == "running")
            {
                return this.CreateTestResult(testName, false, "The test did not finish within time limit.");
            }

            return this.CreateTestResult(testName, false, "Unknown test class: '" + resultClass + "'");
        }

        /// <summary>
        /// Creates a TestResult object
        /// </summary>
        /// <param name="testName">QUnit test name</param>
        /// <param name="passed">The result of the test ("pass" or "fail")</param>
        /// <param name="message">The reason for test failure</param>
        /// <returns>TestResult object</returns>
        private TestResult CreateTestResult(string testName, bool passed, string message)
        {
            return new TestResult
            {
                FileName = this.testFilePath,
                TestName = testName,
                Result = passed,
                Message = message
            };
        }
    }
}