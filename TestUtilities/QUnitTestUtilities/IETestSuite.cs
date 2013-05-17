//-----------------------------------------------------------------------
// <copyright file="IETestSuite.cs" company="Emerging Media Group">
//      Copyright Emerging Media Group. All rights reserved.
// </copyright>
// <remarks>
//      You may find some additional information about this at https://github.com/robdmoore/NQUnit.
// </remarks>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace TestUtilities
{
    /// <summary>
    /// The class that takes care of firing an IE session using WatiN and parsing the DOM of the page to extract the QUnit information.
    /// </summary>
    public class IETestSuite : IDisposable
    {
        /// <summary>
        /// The maximum number of milliseconds before the tests should timeout after page load; 
        /// -1 for infinity, 0 to not support asynchronous tests
        /// </summary>
        private readonly int maxWaitInMs;

        /// <summary>
        /// Instance of Internet Explorer to access a webpage
        /// </summary>
        private readonly IE ie;

        /// <summary>
        /// Initializes a new instance of the IETestSuite class
        /// </summary>
        /// <param name="maxWaitInMilliseconds">The maximum number of milliseconds before the tests should timeout after page load; 
        /// -1 for infinity, 0 to not support asynchronous tests</param>
        public IETestSuite(int maxWaitInMilliseconds)
        {
            this.maxWaitInMs = maxWaitInMilliseconds < 0 ? int.MaxValue : maxWaitInMilliseconds;
            this.ie = new IE();

            if (IETestRunner.HideBrowserWindow)
            {
                this.ie.ShowWindow(NativeMethods.WindowShowStyle.Hide);
            }

            if (IETestRunner.ClearCacheBeforeRunningTests)
            {
                this.ie.ClearCache();
            }
        }

        /// <summary>
        /// Finalizes an instance of the IETestSuite class
        /// </summary>
        ~IETestSuite()
        {
            Dispose(false);
        }

        /// <summary>
        /// Returns an array of QUnitTest objects given a test page URI
        /// </summary>
        /// <param name="testPage">The URI of the test page; either a URL or a file path</param>
        /// <returns>An array of QUnitTest objects</returns>
        public IEnumerable<TestResult> GetQUnitTestResults(string testPage)
        {
            if (string.IsNullOrWhiteSpace(testPage))
            {
                throw new ArgumentNullException("testPage");
            }

            this.ie.GoTo(new Uri(testPage));
            this.ie.WaitForComplete(5);
            return this.GrabTestResultsFromWebPage(testPage);
        }

        /// <summary>
        ///  Performs application-defined tasks associated with freeing, releasing, or
        ///  resetting unmanaged resources. Closes the IE instance.
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
            this.ie.Close();
        }

        /// <summary>
        /// Removes assert counts that are displayed along with qunit test name
        /// </summary>
        /// <param name="fullTagText">Full qunit test name</param>
        /// <returns>QUnit test name after removing asserts count</returns>
        private static string RemoveAssertCounts(string fullTagText)
        {
            if (fullTagText == null)
            {
                return string.Empty;
            }

            int parenPosition = fullTagText.IndexOf('(');

            if (parenPosition > 0)
            {
                return fullTagText.Substring(0, parenPosition);
            }

            return fullTagText;
        }

        /// <summary>
        /// Parses given html and makes it xhtml complaint code if there is anything missing
        /// </summary>
        /// <param name="html">Html text that need to be parsed</param>
        /// <returns>XHTML complaint html text</returns>
        private static string MakeXHtml(string html)
        {
            var replacer = new Regex(@"<([^ >]+)(.*?)>", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var innerReplacer = new Regex(@"(\s+.+?=)([^\s$]+)", RegexOptions.IgnoreCase);
            var h = replacer.Replace(html, match => "<" + match.Groups[1] + innerReplacer.Replace(match.Groups[2].Value, innerMatch => innerMatch.Groups[2].Value.Contains("\"") ? innerMatch.Groups[1].Value + innerMatch.Groups[2].Value : innerMatch.Groups[1].Value + "\"" + innerMatch.Groups[2].Value + "\"") + ">");

            return h;
        }

        /// <summary>
        /// Returns an array of QUnitTest objects given a test page URI.
        /// </summary>
        /// <param name="testPage">The URI of the test page; either a URL or a file path</param>
        /// <returns>An array of QUnitTest objects</returns>
        private IEnumerable<TestResult> GrabTestResultsFromWebPage(string testPage)
        {
            var stillRunning = true;
            var testOl = default(Element);
            var documentRoot = default(XElement);
            var wait = 0;

            // BEWARE: This logic is tightly coupled to the structure of the HTML generated by the QUnit test runner
            while (stillRunning && wait <= this.maxWaitInMs)
            {
                testOl = this.ie.Elements.Filter(Find.ById("qunit-tests"))[0];
                
                if (testOl == null) 
                { 
                    yield break; 
                }

                // Load html structure of "qunit-tests" element
                documentRoot = XDocument.Load(new StringReader(MakeXHtml(testOl.OuterHtml))).Root;
                if (documentRoot == null)
                {
                    yield break;
                }

                // Check whether any tests are still in progress. If tests are still executing wait until they are complete before fetching results
                stillRunning = documentRoot.Elements().Any(e => e.Attributes().First(x => x.Name.Is("class")).Value == "running");

                if (stillRunning && wait < this.maxWaitInMs)
                {
                    Thread.Sleep(wait + 100 > this.maxWaitInMs ? this.maxWaitInMs - wait : 100);
                }

                wait += 100;
            }

            // Now loop over all the elements and prepare a QUnitTest object for each QUnit test
            foreach (var listItem in documentRoot.Elements())
            {
                var testName = listItem.Elements().First(x => x.Name.Is("strong")).Value;
                var resultClass = listItem.Attributes().First(x => x.Name.Is("class")).Value;
                var failedAssert = string.Empty;
                if (resultClass == "fail")
                {
                    var specificAssertFailureListItem = listItem.Elements()
                        .First(x => x.Name.Is("ol")).Elements()
                        .First(x => x.Name.Is("li") && x.Attributes().First(a => a.Name.Is("class")).Value == "fail");
                    if (specificAssertFailureListItem != null)
                    {
                        failedAssert = specificAssertFailureListItem.Value;
                    }
                }

                yield return new TestResult
                {
                    FileName = testPage,
                    TestName = RemoveAssertCounts(testName),
                    Result = resultClass.Equals("pass"),
                    Message = failedAssert
                };
            }
        }
    }
}
