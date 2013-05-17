//Copyright 2012-2013 Rare Crowds, Inc.
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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml;

namespace DataServiceUtilities
{
    /// <summary>Utilities for data service activities</summary>
    public static class DataServiceActivityUtilities
    {
        /// <summary>URL for the loading cell image</summary>
        private const string LoadingImagePath = "dyn_down.gif";

        /// <summary>Formats the results as dhtmlxtreegrid XML data</summary>
        /// <typeparam name="TResult">Type of the results being formatted</typeparam>
        /// <param name="resultSubtree">Subtree of results to format</param>
        /// <param name="rootRowsId">Id for the root &lt;rows&gt; element.</param>
        /// <param name="getResultId">Function for getting result leaf node ids</param>
        /// <param name="pathSeparator">Separator for result key paths</param>
        /// <returns>The results formatted for dhtmlxtreegrid</returns>
        public static string FormatResultsAsDhtmlxTreeGridXml<TResult>(
            IDictionary<string, object> resultSubtree,
            string rootRowsId,
            Func<TResult, string> getResultId,
            char pathSeparator)
        {
            if (resultSubtree.All(kvp => kvp.Value == null))
            {
                // No results to return
                return null;
            }

            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var xmlWriter = new XmlTextWriter(stringWriter))
                {
                    xmlWriter.WriteStartElement("rows");
                    xmlWriter.WriteAttributeString("parent", HttpUtility.UrlEncode(rootRowsId));

                    foreach (var branch in resultSubtree)
                    {
                        WriteDhtmlxTreeGridXml<TResult>(
                            branch,
                            getResultId,
                            pathSeparator,
                            xmlWriter);
                    }
                }

                return stringWriter.ToString();
            }
        }

        /// <summary>Writes dhtmlxtreegrid XML row(s) for the key/value pair</summary>
        /// <typeparam name="TResult">Type of the results at the leaf nodes</typeparam>
        /// <param name="kvp">The node in the subtree to write</param>
        /// <param name="getResultId">Function to get the result leaf node's ids</param>
        /// <param name="pathSeparator">Separator for the node key paths</param>
        /// <param name="writer">XmlWriter to write the dhtmlxtreegrid XML</param>
        private static void WriteDhtmlxTreeGridXml<TResult>(
            KeyValuePair<string, object> kvp,
            Func<TResult, string> getResultId,
            char pathSeparator,
            XmlWriter writer)
        {
            if (kvp.Value is IDictionary<string, object>)
            {
                var name = kvp.Key.Substring(Math.Max(0, kvp.Key.LastIndexOf(pathSeparator))).Trim(pathSeparator);
                var rowId = HttpUtility.UrlEncode(kvp.Key);
                var children = (IDictionary<string, object>)kvp.Value;
             
                writer.WriteStartElement("row");
                writer.WriteAttributeString("id", rowId);
                writer.WriteAttributeString("xmlkids", "1");
                writer.WriteAttributeString("call", "1");

                if (children.Any(child => child.Value == null))
                {
                    // Containers with null value children are still loading
                    writer.WriteStartElement("cell");
                    writer.WriteAttributeString("image", LoadingImagePath);
                    writer.WriteString(name);
                    writer.WriteEndElement();
                }
                else
                {
                    // Write container cell and children
                    writer.WriteElementString("cell", name);
                    foreach (var child in children)
                    {
                        WriteDhtmlxTreeGridXml(
                            child,
                            getResultId,
                            pathSeparator,
                            writer);
                    }
                }

                writer.WriteEndElement();
            }
            else if (kvp.Value is TResult)
            {
                var name = kvp.Key
                    .Substring(Math.Max(0, kvp.Key.LastIndexOf(pathSeparator)))
                    .Trim(pathSeparator);
                var rowId = getResultId((TResult)kvp.Value);
                writer.WriteStartElement("row");
                writer.WriteAttributeString("id", rowId);
                writer.WriteElementString("cell", name);
                writer.WriteEndElement();
            }
            else
            {
                throw new ArgumentException(
                    "Invalid value type: {0}. Valid value types are IDictionary<string, object> and {1}."
                    .FormatInvariant(kvp.Value != null ? kvp.Value.GetType().FullName : "<null>", typeof(TResult).FullName),
                    "kvp");
            }
        }
    }
}
