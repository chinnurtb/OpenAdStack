//-----------------------------------------------------------------------
// <copyright file="GenericXmlSerializableBase.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
