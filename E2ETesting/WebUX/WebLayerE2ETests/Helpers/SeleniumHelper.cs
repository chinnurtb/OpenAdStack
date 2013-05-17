//-----------------------------------------------------------------------
// <copyright file="SeleniumHelper.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SeleniumFramework.Helpers;
using SeleniumFramework.Persistence;
using SeleniumFramework.Utilities;

namespace WebLayerTest.Helpers
{
    /// <summary>
    /// This class contains methods for common activities (Login, Input data to forms)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SeleniumHelper
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether the login is successful or not
        /// </summary>
        public static bool IsLogOnSuccessful { get; set; }

        /// <summary>
        /// Gets or sets the current logged in username
        /// </summary>
        public static string LoggedInUsername { get; set; }

        /// <summary>
        /// Gets or sets the current logged in password
        /// </summary>
        public static string LoggedInPassword { get; set; }

        /// <summary>
        /// Gets or sets the Clients
        /// </summary>
        public static string SelectedClient { get; set; }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Login to Lucy system
        /// </summary>
        /// <returns>Error Message if any</returns>
        public static string LogOnActivity()
        {
            string errorMessage = string.Empty;

            string logOnSheetName = Constants.Get(Constants.LogOnSheetName);
            DataTable logonTestData = ExcelHelper.GetDataTable(ConfigReader.DataFile, logOnSheetName);

            IsLogOnSuccessful = false;

            foreach (DataRow row in logonTestData.Rows)
            {
                // Store UserName
                LoggedInUsername = row["Username"].String();

                // Store UserName
                LoggedInPassword = row["Password"].String();

                // Login User
                bool status = SeleniumHelper.ProcessInputData(logOnSheetName, row, out errorMessage);

                if (status)
                {
                    int counter = 1;

                    while (!IsLogOnSuccessful && counter <= ConfigReader.RetryCount)
                    {
                        // Login Successful
                        IsLogOnSuccessful = SeleniumHelper.VerifyLogOn();

                        if (!IsLogOnSuccessful)
                        {
                            Thread.Sleep(5000);
                        }

                        counter++;
                    }
                }
            }

            return errorMessage;
        }

        /// <summary>
        /// Fill values from TestDataSheet into all input form fields in Mapping Sheet 
        /// </summary>
        /// <param name="mappingDatasheet">Mapping Sheet to iterate all form fields</param>
        /// <param name="inputDataRow">DataRow to read data from</param>
        /// <param name="errorMessage">Error message assigned if any</param>
        /// <returns>Bool value (success or failure)</returns>
        public static bool ProcessInputData(string mappingDatasheet, DataRow inputDataRow, out string errorMessage)
        {
            // Local Variables
            string controlId = string.Empty;
            ElementBy findElementByType = ElementBy.None;
            InputControlType inputControlType = InputControlType.None;
            string inputDataColumnName = string.Empty;
            DataRow curRow = null;
            string inputData = string.Empty;

            // Assign Error message to empty string 
            errorMessage = string.Empty;

            // Parse the UI Control Mapping File
            DataTable mappingDataTable = ExcelHelper.GetDataTable(ConfigReader.UIControlsMappingFile, mappingDatasheet);

            // Input Data to all controls
            if (mappingDataTable != null)
            {
                for (int i = 0; i < mappingDataTable.Rows.Count; i++)
                {
                    curRow = mappingDataTable.Rows[i];

                    controlId = curRow["Control_Id"].String();
                    findElementByType = curRow["Find_Element_By_Type"].String().ToEnum<ElementBy>();
                    inputControlType = curRow["Input_Control_Type"].String().ToEnum<InputControlType>();
                    inputDataColumnName = curRow["Input_Data_Column_Name"].String();

                    try
                    {
                        // Read input data from specified column, Data Column will be blank for button
                        if (!string.IsNullOrEmpty(inputDataColumnName) && inputControlType != InputControlType.Button && inputControlType != InputControlType.Image)
                        {
                            inputData = inputDataRow[inputDataColumnName].String();
                        }

                        if (string.IsNullOrEmpty(inputData))
                        {
                            continue;
                        }

                        // Check the input control types and do the required operation
                        switch (inputControlType)
                        {
                            case InputControlType.TextBox:
                                UIHelper.InsertData(controlId, inputData, findElementByType);
                                break;
                            case InputControlType.DropDownList:
                                UIHelper.SelectOption(controlId, inputData);
                                break;
                            case InputControlType.CheckBox:
                                if (!string.IsNullOrEmpty(inputDataColumnName) && (inputData.Equals("TRUE") || inputData.Equals("1")))
                                {
                                    UIHelper.ElementClick(controlId, findElementByType);
                                }

                                break;
                            case InputControlType.Button:
                                // Defect - Click() not working in IE
                                if (Constants.Get(Constants.Browser).Equals(BrowserType.InternetBrowser.String()))
                                {
                                    UIHelper.InsertData(controlId, Constants.Enter, findElementByType, InputControlType.Button);
                                }
                                else
                                {
                                    UIHelper.ElementClick(controlId, findElementByType);
                                }
                                
                                break;
                            case InputControlType.Image:
                                UIHelper.ElementClick(controlId, findElementByType);
                                break;
                            default:
                                break;
                        }
                    }
                    catch (NoSuchElementException exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }
                    catch (StaleElementReferenceException exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }
                    catch (NullReferenceException exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }
                    catch (ElementNotVisibleException exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }
                    catch (ArgumentException exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Test method to verify the successful login on Company page
        /// </summary>
        /// <returns>Return boolean</returns>
        public static bool VerifyLogOn()
        {
            try
            {
                // Wait for elements to get displayed in case of IE
                ////if (Constants.Get(Constants.Browser).Equals(BrowserType.InternetBrowser.String()))
                ////{
                ////    Utility.Wait();
                ////}

                // TODO - Data Verification to be changed as we progress
                if (!UIHelper.VerifyPageTitle(Constants.HomePageTitle))
                {
                    IsLogOnSuccessful = false;
                }
                else
                {
                    IsLogOnSuccessful = true;
                }
                
                return IsLogOnSuccessful;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
            catch (AssertFailedException)
            {
                return false;
            }
        }

        #endregion

        #endregion
    }
}
