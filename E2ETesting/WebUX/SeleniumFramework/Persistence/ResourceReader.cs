// -----------------------------------------------------------------------
// <copyright file="ResourceReader.cs"  company="Rare Crowds Inc">
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

using System.IO;
using System.Reflection;

namespace SeleniumFramework.Persistence
{
    /// <summary>
    /// This class contains method to read content from various resource
    /// </summary>
    public static class ResourceReader
    {
        #region Methods

        /// <summary>
        /// Get the xsl file content from the Assembly 
        /// </summary>
        /// <returns>xsl content</returns>
        public static string ReadXsl()
        {
            string result = null;

            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream("SeleniumFramework.Resources.xsl.workspace.xsl"))
            {
                using (StreamReader xslStreamReader = new StreamReader(stream))
                {
                    result = xslStreamReader.ReadToEnd();
                }
            }

            return result;
        }

        #endregion
    }
}
