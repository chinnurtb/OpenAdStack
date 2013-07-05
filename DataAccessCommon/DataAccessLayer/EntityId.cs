//-----------------------------------------------------------------------
// <copyright file="EntityId.cs" company="Rare Crowds Inc.">
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DataAccessLayer
{
    /// <summary>
    /// Abstraction of identifiers in Entites
    /// </summary>
    public class EntityId
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityId"/> class with a valid Id.
        /// </summary>
        public EntityId() : this(Guid.NewGuid())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityId"/> class with a guid.
        /// </summary>
        /// <param name="idValue">a Guid.</param>
        public EntityId(Guid idValue)
        {
            this.Id = idValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityId"/> class with a valid Id.
        /// </summary>
        /// <param name="idValue">String representation of a Guid.</param>
        /// <exception cref="ArgumentException">Value must be parsable as a Guid.</exception>
        public EntityId(string idValue) : this(StringToId(idValue))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityId"/> class from a long.
        /// </summary>
        /// <param name="idValue">A positive long integer.</param>
        /// <exception cref="ArgumentException">Value must be non-negative.</exception>
        public EntityId(long idValue) : this(LongToId(idValue))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityId"/> class from another instance.
        /// </summary>
        /// <param name="fromId">Instance to initialize from.</param>
        public EntityId(EntityId fromId)
        {
            this.Id = fromId.Id;
        }

        /// <summary>Gets or sets underlying Guid.</summary>
        protected Guid Id { get; set; }

        /// <summary>Assignment operator overload for Guid</summary>
        /// <param name="idValue">The Guid id value.</param>
        /// <returns>The EntityId</returns>
        public static implicit operator EntityId(Guid idValue)
        {
            return new EntityId(idValue);
        }

        /// <summary>Assignment operator overload to Guid</summary>
        /// <param name="idValue">The EntityId value.</param>
        /// <returns>The Guid</returns>
        public static implicit operator Guid(EntityId idValue)
        {
            return idValue.Id;
        }

        /// <summary>Assignment operator overload to string</summary>
        /// <param name="idValue">The EntityId value.</param>
        /// <returns>The string representation of the entity id.</returns>
        public static implicit operator string(EntityId idValue)
        {
            return idValue.ToString();
        }

        /// <summary>Assignment operator overload for long</summary>
        /// <param name="idValue">The long id value.</param>
        /// <returns>The EntityId</returns>
        public static implicit operator EntityId(long idValue)
        {
            return new EntityId(idValue);
        }

        /// <summary>Assignment operator overload for string</summary>
        /// <param name="idValue">The string representation of a Guid.</param>
        /// <returns>The EntityId</returns>
        public static implicit operator EntityId(string idValue)
        {
            return new EntityId(idValue);
        }

        /// <summary>Equality operator override.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(EntityId left, EntityId right)
        {
            return Equals(left, right);
        }

        /// <summary>Inequality operator override.</summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>True if no equal.</returns>
        public static bool operator !=(EntityId left, EntityId right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Tries to parse a string as an EntityId.
        /// </summary>
        /// <param name="idValue">String representation of a Guid.</param>
        /// <returns>True if successfully parsed and out param set.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Validator")]
        [SuppressMessage("Microsoft.Usage", "CA1806", Justification = "Validator")]
        public static bool IsValidEntityId(string idValue)
        {
            try
            {
                new EntityId(idValue);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>Override of Equals method based underlying Guid.</summary>
        /// <param name="other">The other.</param>
        /// <returns>True if underlying Guid is equal.</returns>
        public bool Equals(EntityId other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other.Id.Equals(this.Id);
        }

        /// <summary>Override of Equals method based underlying Guid.</summary>
        /// <param name="obj">The obj.</param>
        /// <returns>True if underlying Guid is equal.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(EntityId))
            {
                return false;
            }

            return this.Equals((EntityId)obj);
        }

        /// <summary>Equality hashcode.</summary>
        /// <returns>The underlying Guid hashcode.</returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        /// <summary>Get string representation of underlying Guid.</summary>
        /// <returns>Result string.</returns>
        public override string ToString()
        {
            return this.Id.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert a long to valid guid representation
        /// </summary>
        /// <param name="idValue">The long id value.</param>
        /// <exception cref="ArgumentException">Value must be non-negative.</exception>
        /// <returns>A Guid if successful.</returns>
        private static Guid LongToId(long idValue)
        {
            if (idValue < 0)
            {
                throw new ArgumentException("idValue - parameter must not be negative: {0}"
                    .FormatInvariant(idValue));
            }

            // A valid guid string representation needs 32 digits.
            // Pad the input to a length of 19 (max for a long) and pad the remaining 13 with 0s.
            var idString = string.Format(CultureInfo.InvariantCulture, "{0:D13}{1:D19}", 0, idValue);
            return StringToId(idString);
        }

        /// <summary>
        /// Convert at string to the underlying guid id.
        /// </summary>
        /// <param name="idString">String representation of id.</param>
        /// <exception cref="ArgumentException">Value must be parsable as a Guid.</exception>
        /// <returns>A Guid if successful.</returns>
        private static Guid StringToId(string idString)
        {
            Guid idGuid;
            if (!Guid.TryParse(idString, out idGuid))
            {
                throw new ArgumentException("Input string is not a valid id format: {0}"
                    .FormatInvariant(idString));
            }

            return idGuid;
        }
    }
}
