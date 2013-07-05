// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Hasher.cs" company="Rare Crowds Inc">
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
