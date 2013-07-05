//-----------------------------------------------------------------------
// <copyright file="TestContent.cs" company="Rare Crowds Inc">
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

using System.Runtime.Serialization;

namespace AzureQueueIntegrationTests
{
    /// <summary>
    /// Content class for testing
    /// </summary>
    [DataContract]
    public class TestContent
    {
        /// <summary>
        /// Gets or sets the text value
        /// </summary>
        [DataMember]
        public string TextValue { get; set; }

        /// <summary>
        /// Gets or sets the numeric value
        /// </summary>
        [DataMember]
        public int NumericValue { get; set; }

        /// <summary>
        /// Checks if this TestContent is equivalent to the specified object
        /// </summary>
        /// <param name="obj">object to compare to</param>
        /// <returns>True if obj is equivalent, otherwise False.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !this.GetType().IsAssignableFrom(obj.GetType()))
            {
                return false;
            }

            var testContent = (TestContent)obj;
            if (this.NumericValue != testContent.NumericValue || string.Compare(this.TextValue, testContent.TextValue) != 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the hashcode for the test content
        /// </summary>
        /// <returns>Hashcode based upon the numeric value and the hashcode of the TextValue</returns>
        public override int GetHashCode()
        {
            return this.NumericValue ^ (string.IsNullOrEmpty(this.TextValue) ? 0 : this.TextValue.GetHashCode());
        }
    }
}
