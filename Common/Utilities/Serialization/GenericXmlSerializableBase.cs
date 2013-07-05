//-----------------------------------------------------------------------
// <copyright file="GenericXmlSerializableBase.cs" company="Rare Crowds Inc">
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

using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Utilities.Serialization
{
    /// <summary>
    /// Abstract base class for XML serializable types
    /// </summary>
    /// <typeparam name="TSerializableType">Derived type</typeparam>
    [DataContract]
    public abstract class GenericXmlSerializableBase<TSerializableType>
        where TSerializableType : GenericXmlSerializableBase<TSerializableType>
    {
        /// <summary>
        /// Serializer for serializing and deserializing ActivityRequests
        /// </summary>
        private static readonly DataContractSerializer serializer = new DataContractSerializer(typeof(TSerializableType));
        
        /// <summary>
        /// Writes this TDerivedType instance to XML
        /// </summary>
        /// <returns>The xml</returns>
        public string SerializeToXml()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                using (var xmlWriter = new XmlTextWriter(stringWriter) { Formatting = Formatting.Indented })
                {
                    serializer.WriteObject(xmlWriter, this);
                }

                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Creates a new TDerivedType instance from XML
        /// </summary>
        /// <param name="requestXml">The xml</param>
        /// <returns>The TDerivedType instance</returns>
        protected static TSerializableType DeserializeFromXmlInternal(string requestXml)
        {
            using (var stringReader = new StringReader(requestXml))
            {
                using (var xmlReader = new System.Xml.XmlTextReader(stringReader))
                {
                    return (TSerializableType)serializer.ReadObject(xmlReader);
                }
            }
        }
    }
}
