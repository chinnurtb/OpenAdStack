//-----------------------------------------------------------------------
// <copyright file="FFTestRunner.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
// <remarks>
//      You may find some additional information about this at https://github.com/Sqdw/SQUnit.
// </remarks>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;

namespace TestUtilities
{
    /// <summary>
    /// The class that takes care of firing a FireFox session using Selenium and parsing the DOM of the page to extract the QUnit information.
    /// </summary>
    public class FFTestRunner : IDisposable
    {
        /// <summary>
        /// Polling duration that will be used to check the completion of tests execution
        /// </summary>
        private const int PollingIntervalInMs = 100;

        /// <summary>
        /// Instance of the interface through which the we can control the browser
        /// </summary>
        private IWebDriver driver;

        /// <summary>
        /// Instance of the helper class that takes care of firing a FireFox session using Selenium and parsing the DOM of the page to extract the QUnit information
        /// </summary>
        private FFTestSuite testSuite;
        
        /// <summary>
        /// Initializes a new instance of the FFTestRunner class with FireFox as the default driver
        /// </summary>
        public FFTestRunner() : this(CreateFirefoxDriver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the FFTestRunner class
        /// </summary>
        /// <param name="webDriver">An OpenQA.Selenium.IWebDriver interface instance</param>
        public FFTestRunner(IWebDriver webDriver)
        {
            this.MaxWaitInMS = 10000;
            this.driver = webDriver;
        }

        /// <summary>
        /// Finalizes an instance of the FFTestRunner class
        /// </summary>
        ~FFTestRunner()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets an array of TestResult objects that encapsulate the QUnit tests within the passed in files to test.
        /// </summary>
        public static IEnumerable<TestResult> FFResults
        {
            get
            {
                // var testsDirectory = System.Environment.GetEnvironmentVariable("JavascriptTestDir");
                var testsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "JavaScriptTests");

                var results = new List<TestResult>();

                using (FFTestRunner runner = new FFTestRunner())
                {
                    foreach (string file in Directory.GetFiles(testsDirectory, "*.html"))
                    {
                        results.AddRange(runner.RunTestsInFile(file));
                    }
                }

                return results;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of milliseconds before the tests should timeout after page load
        /// </summary>
        public int MaxWaitInMS { get; set; }

        /// <summary>
        /// Returns an array of TestResult objects given a test page URI
        /// </summary>
        /// <param name="filePath">The URI of the test page; either a URL or a file path</param>
        /// <returns>An array of TestResult objects</returns>
        public TestResult[] RunTestsInFile(string filePath)
        {
            var tests = default(TestResult[]);
            var exception = default(Exception);

            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentNullException("filePath");
                }

                this.testSuite = new FFTestSuite(this.driver, filePath);
                this.WaitForTestsToFinish();
                this.testSuite.SaveScreenshot();
                tests = this.testSuite.GetTestResults();
            }
            catch (ArgumentNullException ex)
            {
                exception = ex;
            }
            catch (NullReferenceException ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                return new[] { new TestResult { InitializationException = exception } };
            }

            return tests;
        }

        /// <summary>
        ///  Performs application-defined tasks associated with freeing, releasing, or
        ///  resetting unmanaged resources. Releases the driver for garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Close the current window, quitting the browser if it is the last window currently open.
        /// </summary>
        /// <param name="disposing">TO DO</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.driver == null)
            {
                return;
            }

            this.driver.Close();
            this.driver.Dispose();
            this.driver = null;
        }

        /// <summary>
        /// Initializes a new instance of the OpenQA.Selenium.Firefox.FirefoxDriver class
        /// </summary>
        /// <returns>A new instance of the OpenQA.Selenium.Firefox.FirefoxDriver</returns>
        private static IWebDriver CreateFirefoxDriver()
        {
            return new FirefoxDriver();
        }

        /// <summary>
        ///  This function is called to check for completion of the qunit tests. Helps us in Waiting until all the tests are executed.
        /// </summary>
        private void WaitForTestsToFinish()
        {
            var remainingTimeInMs = this.MaxWaitInMS;

            while (remainingTimeInMs > 0)
            {
                this.testSuite.Update();

                if (!this.testSuite.IsRunning())
                {
                    break;
                }

                Thread.Sleep(PollingIntervalInMs);
                remainingTimeInMs -= PollingIntervalInMs;
            }
        }
    }
}