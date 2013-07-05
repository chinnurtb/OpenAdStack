//-----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Rare Crowds Inc">
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
using System.Collections;
using System.Data;
using System.Globalization;
using OpenQA.Selenium;
using SeleniumFramework.Persistence;

namespace SeleniumFramework.Utilities
{
    /// <summary>
    /// This class Contains methods to Load the constants from Excel and return the Constant values
    /// </summary>
    public static class Constants
    {
        #region Constant Variables

        #region General Constant Variables

        /// <summary>
        /// Constant for Browser
        /// </summary>
        public const string Browser = "BROWSER";

        /// <summary>
        /// Constant for Navigating to Create Company page
        /// </summary>
        public const string EntityCreationDivId = "ENTITY_CREATION_MESSAGE_DIV_ID";

        /// <summary>
        /// Constants for Excel Sheet used for User Login
        /// </summary>
        public const string LogOnSheetName = "LOGIN_SHEETNAME";

        /// <summary>
        /// Constants for using Enter key
        /// </summary>
        public const string Enter = "\n";

        /// <summary>
        /// Constant for Processing Image
        /// </summary>
        public const string ProcessingImageId = "PROCESSING_IMAGE_ID";

        #endregion

        #region Home Page Related Constant Variables

        /// <summary>
        /// Constant to verify the Home Page title
        /// </summary>
        public const string HomePageTitle = "HOME_PAGE_TITLE";

        /// <summary>
        /// Constant for navigating to Home Page
        /// </summary>
        public const string HomePageUrl = "HOME_PAGE_URL";

        #endregion

        #region Company Related Constant Variables

        /// <summary>
        /// Constant for Excel Sheet used to input data on Create Company Page
        /// </summary>
        public const string CompanyCreateSheetName = "COMPANY_CREATE_SHEETNAME";

        /// <summary>
        /// Constant for Navigating to Create Company page
        /// </summary>
        public const string CompanyCreateUrl = "COMPANY_CREATE_URL";

        /// <summary>
        /// Constant for entering the XPath for Company Creation pop up
        /// </summary>
        public const string CompanyCreationLink = "COMPANY_CREATE_LINK";

        /// <summary>
        /// Constant for entering the XPath for Advertiser Creation pop up
        /// </summary>
        public const string AdvertiserCreationLink = "ADVERTISER_CREATE_LINK";

        /// <summary>
        /// Constant for changing the focus from Company to Create Company popup
        /// </summary>
        public const string CompanyCreateFrameId = "COMPANY_POPUP_FRAME_ID";

        /// <summary>
        /// Constant to verify the Company Page title
        /// </summary>
        public const string CompanyPageTitle = "COMPANY_PAGE_TITLE";

        /// <summary>
        /// Constant for the Navigating to Company page
        /// </summary>
        public const string CompanyNavigationLink = "COMPANY_PAGE_NAVIGATION_LINK";

        /// <summary>
        /// Constant for the path of the Company grid
        /// </summary>
        public const string CompanyGridPath = "COMPANY_GRID_PATH";
        
        #endregion

        #region User Related Constant Variables

        /// <summary>
        /// Constant for Excel Sheet used to input data on Create Company Page
        /// </summary>
        public const string UserCreateSheetName = "USER_CREATE_SHEETNAME";

        /// <summary>
        /// Constant for Navigating to Create Company page
        /// </summary>
        public const string UserCreateUrl = "USER_CREATE_URL";

        /// <summary>
        /// Constant for the Navigating to Users page
        /// </summary>
        public const string UserNavigationLink = "USER_NAVIGATION_LINK";

        /// <summary>
        /// Constant for the User Page title
        /// </summary>
        public const string UserPageTitle = "USER_PAGE_TITLE";

        /// <summary>
        /// Constant for the User Invitation Page title
        /// </summary>
        public const string UserInvitationTitle = "USER_INVITATION_TITLE";

        /// <summary>
        /// Constant for the Create User Link
        /// </summary>
        public const string UserCreateLinkId = "USER_CREATE_LINK_ID";

        /// <summary>
        /// Constant for Excel Sheet used to input data on User Invitation Page
        /// </summary>
        public const string UserInvitationSheetName = "USER_INVITATION_SHEETNAME";

        /// <summary>
        /// Constant for the Invitation Error Message
        /// </summary>
        public const string UserInvitationMessage = "USER_INVITATION_VERIFICATION_TEXT";

        /// <summary>
        /// Constant for the Invitation Error Message
        /// </summary>
        public const string InvitationMessageId = "USER_INVITATION_MESSAGE_DIV_ID";

