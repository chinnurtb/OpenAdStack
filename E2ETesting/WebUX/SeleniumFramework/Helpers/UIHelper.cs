//-----------------------------------------------------------------------
// <copyright file="UIHelper.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using OpenQA.Selenium;
using SeleniumFramework.Driver;
using SeleniumFramework.Persistence;
using SeleniumFramework.Utilities;

namespace SeleniumFramework.Helpers
{
    /// <summary>
    /// This class contains all the methods to find elements in the page and fill the data
    /// </summary>
    public static class UIHelper
    {
        #region Methods

        /// <summary>
        /// Select the option from list of options in specified dropdown
        /// </summary>
        /// <param name="selectElement">Dropdown list</param>
        /// <param name="options">Options list</param>
        /// <param name="value">Option to be selected</param>
        public static void SelectOption(ISearchContext selectElement, IList<IWebElement> options, string value)
        {
            string keyMove = Keys.Up;
            int optionsCount = options.Count;

            for (int y = 1; y < optionsCount; y++)
            {
                keyMove = keyMove + "+" + Keys.Up;
            }

            int p = 1;

            foreach (IWebElement option in options)
            {
                if (option.Text == value)
                {
                    selectElement.FindElementByXPath("option[" + p + "]").SendKeys(keyMove + Keys.Enter);
                    break;
                }
                else
                {
                    keyMove = keyMove + "+" + Keys.Down;
                }

                p++;
            }
        }

        /// <summary>
        /// Select the option from list of options in specified dropdown list etc
        /// </summary>
        /// <param name="listElementId">Dropdown element Id</param>
        /// <param name="value">Option to select</param>
        public static void SelectOption(string listElementId, string value)
        {
            IWebElement elementId = WebDriverFactory.Driver.FindElementById(listElementId);
            UIHelper.SelectOption(elementId, value, "option");
        }

        /// <summary>
        /// Select the option from list of options in specified dropdown
        /// </summary>
        /// <param name="selectElement">Dropdown list</param>
        /// <param name="value">Option to be selected</param>
        public static void SelectOption(ISearchContext selectElement, string value)
        {
            IList<IWebElement> options = selectElement.FindElementByTagName("option");

            foreach (IWebElement option in options)
            {
                if (option.Text == value)
                {
                    option.Click();
                    break;
                }
            }
        }

        /// <summary>
        /// Inserts the data into the specific element
        /// </summary>
        /// <param name="elementId">Id of the element</param>
        /// <param name="data">Data to be inserted</param>
        /// <param name="findElementByType">Element Type</param>
        public static void InsertData(string elementId, string data, ElementBy findElementByType)
        {
            InsertData(elementId, data, findElementByType, InputControlType.TextBox);
         }

        /// <summary>
        /// Inserts the data into the specific element
        /// </summary>
        /// <param name="elementId">Element Id</param>
        /// <param name="data">Data to be inserted</param>
        /// <param name="findElementByType">Element Type</param>
        /// <param name="inputControlType">Form Control Type</param>
        public static void InsertData(string elementId, string data, ElementBy findElementByType, InputControlType inputControlType)
        {
            IWebElement webElementId = null;

            switch (findElementByType)
            {
                case ElementBy.Id:
                    webElementId = WebDriverFactory.Driver.FindElementById(elementId);
                    break;
                case ElementBy.XPath:
                    webElementId = WebDriverFactory.Driver.FindElementByXPath(elementId);
                    break;
                default:
                    break;
            }

            if (webElementId != null)
            {
                if (inputControlType == InputControlType.TextBox)
                {
                    webElementId.Clear();
                }

                webElementId.SendKeys(data);
            }
        }

        /// <summary>
        /// Click the specific element
        /// </summary>
        /// <param name="elementId">Element to click</param>
        /// <param name="findElementByType">Element Type</param>
        public static void ElementClick(string elementId, ElementBy findElementByType)
        {
            IWebElement webElementId = null;

            switch (findElementByType)
            {
                case ElementBy.Id:
                    webElementId = WebDriverFactory.Driver.FindElementById(elementId);
                    break;
                case ElementBy.XPath:
                    webElementId = WebDriverFactory.Driver.FindElementByXPath(elementId);
                    break;
                default:
                    break;
            }

            if (webElementId != null)
            {
                webElementId.Click();
            }
        }

