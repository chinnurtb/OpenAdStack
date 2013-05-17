// -----------------------------------------------------------------------
// <copyright file="ConfigReader.cs"  company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
