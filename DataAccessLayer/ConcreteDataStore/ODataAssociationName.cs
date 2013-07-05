//-----------------------------------------------------------------------
// <copyright file="ODataAssociationName.cs" company="Rare Crowds Inc.">
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Abstraction of an OData name for an association.
    /// The field name will for an association will be of the 
    /// form v_ptr_Relationship_Company_Agency_ParentCompany
    /// The name will be mangled to conform to our rules so we shouldn't
    /// have to validate correctness here.
    /// </summary>
    internal class ODataAssociationName : ODataName
    {
        /// <summary>Lookup table of indexes into the odata name.</summary>
        private readonly Dictionary<string, int> odataAssociationNameLU = new Dictionary<string, int>
            {
                { "AssociationType", 2 },
                { "TargetEntityCategory", 3 },
                { "TargetExternalType", 4 },
                { "ExternalName", 5 }
            };

        /// <summary>Initializes a new instance of the <see cref="ODataAssociationName"/> class.</summary>
        /// <param name="odataName">The odata name.</param>
        public ODataAssociationName(string odataName)
            : base(odataName)
        {
        }

        /// <summary>Gets AssociationType.</summary>
        public AssociationType AssociationType
        {
            get { return Association.AssociationTypeFromString(this.ODataNameFields[this.odataAssociationNameLU["AssociationType"]]); }
        }

        /// <summary>Gets TargetEntityCategory.</summary>
        public string TargetEntityCategory
        {
            get { return this.ODataNameFields[this.odataAssociationNameLU["TargetEntityCategory"]]; }
        }

        /// <summary>Gets the Association Group key</summary>
        public Association AssociationGroupKey
        {
            get { return this.TryBuildAssociationGroupKey(); }
        }

        /// <summary>Serialize an association group key to an odata name.</summary>
        /// <param name="associationGroupKey">
        /// An Association instance with the association group key fields populated.
        /// </param>
        /// <returns>The odata name</returns>
        public static string SerializeAssociationGroup(Association associationGroupKey)
        {
            // ptr_AssociationType_Category_TargetExternalType_ExternalName
            // Note this assumes that the ODataNameDelimiter has already been encoded if it
            // appears in an enternal name field
            return @"{0}{1}{2}{1}{3}{1}{4}{1}{5}{1}{6}".FormatInvariant(
                CollectionMarker,
                ODataNameDelimiter,
                AssociationMarker,
                Association.StringFromAssociationType(associationGroupKey.AssociationType),
                associationGroupKey.TargetEntityCategory,
                associationGroupKey.TargetExternalType,
                associationGroupKey.ExternalName);
        }

        /// <summary>
        /// Build an association group key from the name fields.
        /// </summary>
        /// <returns>An Association with the group key fields or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private Association TryBuildAssociationGroupKey()
        {
            try
            {
                return new Association
                {
                    AssociationType = Association.AssociationTypeFromString(
                        this.ODataNameFields[this.odataAssociationNameLU["AssociationType"]]),
                    TargetEntityCategory =
                        this.ODataNameFields[this.odataAssociationNameLU["TargetEntityCategory"]],
                    TargetExternalType = this.ODataNameFields[this.odataAssociationNameLU["TargetExternalType"]],
                    ExternalName = this.ODataNameFields[this.odataAssociationNameLU["ExternalName"]]
                };
            }
            catch (Exception)
            {
            }

            return null;
        }
    }
}
