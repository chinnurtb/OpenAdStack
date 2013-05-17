// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityProperty.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataAccessLayer
{
    /// <summary>
    /// Name-Value pair abstraction for an Entity Property
    /// </summary>
    public class EntityProperty
    {
        /// <summary>Map for conversion from object to PropertyValue.</summary>
        private static readonly Dictionary<Type, Func<object, PropertyValue>> ObjectConversionMap = 
            new Dictionary<Type, Func<object, PropertyValue>>
                    {
                        { typeof(string), x => new PropertyValue((string)x) },
                        { typeof(int), x => new PropertyValue((int)x) },
                        { typeof(long), x => new PropertyValue((long)x) },
                        { typeof(double), x => new PropertyValue((double)x) },
                        { typeof(decimal), x => new PropertyValue((decimal)x) },
                        { typeof(bool), x => new PropertyValue((bool)x) },
                        { typeof(DateTime), x => new PropertyValue((DateTime)x) },
                        { typeof(EntityId), x => new PropertyValue((EntityId)x) },
                        { typeof(Guid), x => new PropertyValue((Guid)x) },
                        { typeof(byte[]), x => new PropertyValue((byte[])x) },
                    };

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityProperty"/> class.
        /// </summary>
        public EntityProperty()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="EntityProperty"/> class.</summary>
        /// <param name="prop">The property to copy this instance from.</param>
        public EntityProperty(EntityProperty prop)
        {
            this.Name = prop.Name;
            this.Value = prop.Value;
            this.IsBlobRef = prop.IsBlobRef;
            this.Filter = prop.Filter;
        }

        /// <summary>Initializes a new instance of the <see cref="EntityProperty"/> class.</summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        public EntityProperty(string propertyName, PropertyValue propertyValue) : 
            this(propertyName, propertyValue, PropertyFilter.Default)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="EntityProperty"/> class.</summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The PropertyValue.</param>
        /// <param name="filter">The PropertyFilter enum value.</param>
        public EntityProperty(string propertyName, PropertyValue propertyValue, PropertyFilter filter)
        {
            this.Name = propertyName;
            this.Value = propertyValue;
            this.IsBlobRef = false;
            this.Filter = filter;
        }

        /// <summary>Gets or sets external (partner) name of a property.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets value of a property.</summary>
        public PropertyValue Value { get; set; }

        /// <summary>Gets or sets a value indicating whether the property is a blob reference.</summary>
        public bool IsBlobRef { get; set; }

        /// <summary>Gets a value indicating whether the property should be treated as a default property.</summary>
        public bool IsDefaultProperty
        {
            get { return this.Filter == PropertyFilter.Default; }
        }

        /// <summary>Gets a value indicating whether the property should be treated as a system property.</summary>
        public bool IsSystemProperty
        {
            get { return this.Filter == PropertyFilter.System; }
        }

        /// <summary>Gets a value indicating whether the property should be treated as an extended property.</summary>
        public bool IsExtendedProperty
        {
            get { return this.Filter == PropertyFilter.Extended; }
        }

        /// <summary>Gets or sets the PropertyFilter value (e.g. - Default, Extended, or System property)</summary>
        public PropertyFilter Filter { get; set; }

        ////
        // Copy Constructors
        // These will create an un-named (Name = string.Empty) EntityProperty with
        // a Value based on the given type. You should only use this if you need to defer
        // providing the name. For instance, they are used implicitly when assigning values to
        // IEntity members where the Property setter will provide the name.
        ////

        /// <summary>Copy construct un-named EntityProperty from string.</summary>
        /// <param name="value">The value as string.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(string value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from Int32.</summary>
        /// <param name="value">The value as Int32.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(int value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from Int64.</summary>
        /// <param name="value">The value as Int64.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(long value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from double.</summary>
        /// <param name="value">The value as double.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(double value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from decimal.</summary>
        /// <param name="value">The value as decimal.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(decimal value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from bool.</summary>
        /// <param name="value">The value as bool.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(bool value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from Date.</summary>
        /// <param name="value">The value as Date.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(DateTime value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from Guid.</summary>
        /// <param name="value">The value as Guid.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(Guid value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        /// <summary>Copy construct un-named EntityProperty from EntityId.</summary>
        /// <param name="value">The value as EntityId.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(EntityId value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }
        
        /// <summary>Copy construct un-named EntityProperty from Binary.</summary>
        /// <param name="value">The value as Binary.</param>
        /// <returns>An un-named EntityProperty.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityProperty(byte[] value)
        {
            return new EntityProperty { Name = string.Empty, Value = value };
        }

        //// 
        // Cast operators
        // Implement the same cast operators as the contained PropertyValue object.
        // This allows us to extract the value straight forwardly.
        //// 

        /// <summary>Cast value to native string type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native string.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator string(EntityProperty property)
        {
            return CheckAndGetValueAs<string>(property);
        }
        
        /// <summary>Cast value to native int type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native int.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator int(EntityProperty property)
        {
            return CheckAndGetValueAs<int>(property);
        }

        /// <summary>Cast value to native long type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native long.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator long(EntityProperty property)
        {
            return CheckAndGetValueAs<long>(property);
        }
        
        /// <summary>Cast value to native double type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native double.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator double(EntityProperty property)
        {
            return CheckAndGetValueAs<double>(property);
        }

        /// <summary>Cast value to native decimal type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native decimal.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator decimal(EntityProperty property)
        {
            return CheckAndGetValueAs<decimal>(property);
        }

        /// <summary>Cast value to native DateTime type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native DateTime.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator DateTime(EntityProperty property)
        {
            return CheckAndGetValueAs<DateTime>(property);
        }

        /// <summary>Cast value to native bool type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native bool.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator bool(EntityProperty property)
        {
            return CheckAndGetValueAs<bool>(property);
        }

        /// <summary>Cast value to native Guid type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native Guid.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator Guid(EntityProperty property)
        {
            return CheckAndGetValueAs<Guid>(property);
        }

        /// <summary>Cast value to EntityId type. PropertyType of valeu must be PropertyType.Guid</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as EntityId.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator EntityId(EntityProperty property)
        {
            return CheckAndGetValueAs<EntityId>(property);
        }
        
        /// <summary>Cast value to native byte[] type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native byte[].</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2225", Justification = "Not a public API.")]
        public static implicit operator byte[](EntityProperty property)
        {
            return CheckAndGetValueAs<byte[]>(property);
        }

        ////
        // Begin Equality Operators
        ////

        /// <summary>Equality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(EntityProperty left, EntityProperty right)
        {
            return Equals(left, right);
        }

        /// <summary>Inequality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(EntityProperty left, EntityProperty right)
        {
            return !Equals(left, right);
        }

        /// <summary>Equals method override.</summary>
        /// <param name="other">The other EntityProperty object being equated.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(EntityProperty other)
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

            // Test based on member name and value
            return Equals(other.Value, this.Value) && Equals(other.Name, this.Name);
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

            if (obj.GetType() == typeof(EntityProperty))
            {
                // Defer to typed Equals
                return this.Equals((EntityProperty)obj);
            }

            // Allow comparison to an object that has a PropertyValue constructor
            // available. Compare on the basis of value only, not name.
            return Equals(TryConvertToPropertyValue(obj), this.Value);
        }

        /// <summary>Hash function for EntityProperty type.</summary>
        /// <returns>A hash code based on the member values.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // (* 397 is just an arbitrary distribution function)
                return ((this.Name != null ? this.Name.GetHashCode() : 0) * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
            }
        }

        ////
        // End Equality Operators
        ////

        /// <summary>ToString override.</summary>
        /// <returns>String serialized representation of value.</returns>
        public override string ToString()
        {
            return this.Value.ToString();
        }

        /// <summary>Get the underlying value as type T. Delegate null handling to PropertyValue</summary>
        /// <typeparam name="T">The type to attempt to get.</typeparam>
        /// <param name="property">The EntityProperty object.</param>
        /// <returns>A value of type T if successful.</returns>
        /// <exception cref="ArgumentException">Throws if coercion is not possible.</exception>
        private static T CheckAndGetValueAs<T>(EntityProperty property)
        {
            // Defer null handling to PropertyValue
            return PropertyValue.CheckAndGetValueAs<T>(property != null ? property.Value : null);
        }

        /// <summary>Convert an object to a PropertyValue if possible.</summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The property value or null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Try pattern.")]
        private static PropertyValue TryConvertToPropertyValue(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            try
            {
                return ObjectConversionMap[obj.GetType()](obj);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
