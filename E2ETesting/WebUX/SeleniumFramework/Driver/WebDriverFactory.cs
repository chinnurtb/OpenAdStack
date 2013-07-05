//-----------------------------------------------------------------------
// <copyright file="WebDriverFactory.cs" company="Rare Crowds Inc">
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
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;
using OpenQA.Selenium.Remote;
using SeleniumFramework.Utilities;

namespace SeleniumFramework.Driver
{
    /// <summary>
    /// This class contains static props/methods to create/close objects of WebDriver for various browsers
    /// </summary>
    public static class WebDriverFactory
    {
        #region Variables

        /// <summary>
        /// Variable to open browser
        /// </summary>
        private static IWebDriver webDriver = null;

        #endregion

        #region Properties

        /// <summary>
        /// Gets WebDriver type object
        /// </summary>
        public static IWebDriver Driver 
        { 
            get
            {
                if (webDriver == null)
                {
                    CreateWebDriver(Constants.Get(Constants.Browser));
                }

                return webDriver;
            }
        }

        #endregion

        #region Methods

        #region Public Methods
        
        /// <summary>
        /// Close WebDriver object
        /// </summary>
        public static void CloseWebDriver()
        {
            if (webDriver != null)
            {
                webDriver.Close();
                webDriver.Dispose();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Create object of WebDriver for the input browser (FF/IE)
        /// </summary>
        /// <param name="browserType">Browser type</param>
        private static void CreateWebDriver(string browserType)
        {
            BrowserType browserTypeEnum = browserType.ToEnum<BrowserType>();

            if (browserTypeEnum == BrowserType.FirefoxBrowser)
            {
                webDriver = new FirefoxDriver(new FirefoxProfile("FirefoxProfile"));
            }
            else if (browserTypeEnum == BrowserType.InternetBrowser)
            {
                InternetExplorerOptions internetExplorerOptions = new InternetExplorerOptions();
                internetExplorerOptions.IntroduceInstabilityByIgnoringProtectedModeSettings = true;

                webDriver = new InternetExplorerDriver(internetExplorerOptions);
            }
            else if (browserTypeEnum == BrowserType.Chrome)
            {
                webDriver = new ChromeDriver();
            }
            else
            {
                throw new NotSupportedException("Browser Type not supported");
            }
        }

        #endregion

        #endregion
    }
}
