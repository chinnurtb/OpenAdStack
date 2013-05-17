//-----------------------------------------------------------------------
// <copyright file="ActivityError.cs" company="Rare Crowds Inc">
//     Copyright Rare Crowds Inc. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;

namespace Activities
{
    /// <summary>
    /// Describes an error that occured during processing
    /// </summary>
    [DataContract]
    public class ActivityError
    {
        /// <summary>
        /// Initializes a new instance of the ActivityError class.
        /// </summary>
        public ActivityError()
        {
            this.ErrorId = -1;
            this.Message = string.Empty;
            this.StackTrace = new StackTrace(1, true).ToString();
        }

        /// <summary>
        /// Gets the stack trace
        /// </summary>
        public string StackTrace { get; private set; }

        /// <summary>
        /// Gets or sets the identifier of the error that occured
        /// </summary>
        [DataMember]
        public int ErrorId { get; set; }

        /// <summary>
        /// Gets or sets a message describing details about the error
        /// </summary>
        /// <remarks>Can be suppressed for external use</remarks>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// Serializes to json
        /// </summary>
        /// <returns>the json</returns>
        public string SerializeToJson()
        {
            return new JavaScriptSerializer().Serialize(this);
        }
    }
}
