//-----------------------------------------------------------------------
// <copyright file="WebDriverExtensionMethods.cs" company="Rare Crowds Inc">
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
using System.Collections.ObjectModel;
using OpenQA.Selenium;
using SeleniumFramework.Driver;

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// This class contains extension methods on IWebDriver type
    /// </summary>
    public static class WebDriverExtensionMethods
    {
        #region Methods

        /// <summary>
        /// Navigate the given Url
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">string URL</param>
        public static void NavigateUrl(this IWebDriver driver, string value)
        {
            Utility.ValidateUrl(value);
            driver.Navigate().GoToUrl(new Uri(value));
        }

        /// <summary>
        /// Find element by Id
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">WebElement Id</param>
        /// <returns>Returns IWebElement type</returns>
        public static IWebElement FindElementById(this ISearchContext driver, string value)
        {
            return driver.FindElement(By.Id(value));
        }

        /// <summary>
        /// Find element by Name
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">WebElement Name</param>
        /// <returns>Returns IWebElement type</returns>
        public static IWebElement FindElementByName(this ISearchContext driver, string value)
        {
            return driver.FindElement(By.Name(value));
        }

        /// <summary>
        /// Find element by XPath
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">The xpath query</param>
        /// <returns>>Returns IWebElement type</returns>
        public static IWebElement FindElementByXPath(this ISearchContext driver, string value)
        {
            return driver.FindElement(By.XPath(value));
        }

        /// <summary>
        /// Find element by Css
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">The CSS Selector to find</param>
        /// <returns>>Returns IWebElement type</returns>
        public static IWebElement FindElementByCssSelector(this ISearchContext driver, string value)
        {
            return driver.FindElement(By.CssSelector(value));
        }

        /// <summary>
        /// Find element by TagName
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">Tag Name</param>
        /// <returns>>Returns ReadOnlyCollection of IWebElement type</returns>
        public static ReadOnlyCollection<IWebElement> FindElementByTagName(this ISearchContext driver, string value)
        {
            return driver.FindElements(By.TagName(value));
        }

        /// <summary>
        /// Find Link element by Text
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="value">The Tag Name</param>
        /// <returns>>Returns IWebElement type</returns>
        public static IWebElement FindElementByLinkText(this ISearchContext driver, string value)
        {
            return driver.FindElement(By.LinkText(value));
        }

        /// <summary>
        /// Find the Grid corresponding to each entity
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <param name="pageContainerId">Main Container Id</param>
        /// <param name="gridContainerId">Grid Container Id</param>
        /// <returns>Returns IWebElement type</returns>
        public static IWebElement FindGridId(this ISearchContext driver, string pageContainerId, string gridContainerId)
        {
            ReadOnlyCollection<IWebElement> pageElements = driver.FindElements(By.Id(pageContainerId));
            IWebElement element = null;

            foreach (IWebElement webElement in pageElements)
            {
                string elementId = webElement.FindElementById(gridContainerId).GetAttribute("Id");

                if (elementId == gridContainerId)
                {
                    element = webElement.FindElementById(gridContainerId);
                }
            }

            return element;
        }

        /// <summary>
        /// Initializes IJavaScriptExecutor instance
        /// </summary>
        /// <param name="driver">Extension method on IWebdriver type</param>
        /// <returns>IJavaScriptExecutor Instance</returns>
        public static IJavaScriptExecutor Scripts(this IWebDriver driver)
        {
            return (IJavaScriptExecutor)driver;
        }

        #endregion
    }
}
