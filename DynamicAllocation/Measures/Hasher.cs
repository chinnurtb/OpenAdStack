// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Hasher.cs" company="Rare Crowds Inc">
//   Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DynamicAllocation
{
    /// <summary>
    /// Utility class for computing hash functions for objects
    /// </summary>
    public class Hasher
    {
        /// <summary>
        /// seed prime
        /// </summary>
        private const int SeedPrime = 17;

        /// <summary>
        /// multiplier prime
        /// </summary>
        private const int MultiplierPrime = 37;

        /// <summary>
        /// the return value
        /// </summary>
        private int value;

        /// <summary>
        /// Initializes a new instance of the Hasher class. the null hash is the seed prime
        /// </summary>
        public Hasher()
        {
            this.value = SeedPrime;
        }

        /// <summary>
        /// Initializes a new instance of the Hasher class. the user may specify an alternative seed
        /// </summary>
        /// <param name="seed">the alternative seed</param>
        public Hasher(int seed)
        {
            this.value = seed;
        }

        /// <summary>
        /// Gets the value
        /// </summary>
        public int Value
        {
            get
            {
                return this.value;
            }
        }

        /// <summary>
        /// add a field to the hash. you must add each field whose value you want to be included in the hash
        /// </summary>
        /// <param name="member">the object to be added</param>
        /// <returns>the hasher so that AddFields may be stringed together</returns>
        public Hasher AddField(object member)
        {
            this.value = (MultiplierPrime * this.value) + member.GetHashCode();
            return this;
        }
    }
}
