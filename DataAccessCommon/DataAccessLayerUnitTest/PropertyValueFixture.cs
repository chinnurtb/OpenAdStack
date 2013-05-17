// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PropertyValueFixture.cs" company="Emerging Media Group">
//   Copyright Emerging Media Group. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Test fixture for the PropertyValue class.</summary>
    [TestClass]
    public class PropertyValueFixture
    {
        /// <summary>Copy constructor</summary>
        [TestMethod]
        public void CopyConstructor()
        {
            PropertyValue prop1 = 1;
            AssertPropertiesEqual(prop1, new PropertyValue(prop1));
        }

        /// <summary>Serialized type serialized value constructor</summary>
        [TestMethod]
        public void SerializedStringSerializedValueConstructor()
        {
            PropertyValue prop1 = 1;
            AssertPropertiesEqual(prop1, new PropertyValue("Int32", "1"));
        }

        /// <summary>PropertyType serialized value constructor</summary>
        [TestMethod]
        public void PropertyTypeSerializedValueConstructor()
        {
            PropertyValue prop1 = 1;
            AssertPropertiesEqual(prop1, new PropertyValue(PropertyType.Int32, "1"));
        }
        
        /// <summary>PropertyType native value constructor</summary>
        [TestMethod]
        public void PropertyTypeNativeValueConstructor()
        {
            PropertyValue prop1 = 1;
            AssertPropertiesEqual(prop1, new PropertyValue(PropertyType.Int32, 1));
        }

        /// <summary>Test equality members.</summary>
        [TestMethod]
        public void Equality()
        {
            // Different values should not match
            PropertyValue prop1 = 1;
            PropertyValue prop2 = 2;
            Assert.IsFalse(prop1 == prop2);
            Assert.IsTrue(prop1 != prop2);

            // Different types are allowed to match
            var prop3 = new PropertyValue(PropertyType.Int32, 1);
            var prop4 = new PropertyValue(PropertyType.Int64, 1);
            Assert.IsTrue(prop3 == prop4);
            Assert.IsFalse(prop3 != prop4);

            // Same value/type for all equality operators
            Assert.IsTrue(prop1 == prop3);
            Assert.IsFalse(prop1 != prop3);
            Assert.IsTrue(prop1.Equals(prop3));
            Assert.IsTrue(prop1.Equals((object)prop3));
        }

        /// <summary>Test that we throw if we try to construct with an invalid type of property value.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ConstructFromNameValueStringsInvalidType()
        {
            var type = "invalidtype";
            var value = 0x0fffffff.ToString(CultureInfo.InvariantCulture);
            new PropertyValue(type, value);
        }

        /// <summary>Test we can coerce a null to a string succeeds.</summary>
        [TestMethod]
        public void CoerceNullPropertyToStringSucceeds()
        {
            // In practice a null string is the only type of PropertyValue that
            // can behavior rationally for all operations.
            PropertyValue propValue = null;
            var castToString = (string)propValue;
            Assert.IsNull(castToString);

            propValue = new PropertyValue(PropertyType.String, (string)null);
            castToString = (string)propValue;
        }

        /// <summary>Test coercing a null property to a non-string fails.</summary>
        [TestMethod]
        public void CoerceNullPropertyToNonStringFails()
        {
            PropertyValue nullProp = null;
            int castInt;
            AssertNullCastToNonNullableFails(() => castInt = (int)nullProp);

            EntityId castId;
            AssertNullCastToNonNullableFails(() => castId = (EntityId)nullProp);

            byte[] castBinary;
            AssertNullCastToNonNullableFails(() => castBinary = (byte[])nullProp);
            var binaryProp = new PropertyValue(PropertyType.Binary, (byte[])null);
            AssertNullCastToNonNullableFails(() => castBinary = (byte[])binaryProp);
        }

        /// <summary>Test that we throw if we try to cast to a type that is not the original</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StringPropertyInvalidCast()
        {
            // start with a string type
            PropertyValue propValue = "foo";

            // try to cast it to int
            var cast = (int)propValue;
        }

        /// <summary>Test PropertyValue constructors for a native string type.</summary>
        [TestMethod]
        public void StringProperty()
        {
            string serializedvalue = "foo";
            string value = serializedvalue;
            var type = Enum.GetName(typeof(PropertyType), PropertyType.String);

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.String, serializedvalue, value, propFromValue, (string)propFromValue);
            var propFromTypeStringPair = new PropertyValue(PropertyType.String, serializedvalue);
            AssertPropertyValue(PropertyType.String, serializedvalue, value, propFromTypeStringPair, (string)propFromTypeStringPair);
            var propFromStringStringPair = new PropertyValue(type, serializedvalue);
            AssertPropertyValue(PropertyType.String, serializedvalue, value, propFromStringStringPair, (string)propFromStringStringPair);
        }

        /// <summary>Test that we can coerce numeric as String to numeric types on cast</summary>
        [TestMethod]
        public void CoerceStringToNumeric()
        {
            // Int as string
            PropertyValue propValue = "1";
            Assert.AreEqual(1, (int)propValue);
            Assert.AreEqual(1L, (long)propValue);
            Assert.AreEqual(1, (double)propValue);
            Assert.AreEqual(1m, (decimal)propValue);

            // Large Long as string looses precision
            propValue = "922337203685477580";
            Assert.AreEqual(922337203685477632, (long)propValue);
            Assert.AreEqual(922337203685477632, (double)propValue);
            Assert.AreEqual(922337203685478000m, (decimal)propValue);

            // Long as string
            propValue = "3147483647"; // bigger than int
            Assert.AreEqual(3147483647L, (long)propValue);
            Assert.AreEqual(3147483647, (double)propValue);
            Assert.AreEqual(3147483647m, (decimal)propValue);

            // Double as string
            propValue = "1.0E+19";
            Assert.AreEqual(1.0E+19, (double)propValue);
            Assert.AreEqual(1.0E+19m, (decimal)propValue);

            // Double with no fraction
            propValue = "1.0";
            Assert.AreEqual(1, (int)propValue);
            Assert.AreEqual(1, (long)propValue);
            Assert.AreEqual(1.0, (double)propValue);
            Assert.AreEqual(1.0m, (decimal)propValue);
        }

        /// <summary>String to out of range int fails</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceStringToOutOfRangeIntFails()
        {
            PropertyValue propValue = "3147483647"; // bigger than int
            Assert.AreEqual(1, (int)propValue);
        }

        /// <summary>String to out of range long fails</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceStringToOutOfRangeLongFails()
        {
            PropertyValue propValue = "9223372036854775808";
            var cast = (long)propValue;
        }
        
        /// <summary>String with fraction to integral fails</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceStringWithFractionToIntegralFails()
        {
            PropertyValue propValue = "1.1";
            var cast = (int)propValue;
        }

        /// <summary>String to out of range decimal fails</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceStringToOutOfRangeDecimalFails()
        {
            PropertyValue propValue = "1.0E+308";
            var cast = (decimal)propValue;
        }

        /// <summary>String to out of range double fails</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceStringToOutOfRangeDoubleFails()
        {
            PropertyValue propValue = "1.8E+308";
            var cast = (double)propValue;
        }

        /// <summary>Test that we throw if we try to construct with a value that is not a valid int.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Int32PropertyInvalidValue()
        {
            var type = Enum.GetName(typeof(PropertyType), PropertyType.Int32);

            // Out of range for int
            var value = 0xffffffffffff.ToString(CultureInfo.InvariantCulture);
            
            new PropertyValue(type, value);
        }

        /// <summary>Test that we can coerce Int32 to other numeric types on cast</summary>
        [TestMethod]
        public void CoerceInt32()
        {
            // start with an int
            PropertyValue propValue = 1;
            Assert.AreEqual(1L, (long)propValue);
            Assert.AreEqual(1, (double)propValue);
            Assert.AreEqual(1m, (decimal)propValue);

            // max int
            propValue = int.MaxValue;
            Assert.AreEqual(2147483647L, (long)propValue);
            Assert.AreEqual(2147483647, (double)propValue);
            Assert.AreEqual(2147483647m, (decimal)propValue);

            // min int
            propValue = int.MinValue;
            Assert.AreEqual(-2147483648L, (long)propValue);
            Assert.AreEqual(-2147483648, (double)propValue);
            Assert.AreEqual(-2147483648m, (decimal)propValue);
        }

        /// <summary>Test PropertyValue constructors for a max value native int type.</summary>
        [TestMethod]
        public void MaxInt32Property()
        {
            var value = int.MaxValue;
            string serializedValue = "2147483647";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Int32, serializedValue, value, propFromValue, (int)propFromValue);
        }

        /// <summary>Test PropertyValue constructors for a min value native int type.</summary>
        [TestMethod]
        public void MinInt32Property()
        {
            var value = int.MinValue;
            string serializedValue = "-2147483648";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Int32, serializedValue, value, propFromValue, (int)propFromValue);
        }

        /// <summary>Test that we throw if we try to construct with a value that is not a valid long.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Int64PropertyInvalidValue()
        {
            var type = Enum.GetName(typeof(PropertyType), PropertyType.Int64);

            // Not a number
            var value = "abc";

            new PropertyValue(type, value);
        }

        /// <summary>Test that we can coerce Int64 to other numeric types in range</summary>
        [TestMethod]
        public void CoerceInt64()
        {
            // start with a long
            PropertyValue propValue = 1L;

            Assert.AreEqual(1, (int)propValue);
            Assert.AreEqual(1, (double)propValue);
            Assert.AreEqual(1m, (decimal)propValue);

            // max long
            propValue = long.MaxValue;
            Assert.AreEqual(9223372036854775807, (double)propValue);
            Assert.AreEqual(9223372036854775807m, (decimal)propValue);

            // min long
            propValue = long.MinValue;
            Assert.AreEqual(-9223372036854775808, (double)propValue);
            Assert.AreEqual(-9223372036854775808m, (decimal)propValue);
        }

        /// <summary>Test that we fail if we coerce Int64 to other numeric types out of range</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceInt64ToOutOfRangeInt32Fails()
        {
            PropertyValue propValue = long.MaxValue;
            Assert.AreEqual(9223372036854775807L, (int)propValue);
        }

        /// <summary>Test PropertyValue constructors for a max value native long type.</summary>
        [TestMethod]
        public void MaxInt64Property()
        {
            var value = long.MaxValue;
            string serializedValue = "9223372036854775807";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Int64, serializedValue, value, propFromValue, (long)propFromValue);
        }

        /// <summary>Test PropertyValue constructors for a min value native long type.</summary>
        [TestMethod]
        public void MinInt64Property()
        {
            var value = long.MinValue;
            string serializedValue = "-9223372036854775808";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Int64, serializedValue, value, propFromValue, (long)propFromValue);
        }

        /// <summary>Test that we throw if we try to construct with a value that is not a valid double.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DoublePropertyInvalidValue()
        {
            var type = Enum.GetName(typeof(PropertyType), PropertyType.Double);

            // Not a number
            var value = "abc";

            new PropertyValue(type, value);
        }

        /// <summary>Test that we can coerce double to other numeric types in range</summary>
        [TestMethod]
        public void CoerceDouble()
        {
            // start with a double
            PropertyValue propValue = 1.0;

            Assert.AreEqual(1, (int)propValue);
            Assert.AreEqual(1L, (long)propValue);
            Assert.AreEqual(1m, (decimal)propValue);
        }

        /// <summary>Test that we fail if we coerce double to other numeric types out of range</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceDoubleToOutOfRangeInt32Fails()
        {
            PropertyValue propValue = 2147483650.0;
            var cast = (int)propValue;
        }

        /// <summary>Test that we fail if we coerce double to other numeric types out of range</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceDoubleWithFractionToInt32Fails()
        {
            PropertyValue propValue = 1.1;
            var cast = (int)propValue;
        }

        /// <summary>Test that we fail if we coerce double to other numeric types out of range</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceDoubleToOutOfRangeInt64Fails()
        {
            PropertyValue propValue = 1.0E+19;
            var cast = (long)propValue;
        }

        /// <summary>Test that we fail if we coerce double to other numeric types out of range</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void CoerceDoubleToOutOfRangeDecimalFails()
        {
            PropertyValue propValue = 1.0E+308;
            var cast = (decimal)propValue;
        }

        /// <summary>Test PropertyValue constructors for a max value native double type.</summary>
        [TestMethod]
        public void MaxDoubleProperty()
        {
            double value = double.MaxValue;
            
            // We want this to serialize to a valid round-trip value which is achieved with the 'R' format specifier
            string serializedValue = "1.7976931348623157E+308";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Double, serializedValue, value, propFromValue, (double)propFromValue);
        }

        /// <summary>Test PropertyValue constructors for a min value native double type.</summary>
        [TestMethod]
        public void MinDoubleProperty()
        {
            double value = double.MinValue;

            // We want this to serialize to a valid round-trip value which is achieved with the 'R' format specifier
            string serializedValue = "-1.7976931348623157E+308";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Double, serializedValue, value, propFromValue, (double)propFromValue);
        }

        /// <summary>Test that we will construct PropertyType.Double from native decimal</summary>
        [TestMethod]
        public void DecimalPropertyCastToDouble()
        {
            // construct property from decimal
            PropertyValue propValue = 1.11m;

            Assert.AreEqual(PropertyType.Double, propValue.DynamicType);
        }

        /// <summary>Test that we will attempt to convert Double, Int32 and Int64 to native decimal</summary>
        [TestMethod]
        public void CastToDecimal()
        {
            PropertyValue doubleProp = 1.11;
            PropertyValue intProp = 1;
            PropertyValue longProp = (long)2;

            // cast to a decimal
            decimal cast = doubleProp;
            Assert.AreEqual(1.11m, cast);
            cast = intProp;
            Assert.AreEqual(1m, cast);
            cast = longProp;
            Assert.AreEqual(2m, cast);
        }

        /// <summary>Test PropertyValue constructors large decimal.</summary>
        [TestMethod]
        public void DoublePropertyFromLargeDecimal()
        {
            // Very large decimals will have rounding issues if we need round-tripping. NOTE: This is about the max.
            decimal value = 792281625142643m;
            string serializedValue = "792281625142643";
            PropertyValue propFromValue = value;
            Assert.AreEqual(value, (decimal)propFromValue);
            Assert.AreEqual(serializedValue, propFromValue.SerializationValue);
        }

        /// <summary>Test PropertyValue constructors large decimal.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SerializingVeryLargeDecimalsFails()
        {
            // Very large decimals will have rounding issues when they are converted back from decimal
            // that can can cause overflow exceptions.
            decimal value = decimal.MaxValue;
            PropertyValue propFromValue = value;
            Assert.AreNotEqual(value, (decimal)propFromValue);
        }

        /// <summary>Test that we throw if we try to construct with a value that is not a valid DateTime.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DateTimePropertyInvalidValue()
        {
            var type = Enum.GetName(typeof(PropertyType), PropertyType.Date);

            // Not a date
            var value = "abc";

            new PropertyValue(type, value);
        }

        /// <summary>Test that we throw if we try to cast to a type that is not the original</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DateTimePropertyInvalidCast()
        {
            // start with a DataTime
            PropertyValue propValue = DateTime.Now;

            // cast to an int
            var cast = (int)propValue;
        }

        /// <summary>Test PropertyValue constructors for a max value native DateTime type.</summary>
        [TestMethod]
        public void MaxDateTimeProperty()
        {
            DateTime value = DateTime.MaxValue.ToUniversalTime();
            string serializedValue = "9999-12-31T23:59:59.9999999Z";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Date, serializedValue, value, propFromValue, (DateTime)propFromValue);
        }

        /// <summary>Test PropertyValue constructors for a min value native DateTime type.</summary>
        [TestMethod]
        public void MinDateTimeProperty()
        {
            DateTime value = DateTime.MinValue.ToUniversalTime();
            string serializedValue = "0001-01-01T08:00:00.0000000Z";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Date, serializedValue, value, propFromValue, (DateTime)propFromValue);
        }

        /// <summary>Serialization for local time and local converted to UTC should be same.</summary>
        [TestMethod]
        public void LocalConvertedToUtcDateTimeProperty()
        {
            var localValue = DateTime.Now;
            PropertyValue local = localValue;
            PropertyValue utc = localValue.ToUniversalTime();
            Assert.AreEqual(local.SerializationValue, utc.SerializationValue);
        }

        /// <summary>Test that we throw if we try to construct with a value that is not a valid bool.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BoolPropertyInvalidValue()
        {
            var type = Enum.GetName(typeof(PropertyType), PropertyType.Bool);

            // Not a bool
            var value = "abc";

            new PropertyValue(type, value);
        }

        /// <summary>Test that we throw if we try to cast to a type that is not the original</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BoolPropertyInvalidCast()
        {
            // Start with a bool
            PropertyValue propValue = true;

            // cast to an int
            var cast = (int)propValue;
        }

        /// <summary>Test PropertyValue constructors for a native bool type.</summary>
        [TestMethod]
        public void BoolProperty()
        {
            bool value = true;
            string serializedValue = "true";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Bool, serializedValue, value, propFromValue, (bool)propFromValue);

            var prop = new PropertyValue("Bool", "TRUE");
            Assert.IsTrue(prop.DynamicValue);
            prop = new PropertyValue("Bool", "false");
            Assert.IsFalse(prop.DynamicValue);
            prop = new PropertyValue("Bool", "False");
            Assert.IsFalse(prop.DynamicValue);
        }
        
        /// <summary>Test that we throw if we try to construct with a value that is not a valid Guid.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GuidPropertyInvalidValue()
        {
            var type = Enum.GetName(typeof(PropertyType), PropertyType.Guid);

            // Not a guid
            var value = "abc";

            new PropertyValue(type, value);
        }

        /// <summary>Test that we throw if we try to cast to a type that is not the original</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void GuidPropertyInvalidCast()
        {
            // start with a DataTime
            PropertyValue propValue = Guid.NewGuid();

            // cast to an int
            var cast = (int)propValue;
        }

        /// <summary>Test PropertyValue constructors for a native Guid type.</summary>
        [TestMethod]
        public void GuidProperty()
        {
            var value = new Guid("11111111111111111111111111111111");
            string serializedValue = "11111111111111111111111111111111";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Guid, serializedValue, value, propFromValue, (Guid)propFromValue);
        }
        
        /// <summary>Test PropertyValue constructors for a EntityId type (treated as PropertyType.Guid).</summary>
        [TestMethod]
        public void EntityIdProperty()
        {
            var value = new EntityId("11111111111111111111111111111111");
            string serializedValue = "11111111111111111111111111111111";

            PropertyValue propFromValue = value;
            AssertPropertyValue<EntityId>(PropertyType.Guid, serializedValue, value, propFromValue, propFromValue);
        }

        /// <summary>Test that we throw if we try to cast to a type that is not the original</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BinaryPropertyInvalidCast()
        {
            // start with a binary value
            PropertyValue propValue = new byte[] { 0x0 };

            // cast to an int
            var cast = (int)propValue;
        }

        /// <summary>Test that we throw if we try to deserialize invalid base64 encoded string.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BinaryPropertyInvalidEncoding()
        {
            // Has invalid characters for base64 encoding 
            var propValue = new PropertyValue(PropertyType.Binary, "123~");
        }

        /// <summary>Test PropertyValue constructors for a native binary type.</summary>
        [TestMethod]
        public void BinaryProperty()
        {
            var value = new byte[] { 0x0, 0x1, 0xF, 0xFF };
            string serializedValue = "AAEP/w==";

            PropertyValue propFromValue = value;
            AssertPropertyValue(PropertyType.Binary, serializedValue, value, propFromValue, (byte[])propFromValue);
        }

        /// <summary>Assert that an operation throws an exception.</summary>
        /// <param name="castOperation">The operation expected to throw.</param>
        private static void AssertNullCastToNonNullableFails(Action castOperation)
        {
            try
            {
                castOperation();
                Assert.Fail();
            }
            catch (ArgumentException)
            {
            }
        }

        /// <summary>Assert two PropertyValues are equal.</summary>
        /// <param name="propCopy">The copy of the property.</param>
        /// <param name="prop">The property.</param>
        private static void AssertPropertiesEqual(PropertyValue propCopy, PropertyValue prop)
        {
            Assert.AreEqual(prop.DynamicValue, propCopy.DynamicValue);
            Assert.AreEqual(prop, propCopy);
        }

        /// <summary>Helper method to assert that all aspects of a PropertyValue are correct for a given underlying generic type T</summary>
        /// <param name="type">The explicity type of the property.</param>
        /// <param name="serializedValue">The serialized string form of the value.</param>
        /// <param name="value">The value as a native type.</param>
        /// <param name="prop">A PropertyValue constructed from value</param>
        /// <param name="valueCast">The value as a native type resulting from casting prop back to the native type.</param>
        /// <typeparam name="T">Underlying native type of property.</typeparam>
        private static void AssertPropertyValue<T>(PropertyType type, string serializedValue, T value, PropertyValue prop, T valueCast)
        {
            // value from casting PropertyValue to type T is equal to value       
            Assert.AreEqual(value, valueCast);

            // value from casting dynamic property to type T is equal to value
            Assert.AreEqual(value, (T)prop.DynamicValue);
            
            // string value is equal to serialized value
            Assert.AreEqual(serializedValue, prop.SerializationValue);
            Assert.AreEqual(serializedValue, prop.ToString());
            
            // PropertyValue type is correct
            Assert.AreEqual(type, prop.DynamicType);
            Assert.AreEqual(type, Enum.Parse(typeof(PropertyType), prop.SerializationType));

            // Equality
            PropertyValue propA = prop;
            Assert.AreEqual(prop, propA);

            // Roundtrip-serialized
            var deserializedProp = RoundTripSerialize(prop);
            Assert.AreEqual(prop.SerializationValue, deserializedProp.SerializationValue);
            Assert.AreEqual(prop.SerializationType, deserializedProp.SerializationType);
        }

        /// <summary>Round-trip serialize a PropertyValue</summary>
        /// <param name="prop">The PropertyValue.</param>
        /// <returns>A new PropertyValue that is the result of DataContractSerialize operation</returns>
        private static PropertyValue RoundTripSerialize(PropertyValue prop)
        {
            var sb = new StringBuilder();

            var writer = XmlWriter.Create(sb);
            var ser = new DataContractSerializer(typeof(PropertyValue));
            ser.WriteObject(writer, prop);
            writer.Close();

            var reader = XmlReader.Create(new StringReader(sb.ToString()));
            var deser = new DataContractSerializer(typeof(PropertyValue));
            var reflatedProp = (PropertyValue)deser.ReadObject(reader);
            reader.Close();
            return reflatedProp;
        }
    }
}
