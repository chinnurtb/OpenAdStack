//-----------------------------------------------------------------------
// <copyright file="Utility.cs" company="Rare Crowds Inc">
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
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Threading;
using SeleniumFramework.Driver;
using SeleniumFramework.Persistence;

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// This class contains genric static props and methods to be used across Selenium Tests
    /// </summary>
    public static class Utility
    {
        #region Properties

        /// <summary>
        /// Gets the machine name
        /// </summary>
        public static string MachineName
        {
            get
            {
                return System.Environment.MachineName;
            }
        }

        /// <summary>
        /// Gets the Date in mm_dd_yyyy format
        /// </summary>
        public static string Date
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:MM_dd_yyyy}", System.DateTime.Now);
            }
        }

        /// <summary>
        /// Gets the DateTime in mmddyyyy_hhmmss format
        /// </summary>
        public static string DateTime
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:MMddyyyy_HHmmss}", System.DateTime.Now);
            }
        }

        #endregion

        #region Methods
        
        /// <summary>
        /// Validate the url (null, empty or white spaces)
        /// </summary>
        /// <param name="value">string URL</param>
        public static void ValidateUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException("URL can't be blank");
            }
        }

        /// <summary>
        /// Waits till specified milliseconds
        /// </summary>
        public static void Wait()
        {
            Thread.Sleep(ConfigReader.WaitTime);
        }

        /// <summary>
        /// Implicit Waits - tell Webdriver to poll the DOM for a certain amount of time when trying to find an element
        /// </summary>
        public static void ImplicitWait()
        {
            // Set the Implicit WaitTime to tell Webdriver to poll the DOM for a certain amount of time when trying to find an element  
            WebDriverFactory.Driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(ConfigReader.ImplicitWaitTime));
        }

        /// <summary>
        /// Get the Enum Description value
        /// </summary>
        /// <param name="value">Enum value</param>
        /// <returns>Description of enum</returns>
        public static string GetEnumDescription(Enum value)
        {
            // Get the Description attribute value for the enum value
            FieldInfo fi = value.GetType().GetField(value.String());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return value.String();
            }
        }

        #endregion
    }
}
