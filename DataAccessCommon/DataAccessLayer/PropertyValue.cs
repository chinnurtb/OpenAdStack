// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyValue.cs" company="Rare Crowds Inc">
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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DataAccessLayer
{
    /// <summary>
    /// Serialization class for a 'variant' property value
    /// </summary>
    [DataContract]
    public class PropertyValue
    {
        /// <summary>Static map of PropertyType's to StringToValue Conversion methods.</summary>
        private static readonly Dictionary<PropertyType, Func<string, dynamic>> ToValueMap = new Dictionary<PropertyType, Func<string, dynamic>>
            {
                { PropertyType.String, s => StringConversions.StringToNativeString(s) },
                { PropertyType.Int32, s => StringConversions.StringToNativeInt32(s) },
                { PropertyType.Int64, s => StringConversions.StringToNativeInt64(s) },
                { PropertyType.Double, s => StringConversions.StringToNativeDouble(s) },
                { PropertyType.Date, s => StringConversions.StringToNativeDateTime(s) },
                { PropertyType.Bool, s => StringConversions.StringToNativeBool(s) },
                { PropertyType.Guid, s => StringConversions.StringToNativeGuid(s) },
                { PropertyType.Binary, s => StringConversions.StringToNativeByteArray(s) }
            };

        /// <summary>Static map of PropertyType's to ValueToString Conversion methods.</summary>
        private static readonly Dictionary<PropertyType, Func<dynamic, string>> ToStringMap = new Dictionary<PropertyType, Func<dynamic, string>>
            {
                { PropertyType.String, v => StringConversions.NativeStringToString((string)v) },
                { PropertyType.Int32, v => StringConversions.NativeInt32ToString((int)v) },
                { PropertyType.Int64, v => StringConversions.NativeInt64ToString((long)v) },
                { PropertyType.Double, v => StringConversions.NativeDoubleToString((double)v) },
                { PropertyType.Date, v => StringConversions.NativeDateTimeToString((DateTime)v) },
                { PropertyType.Bool, v => StringConversions.NativeBoolToString((bool)v) },
                { PropertyType.Guid, v => StringConversions.NativeGuidToString((Guid)v) },
                { PropertyType.Binary, v => StringConversions.NativeByteArrayToString((byte[])v) }
            };

        /// <summary>Static map of built-in types to PropertyType values.</summary>
        private static readonly Dictionary<Type, PropertyType> AllowedTypesMap = new Dictionary<Type, PropertyType>
                {
                    { typeof(string), PropertyType.String }, 
                    { typeof(int), PropertyType.Int32 }, 
                    { typeof(long), PropertyType.Int64 }, 
                    { typeof(double), PropertyType.Double }, 
                    { typeof(decimal), PropertyType.Double }, 
                    { typeof(bool), PropertyType.Bool }, 
                    { typeof(DateTime), PropertyType.Date }, 
                    { typeof(Guid), PropertyType.Guid },
                    { typeof(EntityId), PropertyType.Guid },
                    { typeof(byte[]), PropertyType.Binary }
                };

        /// <summary>Initializes a new instance of the <see cref="PropertyValue"/> class.</summary>
        /// <param name="type">The type as PropertyType.</param>
        /// <param name="serializedValue">The string serialized value.</param>
        public PropertyValue(PropertyType type, string serializedValue)
            : this(StringFromPropertyType(type), serializedValue)
        {
        }

        /// <summary>Initializes a new instance of the <see cref="PropertyValue"/> class.</summary>
        /// <param name="serializedType">The string serialized type.</param>
        /// <param name="serializedValue">The string serialized value.</param>
        public PropertyValue(string serializedType, string serializedValue)
        {
            this.Initialize(serializedType, serializedValue);
        }

        /// <summary>Initializes a new instance of the <see cref="PropertyValue"/> class.</summary>
        /// <param name="type">The type.</param>
        /// <param name="value">The value.</param>
        public PropertyValue(PropertyType type, dynamic value)
        {
            this.Initialize(type, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyValue"/> class from an existing PropertyValue.
        /// </summary>
        /// <param name="value">An existing PropertyValue.</param>
        public PropertyValue(PropertyValue value)
        {
            this.Initialize(value.DynamicType, value.DynamicValue);
        }

        /// <summary>
        /// Gets or sets SerializationType of the property as a string. This is used when (de)serializing
        /// the property for persistence or as a DataContract.
        /// </summary>
        [DataMember(Order = 0)]
        public string SerializationType
        {
            get
            {
                // Derived from underlying PropertyType
                return StringFromPropertyType(this.DynamicType);
            }

            protected set
            {
                // Initialize the underlying PropertyType
                this.DynamicType = PropertyTypeFromString(value);
            }
        }

        /// <summary>
        /// Gets or sets value of the property as a string. This is used when (de)serializing
        /// the property for persistence or as a DataContract.
        /// </summary>
        [DataMember(Order = 1)]
        public string SerializationValue
        {
            get
            {
                // Derived from underlying PropertyType and dynamic value
                return ValueToString(this.DynamicType, this.DynamicValue);
            }

            protected set
            {
                // Intialize the underlying dynamic value
                this.DynamicValue = StringToValue(this.DynamicType, value);
            }
        }

        /// <summary>
        /// Gets the type of the property as a PropertyType object.
        /// </summary>
        public PropertyType DynamicType { get; private set; }

        /// <summary>
        /// Gets or sets dynamic value of the property.
        /// </summary>
        public dynamic DynamicValue { get; set; }

        ////
        // Begin Operator overloads
        ////

        /// <summary>Check PropertyValue for null and extract value.</summary>
        /// <typeparam name="T">The requested return type.</typeparam>
        /// <param name="propertyValue">The property value to check.</param>
        /// <returns>A default value or the extracted value.</returns>
        public static T CheckAndGetValueAs<T>(PropertyValue propertyValue)
        {
            if (propertyValue != null && propertyValue.DynamicValue != null)
            {
                return propertyValue.GetValueAs<T>();
            }

            // String is the only type for which we will accomodate a null PropertyValue.
            if (typeof(T) == typeof(string))
            {
                return default(T);
            }

            throw new ArgumentException(
                "Null value not supported for requested type {0}.".FormatInvariant(typeof(T).FullName));
        }

        /// <summary>
        /// Factory method to build a PropertyValue of the appropriate type
        /// from a generically specified value.
        /// </summary>
        /// <param name="value">The value of the property.</param>
        /// <typeparam name="T">The generically specified type of the property.</typeparam>
        /// <returns>A PropertyValue if success, otherwise null.</returns>
        public static PropertyValue BuildPropertyValue<T>(T value)
        {
            if (!AllowedTypesMap.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException("Type cannot be mapped to PropertyValue: {0}"
                    .FormatInvariant(typeof(T).FullName));
            }

            return new PropertyValue(AllowedTypesMap[typeof(T)], value);
        }

        /// <summary>Copy construct from native string type.</summary>
        /// <param name="value">The value as native string type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(string value)
        {
            return new PropertyValue(PropertyType.String, value);
        }

        /// <summary>Cast value to native string type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native string.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator string(PropertyValue property)
        {
            // Cast to string should fail if this is not natively string.
            // This may seem odd but we have the SerializationValue Property and ToString
            // which will work to get a string representation regardless of
            // the native type. The purpose of the cast operators is
            // to extract the value as the 'native' type that corresponds
            // to the type specified on the object - not do a conversion.
            return CheckAndGetValueAs<string>(property);
        }

        /// <summary>Copy construct from native int type.</summary>
        /// <param name="value">The value as native int type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(int value)
        {
            return new PropertyValue(PropertyType.Int32, value);
        }

        /// <summary>Cast value to native int type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native int.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator int(PropertyValue property)
        {
            return CheckAndGetValueAs<int>(property);
        }

        /// <summary>Copy construct from native long type.</summary>
        /// <param name="value">The value as native long type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(long value)
        {
            return new PropertyValue(PropertyType.Int64, value);
        }

        /// <summary>Cast value to native long type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native long.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator long(PropertyValue property)
        {
            return CheckAndGetValueAs<long>(property);
        }

        /// <summary>Copy construct from native double type.</summary>
        /// <param name="value">The value as native double type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(double value)
        {
            return new PropertyValue(PropertyType.Double, value);
        }

        /// <summary>Cast value to native double type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native double.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator double(PropertyValue property)
        {
            return CheckAndGetValueAs<double>(property);
        }

        /// <summary>
        /// Copy construct from native decimal type (will result in a PropertyType.Double).
        /// Note that values larger than about 792281625142643 will not round trip without
        /// rounding errors.
        /// </summary>
        /// <param name="value">The value as native decimal type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(decimal value)
        {
            // This cannot overflow going this direction.
            var valueAsDouble = Convert.ToDouble(value);
            return new PropertyValue(PropertyType.Double, valueAsDouble);
        }

        /// <summary>
        /// Cast value to native decimal type.
        /// Note that values larger than about 792281625142643 will not round trip with
        /// rounding errors.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native decimal.</returns>
        /// <exception cref="ArgumentException">If requested type is not compatible with defined type.</exception>
        /// <exception cref="OverflowException">If requested type will not hold defined type.</exception>
        public static implicit operator decimal(PropertyValue property)
        {
            return CheckAndGetValueAs<decimal>(property);
        }

        /// <summary>Copy construct from native DateTime type.</summary>
        /// <param name="value">The value as native DateTime type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(DateTime value)
        {
            return new PropertyValue(PropertyType.Date, value);
        }

        /// <summary>Cast value to native DateTime type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native DateTime.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator DateTime(PropertyValue property)
        {
            return CheckAndGetValueAs<DateTime>(property);
        }

        /// <summary>Copy construct from native bool type.</summary>
        /// <param name="value">The value as native bool type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(bool value)
        {
            return new PropertyValue(PropertyType.Bool, value);
        }

        /// <summary>Cast value to native bool type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native bool.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator bool(PropertyValue property)
        {
            return CheckAndGetValueAs<bool>(property);
        }
        
        /// <summary>Copy construct from native Guid type.</summary>
        /// <param name="value">The value as native Guid type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(Guid value)
        {
            return new PropertyValue(PropertyType.Guid, value);
        }

        /// <summary>Cast value to EntityId type. At present this is mapped to a PropertyType of Guid.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as EntityId.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator EntityId(PropertyValue property)
        {
            return CheckAndGetValueAs<EntityId>(property);
        }

        /// <summary>Copy construct from EntityId type. At present this is mapped to a PropertyType of Guid.</summary>
        /// <param name="value">The value as EntityId.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(EntityId value)
        {
            return new PropertyValue(PropertyType.Guid, value);
        }

        /// <summary>Cast value to native Guid type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native Guid.</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator Guid(PropertyValue property)
        {
            return CheckAndGetValueAs<Guid>(property);
        }

        /// <summary>Copy construct from native byte[] type.</summary>
        /// <param name="value">The value as native byte[] type.</param>
        /// <returns>A PropertyValue.</returns>
        public static implicit operator PropertyValue(byte[] value)
        {
            return new PropertyValue(PropertyType.Binary, value);
        }

        /// <summary>Cast value to native byte[] type.</summary>
        /// <param name="property">The property.</param>
        /// <returns>The property value as native byte[].</returns>
        /// <exception cref="ArgumentException">If requested type does not match defined type.</exception>
        public static implicit operator byte[](PropertyValue property)
        {
            return CheckAndGetValueAs<byte[]>(property);
        }

        /// <summary>Equality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(PropertyValue left, PropertyValue right)
        {
            return Equals(left, right);
        }

        /// <summary>Inequality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>False if equal.</returns>
        public static bool operator !=(PropertyValue left, PropertyValue right)
        {
            return !Equals(left, right);
        }

        ////
        // End Operator overloads
        ////

        ////
        // Begin Equality overloads
        // Two PropertyValue objects are considered equal if they have the same value
        // and type. Name is only used as a key and is not considered for equality.
        ////

        /// <summary>Equals method override.</summary>
        /// <param name="other">The other PropertyValue object being equated.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(PropertyValue other)
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

            // Test based on value equality only (not type)
            // There does not seem to be a useful scenario to test equality based
            // on type that would not be done explicity.

            // Binary is a little different - use serialization value since comparison is easier
            if (this.DynamicType == PropertyType.Binary)
            {
                return Equals(other.SerializationValue, this.SerializationValue);
            }

            return Equals(other.DynamicValue, this.DynamicValue);
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

            if (obj.GetType() != typeof(PropertyValue))
            {
                return false;
            }

            // Defer to typed Equals
            return this.Equals((PropertyValue)obj);
        }

        /// <summary>Hash function for PropertyValue type.</summary>
        /// <returns>A hash code based on the member values.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // (* 397 is just an arbitrary distribution function)
                return (this.DynamicType.GetHashCode() * 397) ^ (this.DynamicValue != null ? this.DynamicValue.GetHashCode() : 0);
            }
        }

        ////
        // End Equality overloads
        ////

        /// <summary>ToString override.</summary>
        /// <returns>String serialized representation of value.</returns>
        public override string ToString()
        {
            return this.SerializationValue;
        }
        
        /// <summary>Get the value as type T.</summary>
        /// <typeparam name="T">The type to return if possible.</typeparam>
        /// <returns>The value as T.</returns>
        public T GetValueAs<T>()
        {
            if (this.DynamicValue == null)
            {
                if (default(T) == null)
                {
                    return default(T);
                }

                throw new ArgumentException("Cannot return null for non-nullable requested type.");
            }

            // Nothing to do
            if (typeof(T) == this.DynamicValue.GetType())
            {
                return this.DynamicValue;
            }

            if (AllowedTypesMap.ContainsKey(typeof(T)))
            {
                var targetPropertyType = AllowedTypesMap[typeof(T)];

                // If it's a numeric type there are some allowed coercions
                if (targetPropertyType == PropertyType.Int32
                    || targetPropertyType == PropertyType.Int64 
                    || targetPropertyType == PropertyType.Double)
                {
                    return this.CoerceNumericValue<T>();
                }

                // If it's not numeric the generic parameter must map to the supported PropertyType
                if (PropertyTypeFromString(this.SerializationType) == targetPropertyType)
                {
                    return this.DynamicValue;
                }
            }

            throw new ArgumentException("Could not coerce {0} to {1}."
                .FormatInvariant(this.SerializationType, typeof(T).FullName));
        }

        /// <summary>Get string serialized from of a PropertyType.</summary>
        /// <param name="type">The type as PropertyType.</param>
        /// <returns>The property type as a string.</returns>
        private static string StringFromPropertyType(PropertyType type)
        {
            return Enum.GetName(typeof(PropertyType), type);
        }

        /// <summary>Get PropertyType from a string serialized form.</summary>
        /// <param name="type">The type as string.</param>
        /// <returns>The property Type as PropertyType</returns>
        /// <exception cref="ArgumentException">Throws if the string does not map to one of our PropertyType's.</exception>
        private static PropertyType PropertyTypeFromString(string type)
        {
            PropertyType outType;
            if (!Enum.TryParse(type, out outType))
            {
                throw new ArgumentException("Property type not recognized: {0}"
                    .FormatInvariant(type));
            }

            return outType;
        }

        /// <summary>Helper to convert string serialized values to native types.</summary>
        /// <param name="type">The string form of the target type.</param>
        /// <param name="value">The string serialized value to convert.</param>
        /// <returns>A dynamic value representing a native type.</returns>
        private static dynamic StringToValue(PropertyType type, string value)
        {
            return ToValueMap[type](value);
        }

        /// <summary>Helper to convert native types to string serialized values.</summary>
        /// <param name="type">The target PropertyType.</param>
        /// <param name="value">A dynamic value representing a native type.</param>
        /// <returns>A string serialized representation of the native type.</returns>
        private static string ValueToString(PropertyType type, dynamic value)
        {
            return ToStringMap[type](value);
        }

        /// <summary>Test to determine if a double can be treated as integral type.</summary>
        /// <param name="value">The double value.</param>
        /// <returns>True if integral.</returns>
        private static bool IsIntegral(double value)
        {
            var integerPart = Math.Truncate(Math.Abs(value));
            return (Math.Abs(value) - integerPart) < double.Epsilon;
        }

        /// <summary>Coerce a numeric PropertyValue to a native decimal.</summary>
        /// <returns>A decimal value if successful.</returns>
        /// <exception cref="ArgumentException">If requested type is not compatible with defined type.</exception>
        /// <exception cref="OverflowException">If requested type will not hold defined type.</exception>
        /// <typeparam name="T">Target type of coercion.</typeparam>
        private T CoerceNumericValue<T>()
        {
            try
            {
                var sourceValue = this.DynamicValue;
                var sourceType = this.DynamicType;

                // First check for a string to numeric coercion. Always convert as double since this
                // is the most generous, then coerce to the requested type. This has the possibility
                // of loosing precision on extreme values of long but gains a few more allowed
                // conversions for things like "1.0" -> int
                if (sourceType == PropertyType.String)
                {
                    sourceValue = StringToValue(PropertyType.Double, this.SerializationValue);
                    sourceType = PropertyType.Double;
                }

                // double to decimal
                // int to decimal
                // long to decimal
                if (typeof(T) == typeof(decimal) && (sourceType == PropertyType.Double || sourceType == PropertyType.Int64
                    || sourceType == PropertyType.Int32))
                {
                    return Convert.ToDecimal(sourceValue);
                }

                // double to double
                // int to double
                // long to double
                if (typeof(T) == typeof(double) && (sourceType == PropertyType.Double || sourceType == PropertyType.Int64
                    || sourceType == PropertyType.Int32))
                {
                    return Convert.ToDouble(sourceValue);
                }

                // double to int
                if (typeof(T) == typeof(int) && sourceType == PropertyType.Double)
                {
                    if (IsIntegral(sourceValue))
                    {
                        return Convert.ToInt32(sourceValue);
                    }
                }

                // double to long
                if (typeof(T) == typeof(long) && sourceType == PropertyType.Double)
                {
                    if (IsIntegral(sourceValue))
                    {
                        return Convert.ToInt64(sourceValue);
                    }
                }

                // int to long
                // long to long
                if (typeof(T) == typeof(long) && (sourceType == PropertyType.Int64 || sourceType == PropertyType.Int32))
                {
                    return Convert.ToInt64(sourceValue);
                }

                // int to int
                // long to int
                if (typeof(T) == typeof(int) && (sourceType == PropertyType.Int64 || sourceType == PropertyType.Int32))
                {
                    return Convert.ToInt32(sourceValue);
                }
            }
            catch (OverflowException)
            {
            }

            throw new ArgumentException("Could not coerce {0} to {1}."
                .FormatInvariant(this.SerializationType, typeof(T).FullName));
        }

        /// <summary>Initialize from string serialized type and value.</summary>
        /// <param name="serializedType">The string serialized type.</param>
        /// <param name="serializedValue">The string serialized value.</param>
        private void Initialize(string serializedType, string serializedValue)
        {
            var type = PropertyTypeFromString(serializedType);
            this.Initialize(type, StringToValue(type, serializedValue));
        }

        /// <summary>
        /// Except for default construction (deserialized from the DataContract) all constructors
        /// and intializers will ultimately use this method. 
        /// </summary>
        /// <param name="type">The type as PropertyType.</param>
        /// <param name="value">The value as dynamic value.</param>
        private void Initialize(PropertyType type, dynamic value)
        {
            this.DynamicType = type;
            this.DynamicValue = value;
        }
    }
}
