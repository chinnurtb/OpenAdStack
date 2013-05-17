// -----------------------------------------------------------------------
// <copyright file="ResourceReader.cs"  company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
