// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ODataName.cs" company="Rare Crowds Inc">
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

using System;

namespace ConcreteDataStore
{
    /// <summary>Abstraction of an OData name.</summary>
    internal class ODataName
    {
        /// <summary>Marks a collection.</summary>
        protected const string CollectionMarker = "c";

        /// <summary>Marks a single value.</summary>
        protected const string ValueMarker = "v";

        /// <summary>Marks a system value</summary>
        protected const string SystemMarker = "sys";

        /// <summary>Marks a extended value</summary>
        protected const string ExtendedMarker = "ext";

        /// <summary>Marks an association</summary>
        protected const string AssociationMarker = "ptr";

        /// <summary>Marks a blob reference</summary>
        protected const string BlobRefMarker = "bref";

        /// <summary>The delimiter for name fields.</summary>
        protected const string ODataNameDelimiter = "_";

        /// <summary>List of all markers</summary>
        protected static readonly string[] Markers = new[] { CollectionMarker, ValueMarker, SystemMarker, ExtendedMarker, AssociationMarker, BlobRefMarker };

        /// <summary>Initializes a new instance of the <see cref="ODataName"/> class.</summary>
        /// <param name="odataName">The odata name.</param>
        public ODataName(string odataName)
        {
            this.ODataNameFields = odataName.Split(new[] { ODataNameDelimiter }, StringSplitOptions.None);
        }

        /// <summary>Gets the field strings.</summary>
        protected string[] ODataNameFields { get; private set; }

        /// <summary>Determine if this name refers to a collection.</summary>
        /// <returns>True if it's a collection.</returns>
        public bool IsCollection()
        {
            return HasMarker(this.ODataNameFields, CollectionMarker);
        }

        /// <summary>Determine if this name refers to an association.</summary>
        /// <returns>True if it's an association.</returns>
        public bool IsAssociation()
        {
            return HasMarker(this.ODataNameFields, AssociationMarker);
        }

        /// <summary>Determine if this name refers to a blob reference.</summary>
        /// <returns>True if it's a blob reference.</returns>
        public bool IsBlobRef()
        {
            return HasMarker(this.ODataNameFields, BlobRefMarker);
        }

        /// <summary>Determine if this name refers to a system property.</summary>
        /// <returns>True if it's a system property.</returns>
        public bool IsSystem()
        {
            return HasMarker(this.ODataNameFields, SystemMarker);
        }

        /// <summary>Determine if this name refers to an extended property.</summary>
        /// <returns>True if it's an extended property.</returns>
        public bool IsExtended()
        {
            return HasMarker(this.ODataNameFields, ExtendedMarker);
        }

        /// <summary>Searches the name fields for the a marker</summary>
        /// <param name="odataNameFields">The OData name fields</param>
        /// <param name="marker">The marker</param>
        /// <returns>True if the marker is present</returns>
        private static bool HasMarker(string[] odataNameFields, string marker)
        {
            foreach (var field in odataNameFields)
            {
                if (field == marker)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
