// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasureSetTest.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DynamicAllocation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MeasuresUnitTests
{
    /// <summary>
    /// tests for the MeasureSet class. Currently tests the Equality and CompareTo implementations.
    /// </summary>
    [TestClass]
    public class MeasureSetTest
    {
        /// <summary>Test that equality operators are correct.</summary>
        [TestMethod]
        public void Equality()
        {
            var measureSet = new MeasureSet { 1 };
            var measureSetDuplicate = new MeasureSet { 1 };
            Assert.IsTrue(measureSet == measureSetDuplicate);
            Assert.IsTrue(measureSet.Equals(measureSetDuplicate));
            Assert.IsTrue(measureSet.Equals((object)measureSetDuplicate));

            var differentMeasure = new MeasureSet { 2 };
            Assert.IsFalse(measureSet == differentMeasure);
            Assert.IsTrue(measureSet != differentMeasure);

            measureSet = new MeasureSet { 1, 2 };
            measureSetDuplicate = new MeasureSet { 2, 1 };
            Assert.IsTrue(measureSet == measureSetDuplicate);
            Assert.IsTrue(measureSet.Equals(measureSetDuplicate));
            Assert.IsTrue(measureSet.Equals((object)measureSetDuplicate));

            var differByOneOfTwoMembers = new MeasureSet { 2, 3 };
            Assert.IsFalse(measureSet == differByOneOfTwoMembers);
            Assert.IsTrue(measureSet != differByOneOfTwoMembers);

            var differByHavingAnExtraMember = new MeasureSet { 1, 2, 3 };
            Assert.IsFalse(measureSet == differByHavingAnExtraMember);
            Assert.IsTrue(measureSet != differByHavingAnExtraMember);
        }
 
        /// <summary>
        /// test for the CompareTo method
        /// </summary>
        [TestMethod]
        public void TestCompareTo()
        {
                var excelate1 = new MeasureSet { 1 };
                var excelate1Alt = new MeasureSet { 1 };
                var excelate2 = new MeasureSet { 2 };
             
                // compare sets with only one member
                Assert.IsTrue(((IComparable)excelate1).CompareTo(excelate2) < 0);
                Assert.IsTrue(((IComparable)excelate2).CompareTo(excelate1) > 0);
                Assert.IsTrue(((IComparable)excelate1).CompareTo(excelate1Alt) == 0);
             
                var excelate12 = new MeasureSet { 1, 2 };
                var excelate21 = new MeasureSet { 2, 1 };
            
                // order shouldn't matter
                Assert.IsTrue(((IComparable)excelate12).CompareTo(excelate21) == 0);

                var excelate123 = new MeasureSet { 1, 2, 3 };
             
                // sets with more elements should come later, if the contained elements are first and the same.
                Assert.IsTrue(((IComparable)excelate123).CompareTo(excelate12) > 0);
                Assert.IsTrue(((IComparable)excelate12).CompareTo(excelate123) < 0);

                var excelate23 = new MeasureSet { 2, 3 };
              
                // lexicographical order, ie '23' should come after '123' 
                Assert.IsTrue(((IComparable)excelate23).CompareTo(excelate123) > 0);
        }

        /// <summary>
        /// test using measure sets as keys to dictionaries
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestUsingAsKeyToDictionary()
        {
            var measureSet = new MeasureSet { 1, 2 };
            var measureSetDuplicate = new MeasureSet { 2, 1 };

            var dictionary = new Dictionary<MeasureSet, decimal>();
            dictionary.Add(measureSet, 1);

            // this should throw an arugument exception
            dictionary.Add(measureSetDuplicate, 2);

            Assert.AreEqual(dictionary[measureSet], dictionary[measureSetDuplicate]);
        }

        /// <summary>
        /// Ensure the to string method sorts the measures
        /// </summary>
        [TestMethod]
        public void ToStringTest()
        {
            var measureSet = new MeasureSet { 21, 5, 23, 1 };
            var measureSetString = measureSet.ToString();
            Assert.AreEqual("1, 5, 21, 23", measureSetString);
        }
    }
}
