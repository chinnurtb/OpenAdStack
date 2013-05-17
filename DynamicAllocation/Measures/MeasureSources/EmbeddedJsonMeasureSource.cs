//-----------------------------------------------------------------------
// <copyright file="EmbeddedJsonMeasureSource.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.IO;
using System.Reflection;

namespace DynamicAllocation
{
    /// <summary>Source for measures loaded from embedded JSON resources</summary>
    public class EmbeddedJsonMeasureSource : JsonMeasureSource, IMeasureSource
    {
        /// <summary>Assembly containing the embedded resource</summary>
        private readonly Assembly resourceAssembly;

        /// <summary>The embedded resource name</summary>
        private readonly string resourceName;

        /// <summary>Backing field for JsonMeasureSource.MeasureJson</summary>
        private string measureJson;

        /// <summary>Initializes a new instance of the EmbeddedJsonMeasureSource class.</summary>
        /// <param name="resourceAssembly">Assembly containing the embedded resource</param>
        /// <param name="resourceName">The embedded resource name</param>
        public EmbeddedJsonMeasureSource(Assembly resourceAssembly, string resourceName)
            : base("RESOURCE:{0}".FormatInvariant(resourceName))
        {
            this.resourceAssembly = resourceAssembly;
            this.resourceName = resourceName;
        }

        /// <summary>
        /// Gets the JSON containing this source's measures loaded from an embedded resource.
        /// </summary>
        protected override string MeasureJson
        {
            get
            {
                if (this.measureJson == null)
                {
                    this.LoadMeasuresFromResource();
                }

                return this.measureJson;
            }
        }

        /// <summary>Loads measures JSON from an embedded resource</summary>
        private void LoadMeasuresFromResource()
        {
            var res = this.resourceAssembly.GetManifestResourceStream(this.resourceName);
            if (res == null)
            {
                var message = "Unable to load measure map '{0}' from assembly '{1}'"
                    .FormatInvariant(this.resourceName, typeof(MeasureMap).Assembly.FullName);
                throw new InvalidOperationException(message);
            }

            using (var reader = new StreamReader(res))
            {
                this.measureJson = reader.ReadToEnd();
            }
        }
    }
}
