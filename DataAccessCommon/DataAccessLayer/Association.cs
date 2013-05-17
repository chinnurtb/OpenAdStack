// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Association.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace DataAccessLayer
{
    /// <summary>
    /// Abstraction of an entity association
    /// </summary>
    public class Association
    {
        /// <summary>Map of Association property names to property setters.</summary>
        internal static readonly Dictionary<string, Action<Association, string, string>> NameToSetter = new Dictionary<string, Action<Association, string, string>>
                    {
                        { "TargetEntityId", (assoc, name, value) => SetAssociationProperty(assoc, name, (EntityId)value) },
                        { "TargetEntityCategory", (assoc, name, value) => SetAssociationProperty(assoc, name, value) },
                        { "TargetExternalType", (assoc, name, value) => SetAssociationProperty(assoc, name, value) },
                        { "AssociationType", (assoc, name, value) => SetAssociationProperty(assoc, name, AssociationTypeFromString(value)) }
                    };

        /// <summary>Initializes a new instance of the <see cref="Association"/> class.</summary>
        public Association()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Association"/> class.</summary>
        /// <param name="association">The association to copy this instance from.</param>
        public Association(Association association)
        {
            this.AssociationType = association.AssociationType;
            this.ExternalName = association.ExternalName;
            this.TargetEntityCategory = association.TargetEntityCategory;
            this.TargetExternalType = association.TargetExternalType;
            this.TargetEntityId = association.TargetEntityId;
        }

        /// <summary>Gets or sets the external entity id of the target entity.</summary>
        public EntityId TargetEntityId { get; set; }

        /// <summary>Gets or sets the internal entity category of the target entity.</summary>
        public string TargetEntityCategory { get; set; }

        /// <summary>Gets or sets the external (partner) type of the target entity.</summary>
        public string TargetExternalType { get; set; }

        /// <summary>Gets or sets the external (partner) name of the association.</summary>
        public string ExternalName { get; set; }

        /// <summary>Gets or sets the internal type of association.</summary>
        public AssociationType AssociationType { get; set; }

        /// <summary>Gets or sets additional metadata associated with the association. Not Currently Supported.</summary>
        public string Details { get; set; }

        /// <summary>Get AssociationType from a string serialized form.</summary>
        /// <param name="type">The AssociationType as string.</param>
        /// <returns>The AssociationType</returns>
        /// <exception cref="ArgumentException">Throws if the string does not map to one of our AssociationType's.</exception>
        public static AssociationType AssociationTypeFromString(string type)
        {
            AssociationType outType;
            if (!Enum.TryParse(type, out outType))
            {
                throw new ArgumentException("Association type not recognized: {0}".FormatInvariant(type));
            }

            return outType;
        }

        /// <summary>Get string serialized from of a AssociationType.</summary>
        /// <param name="type">The type as AssociationType.</param>
        /// <returns>The AssociationType as a string.</returns>
        public static string StringFromAssociationType(AssociationType type)
        {
            return Enum.GetName(typeof(AssociationType), type);
        }

        ////
        // Begin Equality Operators
        ////

        /// <summary>Equality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(Association left, Association right)
        {
            return Equals(left, right);
        }

        /// <summary>Inequality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(Association left, Association right)
        {
            return !Equals(left, right);
        }

        /// <summary>Equals method override.</summary>
        /// <param name="other">The other Association object being equated.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(Association other)
        {
            // Degenerate cases
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Test based on member values
            return other.TargetEntityId == this.TargetEntityId 
                && Equals(other.TargetEntityCategory, this.TargetEntityCategory) 
                && Equals(other.TargetExternalType, this.TargetExternalType) 
                && Equals(other.ExternalName, this.ExternalName) 
                && Equals(other.AssociationType, this.AssociationType);
        }

        /// <summary>Equals method override for generic object.</summary>
        /// <param name="obj">The other object being equated.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals(object obj)
        {
            // Degenerate cases
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(Association))
            {
                return false;
            }

            // Defer to the typed Equals
            return this.Equals((Association)obj);
        }

        /// <summary>Hash function for Association type.</summary>
        /// <returns>A hash code based on the member values.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // (* 397 is just an arbitrary distribution function)
                int result = this.TargetEntityCategory != null ? this.TargetEntityCategory.GetHashCode() : 0;
                result = (result * 397) ^ (this.TargetEntityCategory != null ? this.TargetEntityCategory.GetHashCode() : 0);
                result = (result * 397) ^ (this.TargetExternalType != null ? this.TargetExternalType.GetHashCode() : 0);
                result = (result * 397) ^ (this.ExternalName != null ? this.ExternalName.GetHashCode() : 0);
                result = (result * 397) ^ this.AssociationType.GetHashCode();
                return result;
            }
        }

        ////
        // End Equality Operators
        ////

        /// <summary>Set the association property corresponding to name.</summary>
        /// <param name="association">The association to set.</param>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        private static void SetAssociationProperty<T>(Association association, string name, T value)
        {
            typeof(Association).GetProperty(name).SetValue(association, value, null);
        }
    }
}