        /// <summary>
        /// Constant for Navigating to User Verification Form page
        /// </summary>
        public const string UserVerificationFormUrl = "USER_VERIFICATION_FORM_URL";

        /// <summary>
        /// Constant to verify the User Verification Form Page title
        /// </summary>
        public const string UserVerificationFormPageTitle = "USER_VERIFICATION_FORM_PAGE_TITLE";

        /// <summary>
        /// Constant for the Confirm button Id on User Verification Form Page
        /// </summary>
        public const string UserVerificationFormConfirmButtonId = "USER_VERIFICATION_FORM_CONFIRM_BUTTTON_ID";

        /// <summary>
        /// Constant for the Email field on User Verification Form Page
        /// </summary>
        public const string UserVerificationFormEmailFieldId = "USER_VERIFICATION_FORM_EMAIL_FIELD_ID";
        
        #endregion

        #region Campaign Related Constant Variables

        /// <summary>
        /// Constant for Excel Sheet used to input data on Create Campaign Page
        /// </summary>
        public const string CampaignCreateSheetName = "CAMPAIGN_CREATE_SHEETNAME";

        /// <summary>
        /// Constant for Navigating to Create Campaign page
        /// </summary>
        public const string CampaignCreateUrl = "CAMPAIGN_CREATE_URL";

        /// <summary>
        /// Constant for Campaign page navigation link Xpath
        /// </summary>
        public const string CampaignPageNavigationLink = "CAMPAIGN_PAGE_NAVIGATION_LINK";

        /// <summary>
        /// Constant for the Campaign Page title
        /// </summary>
        public const string CampaignPageTitle = "CAMPAIGN_PAGE_TITLE";

        /// <summary>
        /// Constant for the Campaign Page Create New link
        /// </summary>
        public const string CampaignPageCreateNew = "CAMPAIGN_PAGE_CREATE_NEW";

        /// <summary>
        /// Constant for changing the focus from Company to Create Company popup
        /// </summary>
        public const string CampaignCreateFrameId = "CAMPAIGN_CREATE_FRAME_ID";

        /// <summary>
        /// Constant for Navigating to Create Company page
        /// </summary>
        public const string CampaignCreationId = "CAMPAIGN_CREATION_MESSAGE_ID";

        /// <summary>
        /// Constant for Attributes table to drag from
        /// </summary>
        public const string AttributesTableToDragFrom = "ATTRIBUTES_TABLE_TO_DRAG_FROM";

        /// <summary>
        /// Constant for Parent Attribute to Expand
        /// </summary>
        public const string ParentAttributeIdImage = "PARENT_ATTRIBUTE_ID_IMAGE";

        /// <summary>
        /// Constant for Expanding the Child Attribute Elements
        /// </summary>
        public const string ChildAttributeIdImage = "CHILD_ATTRIBUTE_ID_IMAGE";

        /// <summary>
        /// Constant for Attribute Element Ids
        /// </summary>
        public const string ElementAttributeId = "ELEMENT_ATTRIBUTE_ID";
        
        /// <summary>
        /// Constant for Table to Drop Into
        /// </summary>
        public const string AttributeTableToDropInto = "ATTRIBUTES_TABLE_TO_DROP_INTO";
        
        /// <summary>
        /// Constant for Next Page
        /// </summary>
        public const string NextPage = "NEXT_PAGE";

        #endregion

        #endregion

        #region HashTable Collection

        /// <summary>
        /// HashTable collection to store constants name and values from excel
        /// </summary>
        private static Hashtable seleniumConstants = new Hashtable();

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets the specified Constant value from HashTable
        /// </summary>
        /// <param name="key">Constant name in the constants sheet</param>
        /// <returns>Returns string</returns>
        public static string Get(string key)
        {
            if (seleniumConstants.Count == 0)
            {
                InitializeConstants();
            }

            if (!seleniumConstants.ContainsKey(key))
            {
                throw new NotFoundException("Invalid Constant Name - " + key);    
            }

            return Convert.ToString(seleniumConstants[key], CultureInfo.InvariantCulture);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes Hashtable with all Constants specified in Excel file - Constants Sheet
        /// </summary>
        private static void InitializeConstants()
        {
            DataTable dataTable = ExcelHelper.GetDataTable(ConfigReader.DataFile, ConfigReader.ConstantsSheetName);

            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                throw new RowNotInTableException("Constants not loaded from Excel DataFile");
            }

            foreach (DataRow row in dataTable.Rows)
            {
                seleniumConstants.Add(row["Name"].ToString(), row["Value"].ToString());
            }
        }

        #endregion

        #endregion
    }
}
