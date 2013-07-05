//-----------------------------------------------------------------------
// <copyright file="ActivityResult.cs" company="Rare Crowds Inc">
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

using System.Collections.Generic;
using System.Runtime.Serialization;
using Utilities.Serialization;

namespace Activities
{
    /// <summary>
    /// Represents the result of an activity
    /// </summary>
    [DataContract]
    public class ActivityResult : GenericXmlSerializableBase<ActivityResult>
    {
        /// <summary>Backing field for Values</summary>
        private IDictionary<string, string> values;

        /// <summary>
        /// Initializes a new instance of the ActivityResult class.
        /// </summary>
        internal ActivityResult()
        {
            this.Error = new ActivityError();
        }

        /// <summary>
        /// Gets the id of the request corresponding to this result
        /// </summary>
        [DataMember]
        public string RequestId { get; internal set; }

        /// <summary>
        /// Gets the task name of the Activity
        /// </summary>
        [DataMember]
        public string Task { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not the activity succeeded
        /// </summary>
        [DataMember]
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets the error for activities that did not succeed
        /// </summary>
        [DataMember]
        public ActivityError Error { get; private set; }

        /// <summary>
        /// Gets the collection of values for activities that succeeded
        /// </summary>
        [DataMember]
        public IDictionary<string, string> Values
        {
            get { return this.values = this.values ?? new Dictionary<string, string>(); }
            internal set { this.values = value; }
        }

        /// <summary>
        /// Creates a new ActivityResult instance from XML
        /// </summary>
        /// <param name="requestXml">The xml</param>
        /// <returns>The ActivityResult instance</returns>
        public static ActivityResult DeserializeFromXml(string requestXml)
        {
            return GenericXmlSerializableBase<ActivityResult>.DeserializeFromXmlInternal(requestXml);
        }
    }
}
