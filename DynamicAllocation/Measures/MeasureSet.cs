// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeasureSet.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace DynamicAllocation
{
    /// <summary>
    /// Class to represent sets of targeting attributes 
    /// </summary>
    [Serializable]
    [SuppressMessage("Microsoft.Naming", "CA1710", Justification = "Business object name.")]
    [TypeConverter(typeof(MeasureSetConverter))]
    public class MeasureSet : SortedSet<long>, IComparable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeasureSet"/> class.
        /// </summary>
        public MeasureSet()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasureSet"/> class.
        /// </summary>
        /// <param name="integers">IEnumerable of MeasureIds to initialize with</param>
        public MeasureSet(IEnumerable<long> integers)
            : base(integers)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MeasureSet"/> class.
        /// </summary>
        /// <param name="info">SerializationInfo object for base class.</param>
        /// <param name="context">StreamingCotext object for base class.</param>
        protected MeasureSet(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Generates the set product of items (the set all of all sets that has a member from each of the item sets)
        /// the bool generateSubsets determines whether a member from each item set must be present (generateSubsets==false) or may be absent (generateSubsets==true)
        /// </summary> 
        /// <param name="items">An enumerable of sets</param>
        /// <param name="generateSubsets">whether or not also generate subsets of the set product</param>
        /// <returns>a collection of sets that is the set product (with or wothout subsets included depended on the bool generateSubsets)</returns>
        public static ICollection<MeasureSet> SetProduct(IEnumerable<MeasureSet> items, bool generateSubsets)
        {
            var combos = new List<MeasureSet>();
            SetProduct(items, new MeasureSet(), combos, generateSubsets);
            return combos;
        }

        /// <summary>
        /// Creates the power set of the input measures (excluding the empty set)
        /// </summary>
        /// <param name="measures">the list of measures</param>
        /// <returns>the set of all subsets of measures (excluding the empty set)</returns>
        public static Collection<MeasureSet> PowerSet(IList<long> measures)
        {
            return PowerSet(measures, 0);
        }

        /// <summary>
        /// Creates the power set of the input measures (excluding sets with count smaller than the minCount)
        /// </summary>
        /// <param name="measures">the list of measures</param>
        /// <param name="minCount">the minimum set size to include</param>
        /// <returns>the set of all subsets of measures (excluding the empty set)</returns>
        public static Collection<MeasureSet> PowerSet(IList<long> measures, int minCount)
        {
            var measureSets = new Collection<MeasureSet>();
            for (var i = 1; i < 1 << measures.Count; i++)
            {
                var measureSet = new MeasureSet();
                var j = i;
                var index = 0;
                while (j > 0)
                {
                    if (j % 2 == 1)
                    {
                        measureSet.Add(measures[index]);
                    }

                    j /= 2;
                    index++;
                }

                if (measureSet.Count >= minCount)
                {
                    measureSets.Add(measureSet);
                }
            }

            return measureSets;
        }

        ////
        // Begin Equality Operators
        ////

        /// <summary>Equality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if equal.</returns>
        public static bool operator ==(MeasureSet left, MeasureSet right)
        {
            return Equals(left, right);
        }

        /// <summary>Inequality operator override.</summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if not equal.</returns>
        public static bool operator !=(MeasureSet left, MeasureSet right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// less than operator
        /// </summary>
        /// <param name="left">left hand side</param>
        /// <param name="right">right hand side</param>
        /// <returns>boolean result</returns>
        public static bool operator <(MeasureSet left, MeasureSet right)
        {
            return left.CompareTo(right) < 0;
        }

        /// <summary>
        /// greater than operator
        /// </summary>
        /// <param name="left">left hand side</param>
        /// <param name="right">right hand side</param>
        /// <returns>boolean result</returns>
        public static bool operator >(MeasureSet left, MeasureSet right)
        {
            return left.CompareTo(right) > 0;
        }

        /// <summary>
        /// less than or equal to operator
        /// </summary>
        /// <param name="left">left hand side</param>
        /// <param name="right">right hand side</param>
        /// <returns>boolean result</returns>
        public static bool operator <=(MeasureSet left, MeasureSet right)
        {
            return left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// greater than or equal to operator
        /// </summary>
        /// <param name="left">left hand side</param>
        /// <param name="right">right hand side</param>
        /// <returns>boolean result</returns>
        public static bool operator >=(MeasureSet left, MeasureSet right)
        {
            return left.CompareTo(right) >= 0;
        }

        /// <summary>Equals method override.</summary>
        /// <param name="other">The other MeasureSet object being checked against this for equality.</param>
        /// <returns>True if equal.</returns>
        public bool Equals(MeasureSet other)
        {
            if (other.Count != this.Count)
            {
                return false;
            }

            // Test based on member values
            return this.SetEquals(other);
        }

        /// <summary>Equals method override for generic object.</summary>
        /// <param name="obj">The other object being checked against this for equality.</param>
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

            if (obj.GetType() != typeof(MeasureSet))
            {
                return false;
            }

            // Defer to the typed Equals
            return this.Equals((MeasureSet)obj);
        }

        /// <summary>Hash function for MeasureSet type.</summary>
        /// <returns>A hash code based on the member values.</returns>
        public override int GetHashCode()
        {
            // the hashcode for a set should ignore ordering. this accomplishes that by sorting.
            var hasher = new Hasher();
            foreach (var member in this)
            {
                hasher.AddField(member);
            }

            return hasher.Value;
        }

        ////
        // End Equality Operators
        ////
        
        /// <summary>Gets a string representation of the object</summary>
        /// <returns>A string representation</returns>
        public override string ToString()
        {
            return string.Join(", ", this);
        }

        /// <summary>
        /// implements the CompareTo method for this class
        /// </summary>
        /// <param name="obj">the object being compared to</param>
        /// <returns>an int of value -1, 0, or 1</returns>
        public int CompareTo(object obj)
        {
            // Degenerate cases
            if (ReferenceEquals(null, obj))
            {
                throw new InvalidOperationException("Cannot compare to null.");
            }

            if (ReferenceEquals(this, obj))
            {
                return 0;
            }

            if (obj.GetType() != typeof(MeasureSet))
            {
                throw new InvalidOperationException("Cannot compare " + obj.GetType() + " to " + typeof(MeasureSet) + ".");
            }

            // actual CompareTo
            var other = (MeasureSet)obj;

            // compare the individual elements they contain.
            var thisOtherPairs = this.Zip(other, (e, a) => new KeyValuePair<long, long>(e, a));

            foreach (var kvp in thisOtherPairs)
            {
                if (kvp.Key.CompareTo(kvp.Value) != 0)
                {
                    // if two elements are not equal, return their comparision
                    return kvp.Key.CompareTo(kvp.Value);
                }
            }

            // if all the elements are the same, then the one with more elements should come last 
            if (this.Count > other.Count)
            {
                return 1;
            }
            
            if (this.Count < other.Count)
            {
                return -1;
            }
            
            // this.Count == other.Count
            return 0;
        }

        /// <summary>
        /// the recursive complement to the public SetProduct method 
        /// </summary>
        /// <param name="items">An enumerable of sets</param>
        /// <param name="head">used internally to make the recursion work</param>
        /// <param name="combos">the eventual result value</param>
        /// <param name="generateSubsets">whether or not also generate subsets of the set product</param>
        private static void SetProduct(IEnumerable<MeasureSet> items, MeasureSet head, ICollection<MeasureSet> combos, bool generateSubsets)
        {
            MeasureSet firstList = items.First();
            if (generateSubsets)
            {
                firstList.Add(-1);
            }

            IEnumerable<MeasureSet> restOfLists = items.Skip(1);
            foreach (var item in firstList)
            {
                var newHead = new MeasureSet(head);
                if (item != -1)
                {
                    newHead.Add(item);
                }

                if (items.Count() > 1)
                {
                    SetProduct(restOfLists, newHead, combos, generateSubsets);
                }
                else
                {
                    combos.Add(newHead);
                }
            }
        }
    }
}
