// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityPropertyFixture.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using DataAccessLayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataAccessLayerUnitTests
{
    /// <summary>Test fixture for EntityProperty.</summary>
    [TestClass]
    public class EntityPropertyFixture
    {
        /// <summary>Test constructors of EntityProperty</summary>
        [TestMethod]
        public void TestConstruction()
        {
            var propertyName = "SomeName";
            var propertyValue = "SomeValue";

            // Default property
            var prop = new EntityProperty(propertyName, propertyValue);
            Assert.AreEqual(propertyValue, (string)prop.Value);
            Assert.AreEqual(PropertyFilter.Default, prop.Filter);
            Assert.IsTrue(prop.IsDefaultProperty);
            Assert.IsFalse(prop.IsSystemProperty);
            Assert.IsFalse(prop.IsExtendedProperty);
            Assert.IsFalse(prop.IsBlobRef);

            // Extended property
            prop = new EntityProperty(propertyName, propertyValue, PropertyFilter.Extended);
            Assert.AreEqual(propertyValue, (string)prop.Value);
            Assert.AreEqual(PropertyFilter.Extended, prop.Filter);
            Assert.IsFalse(prop.IsDefaultProperty);
            Assert.IsFalse(prop.IsSystemProperty);
            Assert.IsTrue(prop.IsExtendedProperty);
            Assert.IsFalse(prop.IsBlobRef);

            // System property
            prop = new EntityProperty(propertyName, propertyValue, PropertyFilter.System);
            Assert.AreEqual(propertyValue, (string)prop.Value);
            Assert.AreEqual(PropertyFilter.System, prop.Filter);
            Assert.IsFalse(prop.IsDefaultProperty);
            Assert.IsTrue(prop.IsSystemProperty);
            Assert.IsFalse(prop.IsExtendedProperty);
            Assert.IsFalse(prop.IsBlobRef);
        }

        /// <summary>Test equality methods are correct.</summary>
        [TestMethod]
        public void Equality()
        {
            var prop = new EntityProperty { Name = "foo", Value = 1 };
            var propCopy = new EntityProperty(prop);
            Assert.IsTrue(prop == propCopy);
            Assert.IsTrue(prop.Equals(propCopy));
            Assert.IsTrue(prop.Equals((object)propCopy));

            var differByValue = new EntityProperty { Name = "foo", Value = 2 };
            Assert.IsFalse(prop == differByValue);
            Assert.IsTrue(prop != differByValue);

            // Do not allow to differ by name
            var differByName = new EntityProperty { Name = "foo1", Value = 1 };
            Assert.IsFalse(prop == differByName);
            Assert.IsTrue(prop != differByName);
        }

        /// <summary>Test metadata flags are preserved on copy.</summary>
        [TestMethod]
        public void MetadataPreservedOnCopy()
        {
            var prop = new EntityProperty
                {
                    Name = "foo", 
                    Value = 1, 
                    IsBlobRef = true, 
                    Filter = PropertyFilter.Extended
                };
            var propCopy = new EntityProperty(prop);

            Assert.AreEqual(prop.IsBlobRef, propCopy.IsBlobRef);
            Assert.AreEqual(prop.IsDefaultProperty, propCopy.IsDefaultProperty);
            Assert.AreEqual(prop.IsSystemProperty, propCopy.IsSystemProperty);
            Assert.AreEqual(prop.IsExtendedProperty, propCopy.IsExtendedProperty);
            Assert.AreEqual(prop.Filter, propCopy.Filter);
        }

        /// <summary>Test type mismatches are being check by underlying PropertyValue.</summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestTypeMismatch()
        {
            var prop = new EntityProperty { Name = "foo", Value = 1 };

            // Should not be able to cast a PropertyType.Int32 to a string
            string value = prop;
        }
        
        /// <summary>For ToString we perform a conversion to the serialized value of the property.</summary>
        [TestMethod]
        public void TestToStringConversion()
        {
            EntityProperty prop1 = new EntityId();
            Assert.AreEqual(prop1.Value.ToString(), prop1.ToString());
            EntityProperty prop2 = 5;
            Assert.AreEqual(prop2.Value.ToString(), prop2.ToString());
        }

        /// <summary>Test we can copy construct from and cast to string</summary>
        [TestMethod]
        public void TestStringCopyAndCast()
        {
            EntityProperty prop = "bar";
            string value = prop;
            Assert.AreEqual((string)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            value = nullProp;
            Assert.IsNull(value);
        }

        /// <summary>Test we can copy construct and cast to Int32</summary>
        [TestMethod]
        public void TestInt32CopyAndCast()
        {
            EntityProperty prop = 1;
            int value = prop;
            Assert.AreEqual((int)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to Int64</summary>
        [TestMethod]
        public void TestInt64CopyAndCast()
        {
            EntityProperty prop = long.MaxValue;
            long value = prop;
            Assert.AreEqual((long)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to Double</summary>
        [TestMethod]
        public void TestDoubleCopyAndCast()
        {
            EntityProperty prop = 1.1;
            double value = prop;
            Assert.AreEqual((double)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);
            
            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to decimal</summary>
        [TestMethod]
        public void TestDecimalCopyAndCast()
        {
            EntityProperty prop = 1.1m;
            decimal value = prop;
            Assert.AreEqual((decimal)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to Bool</summary>
        [TestMethod]
        public void TestBoolCopyAndCast()
        {
            EntityProperty prop = true;
            bool value = prop;
            Assert.AreEqual((bool)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to Date</summary>
        [TestMethod]
        public void TestDateCopyAndCast()
        {
            EntityProperty prop = DateTime.Now;
            DateTime value = prop;
            Assert.AreEqual((DateTime)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to Guid</summary>
        [TestMethod]
        public void TestGuidCopyAndCast()
        {
            EntityProperty prop = Guid.NewGuid();
            Guid value = prop;
            Assert.AreEqual((Guid)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to Binary</summary>
        [TestMethod]
        public void TestBinaryCopyAndCast()
        {
            EntityProperty prop = new[] { (byte)1, (byte)2 };
            byte[] value = prop;
            Assert.AreEqual((byte[])prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Test we can copy construct and cast to EntityId</summary>
        [TestMethod]
        public void TestEntityIdCopyAndCast()
        {
            EntityProperty prop = new EntityId();
            EntityId value = prop;
            Assert.AreEqual((EntityId)prop.Value, value);
            Assert.AreEqual(string.Empty, prop.Name);

            // Handle null property cast
            EntityProperty nullProp = null;
            AssertNullCastToNonNullableFails(() => value = nullProp);
        }

        /// <summary>Support comparison to generic object if it can be converted to a PropertyValue.</summary>
        [TestMethod]
        public void TestGenericObjectEquality()
        {
            Assert.AreEqual(new EntityProperty("foo"), "foo");
            Assert.AreEqual(new EntityProperty(1), 1);
            Assert.AreEqual(new EntityProperty(long.MaxValue), long.MaxValue);
            Assert.AreEqual(new EntityProperty(1.1), 1.1);
            Assert.AreEqual(new EntityProperty(1.1m), 1.1m);
            Assert.AreEqual(new EntityProperty(true), true);
            var date = DateTime.Now;
            Assert.AreEqual(new EntityProperty(date), date);
            Assert.AreEqual(new EntityProperty(new EntityId(1)), new EntityId(1));
            var guid = Guid.NewGuid();
            Assert.AreEqual(new EntityProperty(guid), guid);
            Assert.AreEqual(new EntityProperty(new byte[] { 0x1, 0x2 }), new byte[] { 0x1, 0x2 });
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
    }
}