        /// <summary>
        /// Verify the the option from list of options in specified dropdown list etc
        /// </summary>
        /// <param name="elementId">Id of the element</param>
        /// <param name="data">Data to be verified</param>
        /// <param name="tagName">Tag name</param>
        /// <returns>Boolean value</returns>
        public static bool VerifyElements(string elementId, string data, string tagName)
        {
            bool flag = false;

            IWebElement selectElement = WebDriverFactory.Driver.FindElementById(elementId);
            IList<IWebElement> options = selectElement.FindElementByTagName(tagName);

            foreach (IWebElement option in options)
            {
                if (option.Text == data && option.Selected)
                {
                    flag = true;
                    break;
                }
                else
                {
                    flag = false;
                }
            }

            return flag;
        }

        /// <summary>
        /// Gets the entity Id corresponding to an entity eg. campaign
        /// </summary>
        /// <param name="elementId">Element Id to search</param>
        /// <param name="entityName">Entity Name</param>
        /// <param name="tagName">Tag name</param>
        /// <returns>Entity Id</returns>
        public static int GetEntityId(string elementId, string entityName, string tagName)
        {
            int entityId = 0;

            IWebElement element = WebDriverFactory.Driver.FindElementById(elementId);
            IList<IWebElement> options = element.FindElementByTagName(tagName);

            foreach (IWebElement option in options)
            {
                if (option.Text == entityName)
                {
                    entityId = Convert.ToInt32(option.GetAttribute("value"), CultureInfo.InvariantCulture);
                    break;
                }
            }

            return entityId;
        }

        /// <summary>
        /// Verify the current Page Title
        /// </summary>
        /// <param name="pageTitle">Page Title</param>
        /// <returns>Boolean Value</returns>
        public static bool VerifyPageTitle(string pageTitle)
        {
            bool flag = false;
            
            // Validate the Page Title
            if (WebDriverFactory.Driver.Title.Equals(Constants.Get(pageTitle), StringComparison.CurrentCultureIgnoreCase))
            {
                flag = true;
            }

            return flag;
        }

        /// <summary>
        /// Compare two message text
        /// </summary>
        /// <param name="expectedText">Expected Text</param>
        /// <param name="actualText">Actual Text</param>
        /// <returns>Boolean Value</returns>
        public static bool CompareMessageText(string expectedText, string actualText)
        {
            bool flag = false;
            
            if (actualText.Equals(expectedText, StringComparison.CurrentCultureIgnoreCase))
            {
                flag = true;
            }

            return flag;
        }

        /// <summary>
        /// Wait for Processing of different elements - Text, Image etc.
        /// </summary>
        /// <param name="elementId">Element Id</param>
        /// <param name="elementType">Element Type</param>
        /// <returns>Boolean Value</returns>
        public static bool WaitForProcessing(string elementId, InputControlType elementType)
        {
            bool isElementFound = false;
            bool flag = true;
            int counter = 1;

            while (flag)
            {
                switch (elementType)
                {
                    case InputControlType.Image:
                        isElementFound = WebDriverFactory.Driver.FindElementById(elementId).Displayed;
                        break;
                    case InputControlType.Div:
                        string value = WebDriverFactory.Driver.FindElementById(elementId).Text;

                        if (!string.IsNullOrEmpty(value))
                        {
                            isElementFound = true;                            
                        }

                        break;
                    default:
                    break;
                }

                if (!isElementFound && counter <= ConfigReader.RetryCount)
                {
                    flag = false;
                }
                else
                {
                    Utility.Wait();
                    counter++;
                }
            }

            return isElementFound;
        }
        
        /// <summary>
        /// Select the option from list of options in specified dropdown list etc
        /// </summary>
        /// <param name="selectElement">Dropdown/Checkbox list</param>
        /// <param name="value">Option to select</param>
        /// <param name="tagName">Tag name</param>
        private static void SelectOption(ISearchContext selectElement, string value, string tagName)
        {
            IList<IWebElement> options = selectElement.FindElementByTagName(tagName);

            foreach (IWebElement option in options)
            {
                if (option.Text == value)
                {
                    option.Click();
                    break;
                }
            }
        }

        #endregion
    }
}
