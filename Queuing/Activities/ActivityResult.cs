//-----------------------------------------------------------------------
// <copyright file="ActivityResult.cs" company="Emerging Media Group">
//     Copyright Emerging Media Group. All rights reserved.
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
