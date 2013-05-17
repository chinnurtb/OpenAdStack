//-----------------------------------------------------------------------
// <copyright file="IETestRunner.cs" company="Emerging Media Group">
//      Copyright Emerging Media Group. All rights reserved.
// </copyright>
// <remarks>
//      You may find some additional information about this class at https://github.com/robdmoore/NQUnit.
// </remarks>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace TestUtilities
{
    /// <summary>
    /// Entry class for parsing and returning QUnit tests.
    /// </summary>
    public static class IETestRunner
    {
        /// <summary>
        /// Gets or sets a value indicating whether the browser cache need to be cleared before running tests to ensure you always run against the latest version of a file.
        /// </summary>
        public static bool ClearCacheBeforeRunningTests { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether browser window need to hidden while running tests to stop it from stealing focus.
        /// </summary>
        public static bool HideBrowserWindow { get; set; }

        /// <summary>
        /// Gets an array of TestResult objects that encapsulate the QUnit tests within the passed in files to test.
        /// </summary>
        public static IEnumerable<TestResult> IEResults
        {
            get
            {
                // var testsDirectory = System.Environment.GetEnvironmentVariable("JavascriptTestDir");
                var testsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "JavaScriptTests");

                return IETestRunner.GetTests(Directory.GetFiles(testsDirectory, "*.html"));
            }
        }

        /// <summary>
        /// Returns an array of QUnitTest objects that encapsulate the QUnit tests within the passed in files to test.
        /// Will wait for infinity for any asynchronous tests to run.
        /// </summary>
        /// <param name="filesToTest">A list of one or more files to run tests on relative to the root of the test project.</param>
        /// <returns>An array of QUnitTest objects encapsulating the QUnit tests in the given files</returns>
        public static IEnumerable<TestResult> GetTests(params string[] filesToTest)
        {
            return GetTests(-1, filesToTest);
        }

        /// <summary>
        /// Returns an array of QUnitTest objects that encapsulate the QUnit tests within the passed in files to test.
        /// </summary>
        /// <param name="maxWaitInMilliseconds">The maximum number of milliseconds before the tests should timeout after page load; -1 for infinity, 0 to not support asynchronous tests</param>
        /// <param name="filesToTest">A list of one or more files to run tests on relative to the root of the test project.</param>
        /// <returns>An array of QUnitTest objects encapsulating the QUnit tests in the given files</returns>
        public static IEnumerable<TestResult> GetTests(int maxWaitInMilliseconds, params string[] filesToTest)
        {
            if (filesToTest == null)
            {
                throw new ArgumentNullException("filesToTest");
            }

            var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            var tests = default(IEnumerable<TestResult>);
            var exception = default(Exception);

            // WatiN requires STA to run so rather than making the whole assembly
            //  run with STA, which causes trouble when running with TeamCity we create
            //  an STA thread in which to run the WatiN tests.
            var t = new Thread(() =>
            {
                var qUnitParser = default(IETestSuite);
                try
                {
                    qUnitParser = new IETestSuite(maxWaitInMilliseconds);
                    tests = filesToTest.SelectMany(qUnitParser.GetQUnitTestResults).ToArray();
                }
                catch (ArgumentNullException ex)
                {
                    exception = ex;
                }
                catch (NullReferenceException ex)
                {
                    exception = ex;
                }
                finally
                {
                    if (qUnitParser != null)
                    {
                        qUnitParser.Dispose();
                    }

                    waitHandle.Set();
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            waitHandle.WaitOne();

            if (exception != null)
            {
                return new[] { new TestResult { InitializationException = exception } };
            }

            return tests;
        }
    }
}