// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataSourceCreationUtility.cs" company="Rare Crowds Inc">
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
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace TestUtilities
{
    /// <summary>
    /// Methods to help in the creation of data sources for data driven testing
    /// </summary>
    public static class DataSourceCreationUtility
    {
        /// <summary>
        /// Creates files with XML strings to be used as a datasource in testing
        /// </summary>
        /// <typeparam name="TInputObject">input type</typeparam>
        /// <typeparam name="TExpectedObject">expected output type</typeparam>
        /// <param name="inputs">an enumerable of the inputs</param>
        /// <param name="expectedOutputs">an enumerable of the expectedOutputs</param>
        /// <param name="filename">the filename (creates a file with name filename.csv</param>
        /// <param name="path">the path where the file should be created</param>
        public static void CreateDataSources<TInputObject, TExpectedObject>(IEnumerable<TInputObject> inputs, IEnumerable<TExpectedObject> expectedOutputs, string filename, string path)
        {
            using (var writer = new StreamWriter(path + "\\" + filename + ".csv"))
            {
                writer.WriteLine("input, expected");

                foreach (var kvp in inputs.Zip(expectedOutputs, (i, e) => new KeyValuePair<TInputObject, TExpectedObject>(i, e)))
                {
                    // hacky way to get quotes around the xml - probably a better way available
                    writer.WriteLine(string.Format(
                        CultureInfo.InvariantCulture, 
                        @"""{0}"",""{1}""", 
                        SerializeObjectToXml(kvp.Key),
                        SerializeObjectToXml(kvp.Value)));
                }
            }
        }

        /// <summary>
        /// Creates files with XML strings to be used as a datasource in testing
        /// if path is unspecified, the current directory is used
        /// </summary>
        /// <typeparam name="TInputObject">input type</typeparam>
        /// <typeparam name="TExpectedObject">expected output type</typeparam>
        /// <param name="inputs">an enumerable of the inputs</param>
        /// <param name="expectedOutputs">an enumerable of the expectedOutputs</param>
        /// <param name="filename">the filename (creates a file with name filename.csv</param>
        public static void CreateDataSources<TInputObject, TExpectedObject>(IEnumerable<TInputObject> inputs, IEnumerable<TExpectedObject> expectedOutputs, string filename)
        {
            // if unspecified, set the path to the current directory
            var currentPath = Directory.GetCurrentDirectory();
            CreateDataSources<TInputObject, TExpectedObject>(inputs, expectedOutputs, filename, currentPath);
        }

        /// <summary>
        /// serializes object of type T to xml and saves to a file (used to help create data sources for data driven testing)
        /// </summary>
        /// <typeparam name="T">the type of object to serialize</typeparam>
        /// <param name="obj">the object to serialize</param>
        /// <param name="filename">the filename (without the '.xml' extension)</param>
        public static void SerializeObjectToXml<T>(T obj, string filename)
        {
            var currentPath = Directory.GetCurrentDirectory();

            using (var writer = new FileStream(currentPath + "\\" + filename + ".xml", FileMode.Create))
            {
                var ser = new DataContractSerializer(typeof(T));
                ser.WriteObject(writer, obj);
            }
        }

        /// <summary>
        /// serializes object of type T to xml and returns a string (used to help create data sources for data driven testing)
        /// </summary>
        /// <typeparam name="T">the type of object to serialize</typeparam>
        /// <param name="obj">the object to serialize</param>
        /// <returns>xml string</returns>
        public static string SerializeObjectToXml<T>(T obj)
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            settings.CloseOutput = false;
            using (var writer = XmlWriter.Create(sb, settings))
            {
                var serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(writer, obj);
            }

            // hacky way to escape quotes - probably a bette way available
            var escapedXml = sb.ToString().Replace(@"""", @"""""");
            return escapedXml;
        }
    }
}
