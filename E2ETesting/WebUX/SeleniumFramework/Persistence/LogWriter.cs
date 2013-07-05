//-----------------------------------------------------------------------
// <copyright file="LogWriter.cs" company="Rare Crowds Inc">
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
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using SeleniumFramework.Utilities;

namespace SeleniumFramework.Persistence
{
    /// <summary>
    /// This class contains the methods to create the log report during Selenium Test run
    /// </summary>
    public static class LogWriter
    {
        #region Variables

        /// <summary>
        /// Varible to create xml document
        /// </summary>
        private static XDocument xmlDoc;

        /// <summary>
        ///  Variable to store the xml file path
        /// </summary>
        private static string xmlFilePath = string.Empty;

        /// <summary>
        ///  Variable to store the partial path of created xml file to be used in mail
        /// </summary>
        private static string xmlFilePathForEmail = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets Excel data file partial path to be used in mail
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                if (xmlDoc == null)
                {
                    InitializeDocument();    
                }

                return xmlFilePathForEmail;
            }
        }

        #endregion 

        #region Methods

        #region Public Methods

        /// <summary>
        /// Writes the Test Case Results in Xml document
        /// </summary>
        /// <param name="testCaseId">Test case Id</param>
        /// <param name="mode">Test case Mode</param>
        /// <param name="scriptName">Script name</param>
        /// <param name="summary">Test case summary</param>
        /// <param name="status">Test case status</param>
        /// <param name="message">Test case output message</param>
        public static void WriteLog(string testCaseId, Mode mode, string scriptName, string summary, string status, string message)
        {
            if (xmlDoc == null)
            {
                InitializeDocument();
            }

            XElement element = new XElement(
                                    "result",
                                    new XElement("testcaseid", testCaseId),
                                    new XElement("scriptname", scriptName),
                                    new XElement("mode", mode.String()),
                                    new XElement("date", string.Format(CultureInfo.InvariantCulture, "{0:MM/dd/yyyy}", DateTime.Now)),
                                    new XElement("summary", summary),
                                    new XElement("status", status),
                                    new XElement("message", message));

            xmlDoc.Root.Add(element);
            xmlDoc.Save(xmlFilePath);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize XmlDocument type object
        /// </summary>
        private static void InitializeDocument()
        {
            string logPath = ConfigReader.LogPath;

            // Create log path folder if not created
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            // Create Project Name folder if not created
            string logReportPath = Path.Combine(logPath, ConfigReader.ProjectName);
            if (!Directory.Exists(logReportPath))
            {
                Directory.CreateDirectory(logReportPath);
            }

            // Combine the Log path and date to create Folder
            string newLogPathForEmail = ConfigReader.ProjectName + "/" + Utility.Date;
            string newLogPath = Path.Combine(logReportPath, Utility.Date);
            if (!Directory.Exists(newLogPath))
            {
                Directory.CreateDirectory(newLogPath);
            }

            // Create xml file for logging test results
            string xmlLogFile = string.Format(CultureInfo.InvariantCulture, "TestReport_{0}.xml", Utility.DateTime);
            xmlFilePath = Path.Combine(newLogPath, xmlLogFile);
            xmlFilePathForEmail = newLogPathForEmail + "/" + xmlLogFile;
            if (!File.Exists(xmlFilePath))
            {
                CreateXmlFile(xmlFilePath);
            }

            // Create xsl file to be used while show test result
            string xslFilePath = Path.Combine(newLogPath, "workspace.xsl");
            if (!File.Exists(xslFilePath))
            {
                CreateXslFile(xslFilePath);
            }

            // Create XML Document object 
            xmlDoc = XDocument.Load(xmlFilePath);
        }

        /// <summary>
        /// Creates Xml Log File 
        /// </summary>
        /// <param name="xmlFilePath">The log file name to be created</param>
        private static void CreateXmlFile(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;

                using (XmlWriter writer = XmlWriter.Create(xmlFilePath, settings))
                {
                    // Write the Processing Instruction node
                    string style = "type=\"text/xsl\" href=\"workspace.xsl\"";
                    writer.WriteProcessingInstruction("xml-stylesheet", style);

                    writer.WriteStartElement("root");
                    writer.WriteEndElement();

                    writer.Flush();
                    writer.Close();
                }
            }
        }

        /// <summary>
        /// Creates Xsl File 
        /// </summary>
        /// <param name="xslFilePath">The xsl file name to be created</param>
        private static void CreateXslFile(string xslFilePath)
        {
            if (!File.Exists(xslFilePath))
            {
                File.WriteAllText(xslFilePath, string.Format(CultureInfo.InvariantCulture, ResourceReader.ReadXsl(), ConfigReader.ClientName, ConfigReader.SiteName));
            }
        }

        #endregion

        #endregion
    }
}
