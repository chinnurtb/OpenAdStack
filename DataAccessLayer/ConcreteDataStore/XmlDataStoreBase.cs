// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDataStoreBase.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace ConcreteDataStore
{
    /// <summary>Shared XmlDataStore behavior</summary>
    internal abstract class XmlDataStoreBase
    {
        /// <summary>Initializes a new instance of the <see cref="XmlDataStoreBase"/> class.</summary>
        /// <param name="dataSet">The data Set.</param>
        protected XmlDataStoreBase(XmlIndexDataSet dataSet)
        {
            this.DataSet = dataSet;
        }

        /// <summary>Gets or sets the underlying DataSet.</summary>
        public DataSet DataSet { get; protected set; }

        /// <summary>Gets or sets BackingFile path.</summary>
        public string BackingFile { get; protected set; }

        /// <summary>Load the underlying DataSet from a backing file. The index data store must be
        /// properly initialized for the entity data to load.
        /// </summary>
        /// <param name="backingFile">The backing file.</param>
        /// <param name="createIfNotFound">Create a new backing file if the specified path is not found.</param>
        public virtual void LoadFromFile(string backingFile, bool createIfNotFound = false)
        {
            string xmlData;

            if (backingFile == "DefaultIndex")
            {
                xmlData = GetEmbeddedXmlAsString(typeof(XmlDataStoreBase), @"Resources.DefaultIndexStore.xml");
            }
            else
            {
                this.BackingFile = this.CreateBackingFileIfRequired(backingFile, createIfNotFound);

                var doc = new XmlDocument();
                doc.Load(backingFile);
                var sw = new StringWriter(CultureInfo.InvariantCulture);
                var xw = new XmlTextWriter(sw);
                doc.WriteTo(xw);
                xmlData = sw.ToString();
            }
            
            this.LoadFromXml(xmlData);
        }

        /// <summary>Load data store from xml string. The index data store must be
        /// properly initialized for the entity data to load.
        /// </summary>
        /// <param name="xmlData">An xml string conforming to the XmlDataStore schema.</param>
        public abstract void LoadFromXml(string xmlData);

        /// <summary>Commit changes to file if this instance is based on a file.</summary>
        public virtual void Commit()
        {
            // No-op if there is no backing file
            if (!string.IsNullOrEmpty(this.BackingFile))
            {
                this.DataSet.WriteXml(this.BackingFile);
            }
        }

        /// <summary>Read pre-defined tables from the schema.</summary>
        /// <param name="xmlData">An xml string conforming to the XmlDataStore schema..</param>
        protected void ReadTableData(string xmlData)
        {
            // Need to clear the table data before re-reading
            foreach (DataTable table in this.DataSet.Tables)
            {
                table.Clear();
            }

            this.DataSet.ReadXml(XmlReader.Create(new StringReader(xmlData)));
        }

        /// <summary>Create the backing file if it doesn't exist.</summary>
        /// <param name="backingFile">The backing file path.</param>
        /// <param name="createIfNotFound">Create a new backing file if the specified path is not found.</param>
        /// <exception cref="ArgumentException">Throw if directory doesn't exist.</exception>
        /// <returns>The fully qualified path of the backing file.</returns>
        protected string CreateBackingFileIfRequired(string backingFile, bool createIfNotFound = false)
        {
            var fullyQualifiedBackingFile = Path.GetFullPath(backingFile);

            // In the case where the file does exist already, we don't want to
            // overwrite it.
            if (File.Exists(fullyQualifiedBackingFile))
            {
                return fullyQualifiedBackingFile;
            }

            if (!createIfNotFound)
            {
                throw new ArgumentException("Datastore backing file does not exist.");
            }

            var dir = Path.GetDirectoryName(fullyQualifiedBackingFile);
            if (dir == null)
            {
                throw new ArgumentException("Datastore backing file path invalid.");
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            this.DataSet.WriteXml(fullyQualifiedBackingFile);

            return fullyQualifiedBackingFile;
        }

        /// <summary>Return an xml string from an embedded resource.</summary>
        /// <param name="scopingType">The type used to scope the resource location.</param>
        /// <param name="resourceName">The resource name.</param>
        /// <returns>Xml string.</returns>
        private static string GetEmbeddedXmlAsString(Type scopingType, string resourceName)
        {
            string result = string.Empty;
            var res = Assembly.GetExecutingAssembly().GetManifestResourceStream(scopingType, resourceName);

            if (res != null)
            {
                result = new StreamReader(res).ReadToEnd();
            }

            return result;
        }
    }
}
