//-----------------------------------------------------------------------
// <copyright file="AssociationComparer.cs" company="Rare Crowds Inc.">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DataAccessLayer;

namespace ConcreteDataStore
{
    /// <summary>
    /// Custom comparer for Associations when grouping for serialization.
    /// </summary>
    internal class AssociationComparer : IEqualityComparer<Association>
    {
        /// <summary>Determines whether the specified objects are equal.</summary>
        /// <param name="a1">The first object of type Association to compare.</param>
        /// <param name="a2">The second object of type Association to compare.</param>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(Association a1, Association a2)
        {
            if (a1 == null || a2 == null)
            {
                return false;
            }

            return BuildCompoundKey(a1) == BuildCompoundKey(a2);
        }

        /// <summary>Returns a hash code for the specified object.</summary>
        /// <returns>A hash code for the specified object.</returns>
        /// <param name="a">The Association for which a hash code is to be returned.</param>
        public int GetHashCode(Association a)
        {
            return BuildCompoundKey(a).GetHashCode();
        }

        /// <summary>
        /// Group a collection of Associations grouped by a compound key of
        /// ExternalName, TargetEntityCategory, TargetExternalType, and AssociationType
        /// </summary>
        /// <param name="associations">The collection of associations.</param>
        /// <returns>
        /// A collection of tuples mapping each compound key to the TargetEntityId's having that key.
        /// </returns>
        internal static IDictionary<Association, EntityId[]> BuildEntityAssociationGroups(IEnumerable<Association> associations)
        {
            return associations.GroupBy(assoc => assoc, new AssociationComparer())
                .ToDictionary(
                    kvp => BuildEncodedAssociationKeyFields(kvp.Key),
                    kvp => kvp.Select(groupItem => groupItem.TargetEntityId).ToArray());
        }

        /// <summary>
        /// Build an Association with those fields that form a compound key for the association groups
        /// (ExternalName, TargetEntityCategory, TargetExternalType, and AssociationType).
        /// The 'External' fields should be encoded since they are not necessarily under
        /// our control.
        /// </summary>
        /// <param name="association">The source association.</param>
        /// <returns>An association with the key fields populated.</returns>
        internal static Association BuildEncodedAssociationKeyFields(Association association)
        {
            return new Association
            {
                AssociationType = association.AssociationType,
                ExternalName = AzureNameEncoder.EncodeAzureName(association.ExternalName),
                TargetEntityCategory = association.TargetEntityCategory,
                TargetExternalType = AzureNameEncoder.EncodeAzureName(association.TargetExternalType)
            };
        }

        /// <summary>Build a compound key string from the elements of the Association to be compared.</summary>
        /// <param name="a">The association.</param>
        /// <returns>The compound key string.</returns>
        private static string BuildCompoundKey(Association a)
        {
            var key = string.Concat(
                a.ExternalName ?? string.Empty,
                a.TargetEntityCategory ?? string.Empty,
                a.TargetExternalType ?? string.Empty,
                Enum.GetName(typeof(AssociationType), a.AssociationType));

            return key;
        }
    }
}