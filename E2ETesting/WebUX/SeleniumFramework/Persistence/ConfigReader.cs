// -----------------------------------------------------------------------
// <copyright file="ConfigReader.cs"  company="Rare Crowds Inc">
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
using System.Configuration;
using System.Globalization;

namespace SeleniumFramework.Persistence
{
    /// <summary>
    /// This class contains the props for each key in app.config and connection string
    /// </summary>
    public static class ConfigReader
    {
        #region app.config keys

        /// <summary>
        /// Gets the Website Url to be used for Automation Tests
        /// </summary>
        public static string Website
        {
            get
            {
                return ConfigurationManager.AppSettings["WebsiteUrl"];
            }
        }

        /// <summary>
        /// Gets the Input Data File Name for Test Execution
        /// </summary>
        public static string DataFile
        {
            get
            {
                return ConfigurationManager.AppSettings["DataFile"];
            }
        }

        /// <summary>
        /// Gets the Page UI Controls Mapping File name for test execution
        /// </summary>
        public static string UIControlsMappingFile
        {
            get
            {
                return ConfigurationManager.AppSettings["UIControlsMappingFile"];
            }
        }

        /// <summary>
        /// Gets the Constant Sheet Name
        /// </summary>
        public static string ConstantsSheetName
        {
            get
            {
                return ConfigurationManager.AppSettings["ConstantsSheetName"];
            }
        }

        /// <summary>
        ///  Gets the Wait time
        /// </summary>
        public static int WaitTime
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["WaitTime"], CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the implicit wait time
        /// </summary>
        public static int ImplicitWaitTime
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["ImplicitWaitTime"], CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets the Retry Count to check the response
        /// </summary>
        public static int RetryCount 
        {
            get
            {
                return Convert.ToInt32(ConfigurationManager.AppSettings["RetryCount"], CultureInfo.InvariantCulture);
            }
        }

        #endregion
    }
}
